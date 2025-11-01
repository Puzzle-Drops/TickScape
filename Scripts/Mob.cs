using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all NPCs/Mobs with combat AI.
/// Matches SDK's Mob.ts implementation.
/// 
/// KEY CONCEPTS:
/// - Mobs have STATS (strength, range, magic) that affect damage
/// - Mobs have WEAPONS that define range, speed, and calculate damage
/// - Mobs switch between weapons by changing attackStyle
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
    /// Handle auto-aggro timer.
    /// </summary>
    public override void TimerStep()
    {
        base.TimerStep();

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

    #region Movement AI

    /// <summary>
    /// Calculate next tile to move toward player.
    /// SDK Reference: Mob.getNextMovementStep() in Mob.ts lines 103-153
    /// </summary>
    protected Vector2Int GetNextMovementStep()
    {
        if (aggro == null)
            return gridPosition;

        // Calculate direction toward player
        int dx = gridPosition.x + System.Math.Sign(aggro.gridPosition.x - gridPosition.x);
        int dy = gridPosition.y + System.Math.Sign(aggro.gridPosition.y - gridPosition.y);

        // Check if player is under the mob
        if (Collision.CollisionMath(gridPosition.x, gridPosition.y, size,
                                    aggro.gridPosition.x, aggro.gridPosition.y, 1))
        {
            // Player is under mob - do random movement to get unstuck
            if (aggro.lastInteraction == this && aggro.lastInteractionAge == 0)
            {
                // Cannot move if player just interacted with this mob
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
            // If moving diagonally would place us on player, move only on Y axis
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
    /// Execute mob movement with pathfinding validation.
    /// SDK Reference: Mob.movementStep() in Mob.ts lines 189-270
    /// </summary>
    public override void MovementStep()
    {
        if (IsDying())
            return;

        // Process incoming attacks first
        ProcessIncomingAttacks();

        // Update line of sight
        SetHasLOS();

        // Check if movement is enabled
        if (!canMove)
            return;

        // Don't move if can't move or no aggro
        if (!CanMove() || aggro == null)
            return;

        // Get target tile
        Vector2Int targetTile = GetNextMovementStep();

        // Check if we can move there
        if (targetTile != gridPosition)
        {
            if (Collision.IsTileWalkable(targetTile, size, this))
            {
                gridPosition = targetTile;
            }
        }
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
            Player playerComponent = FindObjectOfType<Player>();
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