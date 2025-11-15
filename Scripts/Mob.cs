using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all NPCs/Mobs with combat AI.
/// Matches SDK's Mob.ts implementation with EXACT movement logic.
/// 
/// ============================================================================
/// CRITICAL: COORDINATE SYSTEM DIFFERENCES BETWEEN SDK AND UNITY
/// ============================================================================
/// 
/// SDK (TypeScript):
///   - X-axis: East-West (X+ = East) ✓
///   - Y-axis: North-South where Y+ = SOUTH (visually down)
///   - Location: Southwest corner of entity
///   
/// Unity (This Implementation):
///   - X-axis: East-West (X+ = East) ✓ SAME
///   - Y-axis: North-South where Y+ = NORTH (visually up, higher Z in world) ⚠️ INVERTED
///   - gridPosition: Southwest corner of entity ✓ SAME
/// 
/// Example - 2x2 mob at gridPosition (15, 17):
///   SDK Coordinates:          Unity Coordinates:
///   NW: (15, 16) ← Y lower   NW: (15, 18) ← Y higher
///   SW: (15, 17) ← origin    SW: (15, 17) ← origin
///   NE: (16, 16)             NE: (16, 18)
///   SE: (16, 17)             SE: (16, 17)
/// 
/// This inversion affects ALL Y-axis calculations in movement methods!
/// ============================================================================
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

    /// <summary>
    /// Draw movement tile checks in Scene view for debugging.
    /// Green = walkable tile being checked
    /// Red = blocked tile being checked
    /// Yellow = diagonal attempt
    /// Magenta = fallback attempt
    /// </summary>
    void OnDrawGizmos()
    {
        if (!debugMovement || !Application.isPlaying || GridManager.Instance == null)
            return;

        if (aggro == null)
            return;

        // Calculate what the next movement step would be
        Vector2Int targetTile = GetNextMovementStep();
        int xOff = targetTile.x - gridPosition.x;
        int yOff = targetTile.y - gridPosition.y;

        // Visualize diagonal attempt
        List<Vector2Int> xTiles = GetXMovementTiles(xOff, yOff);
        List<Vector2Int> yTiles = GetYMovementTiles(xOff, yOff);

        foreach (var tile in xTiles)
        {
            bool walkable = Collision.IsTileWalkable(tile, 1, this);
            Gizmos.color = walkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Vector3 worldPos = GridManager.Instance.GridToWorld(tile);
            // Center on tile (GridToWorld returns SW corner, add 0.5 to center)
            Gizmos.DrawCube(worldPos + new Vector3(0.5f, 0.1f, 0.5f), new Vector3(0.9f, 0.1f, 0.9f));
        }

        foreach (var tile in yTiles)
        {
            bool walkable = Collision.IsTileWalkable(tile, 1, this);
            Gizmos.color = walkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Vector3 worldPos = GridManager.Instance.GridToWorld(tile);
            // Center on tile
            Gizmos.DrawCube(worldPos + new Vector3(0.5f, 0.1f, 0.5f), new Vector3(0.9f, 0.1f, 0.9f));
        }

        // Visualize fallback attempts if diagonal blocked
        bool xSpace = AllTilesWalkable(xTiles);
        bool ySpace = AllTilesWalkable(yTiles);
        bool both = xSpace && ySpace;

        if (!both && xOff != 0)
        {
            // X-only fallback
            List<Vector2Int> xOnlyTiles = GetXMovementTiles(xOff, 0);
            foreach (var tile in xOnlyTiles)
            {
                bool walkable = Collision.IsTileWalkable(tile, 1, this);
                Gizmos.color = walkable ? new Color(1, 1, 0, 0.3f) : new Color(1, 0.5f, 0, 0.3f);
                Vector3 worldPos = GridManager.Instance.GridToWorld(tile);
                // Center on tile, slightly higher Y to layer
                Gizmos.DrawCube(worldPos + new Vector3(0.5f, 0.15f, 0.5f), new Vector3(0.8f, 0.1f, 0.8f));
            }
        }

        if (!both && !xSpace && yOff != 0)
        {
            // Y-only fallback
            List<Vector2Int> yOnlyTiles = GetYMovementTiles(0, yOff);
            foreach (var tile in yOnlyTiles)
            {
                bool walkable = Collision.IsTileWalkable(tile, 1, this);
                Gizmos.color = walkable ? new Color(1, 0, 1, 0.3f) : new Color(0.5f, 0, 0.5f, 0.3f);
                Vector3 worldPos = GridManager.Instance.GridToWorld(tile);
                // Center on tile, even higher Y to layer
                Gizmos.DrawCube(worldPos + new Vector3(0.5f, 0.2f, 0.5f), new Vector3(0.7f, 0.1f, 0.7f));
            }
        }
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
    /// 
    /// SDK Reference: Mob.getXMovementTiles() in Mob.ts lines 312-333
    /// 
    /// COORDINATE SYSTEM NOTE:
    /// SDK uses Y+ = South, so it does "y - i" to iterate northward
    /// Unity uses Y+ = North, so we do "y + i" to iterate northward
    /// 
    /// IMPORTANT: For a 2x2 mob moving east, we check 2 tiles along the eastern edge.
    /// The tiles checked depend on if we're also moving diagonally.
    /// </summary>
    /// <param name="xOff">X movement offset (-1 = west, 0 = none, 1 = east)</param>
    /// <param name="yOff">Y movement offset (-1 = south, 0 = none, 1 = north)</param>
    private List<Vector2Int> GetXMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> xTiles = new List<Vector2Int>();

        // If not moving in X direction, return empty
        if (xOff == 0)
            return xTiles;

        // Adjust Y range if moving diagonally
        // SDK Reference: Mob.ts lines 315-316
        int start = yOff == -1 ? -1 : 0;  // If moving south, check one extra tile south
        int end = yOff == 1 ? size + 1 : size;  // If moving north, check one extra tile north

        if (xOff == -1)
        {
            // Moving WEST - check tiles along western edge
            // SDK Reference: Mob.ts lines 318-324
            for (int i = start; i < end; i++)
            {
                // CRITICAL FIX: Unity Y+ = North, so we ADD i (not subtract like SDK)
                // For 2x2 mob at (15, 17):
                //   i=0: (14, 17) - SW western neighbor
                //   i=1: (14, 18) - NW western neighbor (Y increased going north)
                xTiles.Add(new Vector2Int(
                    gridPosition.x - 1,
                    gridPosition.y + i  // FIXED: Was gridPosition.y - i
                ));
            }
        }
        else if (xOff == 1)
        {
            // Moving EAST - check tiles along eastern edge
            // SDK Reference: Mob.ts lines 325-331
            for (int i = start; i < end; i++)
            {
                // CRITICAL FIX: Unity Y+ = North, so we ADD i (not subtract like SDK)
                // For 2x2 mob at (15, 17):
                //   i=0: (17, 17) - SE eastern neighbor
                //   i=1: (17, 18) - NE eastern neighbor (Y increased going north)
                xTiles.Add(new Vector2Int(
                    gridPosition.x + size,
                    gridPosition.y + i  // FIXED: Was gridPosition.y - i
                ));
            }
        }

        return xTiles;
    }

    /// <summary>
    /// Get tiles along the Y edge that need to be clear for Y movement.
    /// Handles multi-tile mobs (size > 1).
    /// 
    /// SDK Reference: Mob.getYMovementTiles() in Mob.ts lines 335-363
    /// 
    /// COORDINATE SYSTEM NOTE:
    /// SDK: Y+ = SOUTH (going south increases Y)
    ///   - Moving south (yOff = -1) checks location.y + 1 (one tile MORE south)
    ///   - Moving north (yOff = 1) checks location.y - size (one tile MORE north)
    /// 
    /// Unity: Y+ = NORTH (going north increases Y)
    ///   - Moving south (yOff = -1) checks gridPosition.y - 1 (one tile LESS north = more south)
    ///   - Moving north (yOff = 1) checks gridPosition.y + size (one tile MORE north)
    /// 
    /// This inversion is why the calculations are flipped!
    /// </summary>
    /// <param name="xOff">X movement offset (-1 = west, 0 = none, 1 = east)</param>
    /// <param name="yOff">Y movement offset (-1 = south, 0 = none, 1 = north)</param>
    private List<Vector2Int> GetYMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> yTiles = new List<Vector2Int>();

        // If not moving in Y direction, return empty
        if (yOff == 0)
            return yTiles;

        // Adjust X range if moving diagonally
        // SDK Reference: Mob.ts lines 338-339
        int start = xOff == -1 ? -1 : 0;  // If moving west, check one extra tile west
        int end = xOff == 1 ? size + 1 : size;  // If moving east, check one extra tile east

        if (yOff == -1)
        {
            // Moving SOUTH (toward lower Y values in Unity)
            // SDK Reference: Mob.ts lines 341-347
            // SDK does: y + 1 (because in SDK, Y+ = south, so +1 is more south)
            // Unity: Y+ = north, so -1 is more south
            // 
            // For 2x2 mob at (15, 17) moving south:
            //   SW corner at (15, 17) - one tile south is (15, 16)
            //   We check tiles at Y = 16 (one below SW corner)
            for (int i = start; i < end; i++)
            {
                // CRITICAL FIX: Unity Y+ = North, so to go south we SUBTRACT 1
                // For 2x2 mob at (15, 17):
                //   i=0: (15, 16) - southern neighbor of SW corner
                //   i=1: (16, 16) - southern neighbor of SE corner
                yTiles.Add(new Vector2Int(
                    gridPosition.x + i,
                    gridPosition.y - 1  // FIXED: Was gridPosition.y - size
                ));
            }
        }
        else if (yOff == 1)
        {
            // Moving NORTH (toward higher Y values in Unity)
            // SDK Reference: Mob.ts lines 348-354
            // SDK does: y - size (because in SDK, Y+ = south, so -size is more north)
            // Unity: Y+ = north, so +size is more north
            // 
            // For 2x2 mob at (15, 17) moving north:
            //   NW corner at (15, 18) - one tile north is (15, 19)
            //   We check tiles at Y = 19 (one above NW corner)
            for (int i = start; i < end; i++)
            {
                // CRITICAL FIX: Unity Y+ = North, so to go north we ADD size
                // For 2x2 mob at (15, 17):
                //   i=0: (15, 19) - northern neighbor of NW corner
                //   i=1: (16, 19) - northern neighbor of NE corner
                yTiles.Add(new Vector2Int(
                    gridPosition.x + i,
                    gridPosition.y + size  // FIXED: Was gridPosition.y + 1
                ));
            }
        }

        return yTiles;
    }

    /// <summary>
    /// Check if all tiles in a list are walkable.
    /// Equivalent to SDK's every() function.
    /// SDK Reference: Mob.ts lines 237-244 (uses .every())
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

        // Save position for visual interpolation BEFORE moving
        // SDK Reference: Mob.ts line 199 - perceivedLocation = { x: this.location.x, y: this.location.y }
        perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);

        // Update line of sight BEFORE movement (for movement decisions)
        // SDK Reference: Mob.ts line 201
        SetHasLOS();

        // Check if movement is enabled
        if (!canMove)
            return;

        // Don't move if can't move or no aggro
        // SDK Reference: Mob.ts line 202
        if (!CanMove() || aggro == null)
            return;

        // ===== STAGE 0: Get desired target tile =====
        Vector2Int targetTile = GetNextMovementStep();

        // Calculate movement offsets
        int xOff = targetTile.x - gridPosition.x;
        int yOff = targetTile.y - gridPosition.y;

        // ===== STAGE 1: Try DIAGONAL movement (both X and Y) =====
        // SDK Reference: Mob.ts lines 209-244
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
        // SDK Reference: Mob.ts lines 246-262
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
        // SDK Reference: Mob.ts lines 264-270
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
        // SDK Reference: Mob.ts lines 217-221
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
        // SDK Reference: Mob.ts lines 223-229
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
        // SDK Reference: Mob.ts lines 230-242
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
        // SDK Reference: Mob.ts lines 274-276
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
        // SDK Reference: Mob.ts lines 289-290
        hadLOS = hasLOS;
        SetHasLOS();

        if (aggro == null || !CanAttack())
            return;

        // Determine attack style for this attack
        // SDK Reference: Mob.ts line 295
        attackStyle = AttackStyleForNewAttack();

        // Check if player is under mob
        // SDK Reference: Mob.ts lines 297-303
        bool isUnderAggro = Collision.CollisionMath(
            gridPosition.x, gridPosition.y, size,
            aggro.gridPosition.x, aggro.gridPosition.y, 1);

        // Reset feedback
        attackFeedback = AttackIndicator.NONE;

        // Attack if conditions met
        // SDK Reference: Mob.ts lines 304-306
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