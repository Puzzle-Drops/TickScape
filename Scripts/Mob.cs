using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all NPCs/Mobs with combat AI.
/// Matches SDK's Mob.ts implementation with EXACT movement logic.
/// 
/// KEY CONCEPTS:
/// - Mobs have STATS (strength, range, magic) that affect damage
/// - Mobs have WEAPONS that define range, speed, and calculate damage
/// - Mobs switch between weapons by changing attackStyle
/// - Mobs use 3-STAGE MOVEMENT FALLBACK (diagonal → X-only → Y-only)
/// 
/// SETUP:
/// 1. Create GameObject with Mob component
/// 2. Set stats in Inspector or code
/// 3. Create weapons in Start() or assign via Inspector
/// 4. Override AttackStyleForNewAttack() to control attack patterns
/// </summary>
public class Mob : Unit
{
    #region Inspector Configuration

    [Header("Mob Identity")]
    [Tooltip("Display name of this mob")]
    public string mobName = "Unknown Mob";

    // Note: gridPosition is inherited from Entity base class
    // Add context menu for position syncing
    [ContextMenu("Sync Grid Position from Transform")]
    public void SyncGridPositionFromTransform()
    {
        if (GridManager.Instance != null)
        {
            // Convert world position to grid coordinates
            gridPosition = GridManager.Instance.WorldToGrid(transform.position);
            Debug.Log($"[MOB] Updated grid position to {gridPosition} from transform position {transform.position}");
        }
        else
        {
            // Fallback if no GridManager (rough conversion)
            gridPosition = new Vector2Int(
                Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.z)
            );
            Debug.LogWarning("[MOB] No GridManager found, using rough conversion");
        }
    }

    [Header("Combat Stats - Affects Damage Calculations")]
    [Tooltip("Attack level (1-99). Affects MELEE accuracy")]
    public int attackLevel = 75;

    [Tooltip("Strength level (1-99). Affects MELEE max hit")]
    public int strengthLevel = 75;

    [Tooltip("Defence level (1-99). Reduces chance to be hit")]
    public int defenceLevel = 50;

    [Tooltip("Range level (1-99). Affects RANGED accuracy AND max hit")]
    public int rangeLevel = 75;

    [Tooltip("Magic level (1-99). Affects MAGIC accuracy")]
    public int magicLevel = 75;

    [Tooltip("Hitpoints. Total HP of this mob")]
    public int hitpointsLevel = 60;

    [Header("Combat Behavior")]
    [Tooltip("Can this mob be attacked by the player?")]
    public bool canBeAttacked = true;

    [Tooltip("Can this mob move? Unchecked = stationary")]
    public bool canMove = true;

    [Tooltip("Automatically aggro the player on spawn?")]
    public bool autoAggroPlayer = false;

    [Tooltip("Delay in ticks before auto-aggro activates")]
    public int autoAggroDelay = 2;

    [Tooltip("Show attack indicators for debugging")]
    public bool showAttackIndicators = true;

    [Header("Movement Debug Visualization")]
    [Tooltip("Show movement tile checks in Scene view (green = checking, red = blocked)")]
    public bool debugMovement = false;

    #endregion

    #region Mob State

    /// <summary>
    /// Weapons mapped by attack style name.
    /// SDK Reference: Mob.weapons in Mob.ts
    /// </summary>
    public Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>();

    /// <summary>
    /// Current attack style being used.
    /// Options: "slash", "crush", "stab", "range", "magic"
    /// SDK Reference: Mob.attackStyle in Mob.ts
    /// </summary>
    public string attackStyle = "slash";

    /// <summary>
    /// Visual attack feedback for debugging.
    /// SDK Reference: Mob.attackFeedback in Mob.ts
    /// </summary>
    public AttackIndicator attackFeedback = AttackIndicator.NONE;

    /// <summary>
    /// Had line of sight last tick (for tracking LOS changes).
    /// SDK Reference: Mob.hadLOS in Mob.ts
    /// </summary>
    public bool hadLOS = false;

    /// <summary>
    /// Spawn delay counter. Mob doesn't move until this reaches 0.
    /// SDK Reference: Unit.age in Unit.ts
    /// </summary>
    private int age = 0;

    // Auto-aggro tracking
    private int aggroTimer = 0;
    private bool hasAggroed = false;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Mobs ALWAYS block other mobs from moving through them
        // SDK Reference: Mob.consumesSpace getter in Mob.ts
        consumesSpace = true;

        // Mobs auto-retaliate when attacked
        autoRetaliate = true;
    }

    protected virtual void Start()
    {
        // Register with GridManager
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterEntity(this);
        }

        // Add ProjectileRenderer for visual projectiles
        if (GetComponent<ProjectileRenderer>() == null)
        {
            gameObject.AddComponent<ProjectileRenderer>();
        }

        // Add HitsplatRenderer for damage numbers (if you have this component)
        if (GetComponent<HitsplatRenderer>() == null)
        {
            gameObject.AddComponent<HitsplatRenderer>();
        }

        // Initialize auto-aggro timer if enabled
        if (autoAggroPlayer)
        {
            aggroTimer = autoAggroDelay;
            age = autoAggroDelay; // Also set spawn delay
        }

        // Subclasses should create weapons in Start()
    }

    #endregion

    #region Stats Setup

    /// <summary>
    /// Set base stats from inspector values.
    /// SDK Reference: Mob.setStats() in Mob.ts lines 78-95
    /// </summary>
    public override void SetStats()
    {
        // Use inspector values or default to 99s
        stats = new UnitStats
        {
            attack = attackLevel > 0 ? attackLevel : 99,
            strength = strengthLevel > 0 ? strengthLevel : 99,
            defence = defenceLevel > 0 ? defenceLevel : 99,
            range = rangeLevel > 0 ? rangeLevel : 99,
            magic = magicLevel > 0 ? magicLevel : 99,
            hitpoint = hitpointsLevel > 0 ? hitpointsLevel : 99,
            prayer = 0 // NPCs don't use prayer
        };

        currentStats = stats.Clone();
    }

    #endregion

    #region Timer Step Override

    /// <summary>
    /// Handle spawn delay and auto-aggro timer.
    /// SDK Reference: Mob.timerStep() in Mob.ts + Unit.movementStep() lines 195-198
    /// </summary>
    public override void TimerStep()
    {
        base.TimerStep();

        // SDK spawn delay system
        // Mob doesn't move/act until age reaches 0
        age--;

        // Handle auto-aggro countdown
        if (autoAggroPlayer && !hasAggroed && aggroTimer > 0)
        {
            aggroTimer--;
            if (aggroTimer <= 0)
            {
                SetAggressiveToPlayer();
                hasAggroed = true;
            }
        }
    }

    #endregion

    #region Movement System - SDK EXACT IMPLEMENTATION

    /// <summary>
    /// Get tiles along the X edge that need to be clear for X movement.
    /// Handles multi-tile mobs (size > 1).
    /// SDK Reference: Mob.getXMovementTiles() in Mob.ts lines 312-333
    /// 
    /// IMPORTANT: For a 2x2 mob moving east, we check 2 tiles along the eastern edge.
    /// The tiles checked depend on if we're also moving diagonally.
    /// </summary>
    /// <param name="xOff">X movement offset (-1 = west, 0 = none, 1 = east)</param>
    /// <param name="yOff">Y movement offset (-1 = north, 0 = none, 1 = south)</param>
    private List<Vector2Int> GetXMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> xTiles = new List<Vector2Int>();

        // If not moving in X direction, return empty
        if (xOff == 0)
            return xTiles;

        // Adjust Y range if moving diagonally
        int start = yOff == -1 ? -1 : 0;
        int end = yOff == 1 ? size + 1 : size;

        if (xOff == -1)
        {
            // Moving WEST - check tiles along western edge
            for (int i = start; i < end; i++)
            {
                xTiles.Add(new Vector2Int(
                    gridPosition.x - 1,
                    gridPosition.y - i
                ));
            }
        }
        else if (xOff == 1)
        {
            // Moving EAST - check tiles along eastern edge
            for (int i = start; i < end; i++)
            {
                xTiles.Add(new Vector2Int(
                    gridPosition.x + size,
                    gridPosition.y - i
                ));
            }
        }

        return xTiles;
    }

    /// <summary>
    /// Get tiles along the Y edge that need to be clear for Y movement.
    /// Handles multi-tile mobs (size > 1).
    /// SDK Reference: Mob.getYMovementTiles() in Mob.ts lines 335-363
    /// 
    /// UNITY COORDINATE SYSTEM:
    /// - Y+ = NORTH (forward in world, higher Z values)
    /// - yOff = 1 means moving NORTH (to higher Y)
    /// - yOff = -1 means moving SOUTH (to lower Y)
    /// </summary>
    /// <param name="xOff">X movement offset (-1 = west, 0 = none, 1 = east)</param>
    /// <param name="yOff">Y movement offset (-1 = north, 0 = none, 1 = south)</param>
    private List<Vector2Int> GetYMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> yTiles = new List<Vector2Int>();

        // If not moving in Y direction, return empty
        if (yOff == 0)
            return yTiles;

        // Adjust X range if moving diagonally
        int start = xOff == -1 ? -1 : 0;
        int end = xOff == 1 ? size + 1 : size;

        if (yOff == -1)
        {
            // Moving SOUTH (to lower Y values in Unity)
            for (int i = start; i < end; i++)
            {
                yTiles.Add(new Vector2Int(
                    gridPosition.x + i,
                    gridPosition.y - size  // Southern edge
                ));
            }
        }
        else if (yOff == 1)
        {
            // Moving NORTH (to higher Y values in Unity)
            for (int i = start; i < end; i++)
            {
                yTiles.Add(new Vector2Int(
                    gridPosition.x + i,
                    gridPosition.y + 1  // Northern edge
                ));
            }
        }

        return yTiles;
    }

    /// <summary>
    /// Check if all tiles in a list are walkable.
    /// Equivalent to SDK's every() function.
    /// </summary>
    private bool AllTilesWalkable(List<Vector2Int> tiles)
    {
        if (tiles.Count == 0)
            return true; // No tiles to check = movement is valid

        foreach (Vector2Int tile in tiles)
        {
            if (!Collision.IsTileWalkable(tile, 1, this))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calculate next tile to move toward target.
    /// SDK Reference: Mob.getNextMovementStep() in Mob.ts lines 103-153
    /// </summary>
    protected Vector2Int GetNextMovementStep()
    {
        if (aggro == null)
            return gridPosition;

        // Calculate direction toward target
        int dx = gridPosition.x + System.Math.Sign(aggro.gridPosition.x - gridPosition.x);
        int dy = gridPosition.y + System.Math.Sign(aggro.gridPosition.y - gridPosition.y);

        // Check if target is under the mob
        if (Collision.CollisionMath(gridPosition.x, gridPosition.y, size,
                                    aggro.gridPosition.x, aggro.gridPosition.y, 1))
        {
            // Target is under mob - do random movement to get unstuck
            if (aggro.lastInteraction == this && aggro.lastInteractionAge == 0)
            {
                // Cannot move if target just interacted with this mob
                return gridPosition;
            }

            // Random X or Y movement
            if (RandomHelper.Get() < 0.5f)
            {
                dy = gridPosition.y;
                dx = gridPosition.x + (RandomHelper.Get() < 0.5f ? 1 : -1);
            }
            else
            {
                dx = gridPosition.x;
                dy = gridPosition.y + (RandomHelper.Get() < 0.5f ? 1 : -1);
            }
        }
        else if (Collision.CollisionMath(dx, dy, size, aggro.gridPosition.x, aggro.gridPosition.y, 1))
        {
            // If moving diagonally would place us on target, move only on Y axis
            // This allows corner safespotting
            dy = gridPosition.y;
        }

        // No movement right after melee special attack (8 ticks)
        if (attackDelay > GetAttackSpeed())
        {
            dx = gridPosition.x;
            dy = gridPosition.y;
        }

        return new Vector2Int(dx, dy);
    }

    /// <summary>
    /// Execute mob movement with 3-STAGE FALLBACK SYSTEM.
    /// This is the CORE of OSRS mob AI movement.
    /// SDK Reference: Mob.movementStep() in Mob.ts lines 189-270
    /// 
    /// MOVEMENT ALGORITHM:
    /// 1. Try DIAGONAL: Check both X and Y tiles → Move diagonally
    /// 2. If diagonal blocked, try X-ONLY → Move horizontally
    /// 3. If X blocked, try Y-ONLY → Move vertically
    /// 
    /// This ensures mobs ALWAYS try to close distance even if direct path is blocked.
    /// </summary>
    public override void MovementStep()
    {
        if (IsDying())
            return;

        // Process incoming attacks first
        ProcessIncomingAttacks();

        // SDK spawn delay - don't move until age reaches 0
        if (age > 0)
            return;

        // Save position for visual interpolation
        perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);

        // Update line of sight
        SetHasLOS();

        // Check if movement is enabled
        if (!canMove)
            return;

        // Don't move if can't move or no aggro
        if (!CanMove() || aggro == null)
            return;

        // ===== STAGE 0: Get desired target tile =====
        Vector2Int targetTile = GetNextMovementStep();

        // Calculate movement offsets
        int xOff = targetTile.x - gridPosition.x;
        int yOff = targetTile.y - gridPosition.y;  // Unity coordinate system: Y+ = North

        // ===== STAGE 1: Try DIAGONAL movement (both X and Y) =====
        List<Vector2Int> xTiles = GetXMovementTiles(xOff, yOff);
        List<Vector2Int> yTiles = GetYMovementTiles(xOff, yOff);

        bool xSpace = AllTilesWalkable(xTiles);
        bool ySpace = AllTilesWalkable(yTiles);
        bool both = xSpace && ySpace;

        // Debug visualization - DIAGONAL attempt
        if (debugMovement && xTiles.Count > 0 && yTiles.Count > 0)
        {
            Color debugColor = both ? Color.green : Color.red;
            foreach (var tile in xTiles)
            {
                Collision.DrawCollisionBounds(tile, 1, debugColor, 0.6f);
            }
            foreach (var tile in yTiles)
            {
                Collision.DrawCollisionBounds(tile, 1, debugColor, 0.6f);
            }
        }

        // ===== STAGE 2: If diagonal blocked, try FALLBACK =====
        if (!both)
        {
            // Try X-only movement
            xTiles = GetXMovementTiles(xOff, 0);
            xSpace = AllTilesWalkable(xTiles);

            // Debug visualization - X-only attempt
            if (debugMovement && xTiles.Count > 0)
            {
                Color debugColor = xSpace ? Color.green : Color.yellow;
                foreach (var tile in xTiles)
                {
                    Collision.DrawCollisionBounds(tile, 1, debugColor, 0.6f);
                }
            }

            if (!xSpace)
            {
                // X is blocked, try Y-only movement
                yTiles = GetYMovementTiles(0, yOff);
                ySpace = AllTilesWalkable(yTiles);

                // Debug visualization - Y-only attempt
                if (debugMovement && yTiles.Count > 0)
                {
                    Color debugColor = ySpace ? Color.green : Color.magenta;
                    foreach (var tile in yTiles)
                    {
                        Collision.DrawCollisionBounds(tile, 1, debugColor, 0.6f);
                    }
                }
            }
        }

        // ===== STAGE 3: Execute movement in valid direction(s) =====
        if (both)
        {
            // Diagonal movement
            gridPosition.x = targetTile.x;
            gridPosition.y = targetTile.y;
            if (debugMovement)
                Debug.Log($"[MOB] {mobName} moved DIAGONALLY to {gridPosition}");
        }
        else if (xSpace)
        {
            // X-only movement
            gridPosition.x = targetTile.x;
            if (debugMovement)
                Debug.Log($"[MOB] {mobName} moved HORIZONTALLY (X-only) to {gridPosition}");
        }
        else if (ySpace)
        {
            // Y-only movement
            gridPosition.y = targetTile.y;
            if (debugMovement)
                Debug.Log($"[MOB] {mobName} moved VERTICALLY (Y-only) to {gridPosition}");
        }
        else
        {
            // All movement blocked
            if (debugMovement)
                Debug.Log($"[MOB] {mobName} CANNOT MOVE - all directions blocked");
        }
    }

    #endregion

    #region Combat Overrides

    /// <summary>
    /// Get attack range from current weapon.
    /// SDK Reference: Mob inherits from Unit which has attackRange getter
    /// </summary>
    public override int GetAttackRange()
    {
        if (weapons.ContainsKey(attackStyle) && weapons[attackStyle] != null)
        {
            return weapons[attackStyle].attackRange;
        }
        return 1; // Default melee range
    }

    /// <summary>
    /// Get attack speed from current weapon.
    /// SDK Reference: Mob inherits from Unit which has attackSpeed getter
    /// </summary>
    public override int GetAttackSpeed()
    {
        if (weapons.ContainsKey(attackStyle) && weapons[attackStyle] != null)
        {
            return weapons[attackStyle].attackSpeed;
        }
        return 4; // Default 4 ticks
    }

    /// <summary>
    /// Execute attack with current weapon.
    /// SDK Reference: Mob.attack() in Mob.ts lines 215-243
    /// </summary>
    public override void PerformAttack()
    {
        if (aggro == null || aggro.IsDying())
            return;

        // Check if should switch to melee (for hybrid mobs)
        string meleeStyle = CanMeleeIfClose();
        if (!string.IsNullOrEmpty(meleeStyle) && !Weapon.IsMeleeAttackStyle(attackStyle))
        {
            if (IsWithinMeleeRange() && RandomHelper.Get() < 0.5f)
            {
                // 50% chance to switch to melee when in range
                attackStyle = meleeStyle;
            }
        }

        // Get current weapon
        if (!weapons.ContainsKey(attackStyle) || weapons[attackStyle] == null)
        {
            Debug.LogWarning($"[MOB] {mobName} has no weapon for style: {attackStyle}");
            return;
        }

        Weapon weapon = weapons[attackStyle];

        // Check if attack will be blocked by prayer
        AttackBonuses bonuses = new AttackBonuses { attackStyle = attackStyle };
        if (weapon.IsBlockable(this, aggro, attackStyle))
        {
            attackFeedback = AttackIndicator.BLOCKED;
        }
        else
        {
            attackFeedback = AttackIndicator.HIT;
        }

        // Execute attack
        weapon.Attack(this, aggro, bonuses);
    }

    #endregion

    #region Attack Style Selection (Virtual - Override in Subclasses)

    /// <summary>
    /// Determine attack style for next attack.
    /// Override in subclasses to customize behavior.
    /// SDK Reference: Mob.attackStyleForNewAttack() in Mob.ts lines 210-212
    /// 
    /// Examples:
    /// - Pure melee mob: return "slash"
    /// - Pure ranged mob: return "range"
    /// - Hybrid mob: return distance-based style
    /// </summary>
    public virtual string AttackStyleForNewAttack()
    {
        // Default: use first available weapon
        if (weapons.ContainsKey("slash")) return "slash";
        if (weapons.ContainsKey("range")) return "range";
        if (weapons.ContainsKey("magic")) return "magic";

        return "slash"; // Fallback
    }

    /// <summary>
    /// Can this mob use melee when player is adjacent?
    /// Override in hybrid mobs that switch between melee/ranged.
    /// SDK Reference: Mob.canMeleeIfClose() in Mob.ts lines 207-209
    /// 
    /// Return empty string = never melee when close
    /// Return "slash"/"crush"/"stab" = can melee when close
    /// </summary>
    public virtual string CanMeleeIfClose()
    {
        return ""; // Default: don't switch
    }

    #endregion

    #region Auto-Aggro System

    /// <summary>
    /// Make this mob aggressive toward the player.
    /// Searches for player by tag first, then by component.
    /// </summary>
    public void SetAggressiveToPlayer()
    {
        // Find the player - try by tag first
        GameObject playerObj = GameObject.FindWithTag("Player");

        // If no tag, try finding by Player component
        if (playerObj == null)
        {
            Player playerComponent = FindAnyObjectByType<Player>();
            if (playerComponent != null)
            {
                playerObj = playerComponent.gameObject;
            }
        }

        if (playerObj != null)
        {
            Player player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                SetAggro(player);
                Debug.Log($"[MOB] {mobName} now aggressive to player!");
            }
            else
            {
                Debug.LogWarning($"[MOB] Found player object but no Player component!");
            }
        }
        else
        {
            Debug.LogWarning($"[MOB] Could not find player in scene!");
        }
    }

    #endregion

    #region Attack Step Override

    /// <summary>
    /// Execute combat logic each tick.
    /// SDK Reference: Mob.attackStep() in Mob.ts lines 272-283
    /// </summary>
    public override void AttackStep()
    {
        base.AttackStep();

        // Don't process attack during spawn delay
        if (age > 0)
            return;

        if (IsDying())
            return;

        // Attempt attack
        AttackIfPossible();
    }

    /// <summary>
    /// Check conditions and execute attack.
    /// SDK Reference: Mob.attackIfPossible() in Mob.ts lines 288-306
    /// </summary>
    protected void AttackIfPossible()
    {
        hadLOS = hasLOS;
        SetHasLOS();

        if (aggro == null || !CanAttack())
            return;

        // Determine attack style for this attack
        attackStyle = AttackStyleForNewAttack();

        // Check if player is under mob
        bool isUnderAggro = Collision.CollisionMath(
            gridPosition.x, gridPosition.y, size,
            aggro.gridPosition.x, aggro.gridPosition.y, 1);

        // Reset feedback
        attackFeedback = AttackIndicator.NONE;

        // Attack if conditions met
        if (!isUnderAggro && hasLOS && attackDelay <= 0)
        {
            PerformAttack();
            DidAttack(); // Reset attack delay
        }
    }

    #endregion

    #region Utility Overrides

    /// <summary>
    /// Get mob's display name.
    /// SDK Reference: Mob.mobName() in Mob.ts
    /// </summary>
    public override string UnitName()
    {
        return mobName;
    }

    /// <summary>
    /// Can this mob be attacked?
    /// SDK Reference: Mob.canBeAttacked() in Mob.ts
    /// </summary>
    public bool CanBeAttacked()
    {
        return canBeAttacked;
    }

    #endregion

    #region Death Animation

    /// <summary>
    /// Death animation: fade to 10% scale.
    /// SDK Reference: Not in SDK (Unity-specific visual)
    /// </summary>
    public override void Dead()
    {
        base.Dead();
        StartCoroutine(DeathFadeCoroutine());
    }

    private System.Collections.IEnumerator DeathFadeCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 0.1f;
        float duration = GetDeathAnimationLength() * 0.6f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }
    }

    #endregion
}
