using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all NPCs/Mobs with combat AI.
/// Matches SDK's Mob.ts implementation.
/// SDK Reference: Mob.ts
/// 
/// KEY BEHAVIORS:
/// - Multiple weapons (switches between melee/ranged/magic)
/// - Movement AI (chases player, avoids corners)
/// - Attack style selection
/// - Line of sight tracking
/// - Auto-aggro when attacked
/// </summary>
public class Mob : Unit
{
    #region Inspector Configuration

    [Header("Mob Identity")]
    [Tooltip("Display name of this mob")]
    public string mobName = "Unknown Mob";

    [Header("Weapons (Assign via Inspector)")]
    [Tooltip("Melee weapon (slash/crush/stab styles)")]
    public Weapon meleeWeapon;

    [Tooltip("Ranged weapon")]
    public Weapon rangedWeapon;

    [Tooltip("Magic weapon")]
    public Weapon magicWeapon;

    [Header("Combat Behavior")]
    [Tooltip("Can this mob be attacked by the player?")]
    public bool canBeAttacked = true;

    [Tooltip("Show attack indicators for debugging")]
    public bool showAttackIndicators = true;

    #endregion

    #region Mob State

    /// <summary>
    /// Weapons mapped by attack style name.
    /// SDK Reference: Mob.weapons in Mob.ts
    /// </summary>
    protected Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>();

    /// <summary>
    /// Current attack style being used.
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

        // Build weapons dictionary from Inspector assignments
        BuildWeaponsDictionary();
    }

    void Start()
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
    }

    #endregion

    #region Weapons Setup

    /// <summary>
    /// Build weapons dictionary from Inspector-assigned weapons.
    /// SDK Reference: Mob.weapons in Mob.ts (but we populate from Inspector)
    /// </summary>
    private void BuildWeaponsDictionary()
    {
        weapons.Clear();

        // Add melee weapon under all melee styles
        if (meleeWeapon != null)
        {
            weapons["slash"] = meleeWeapon;
            weapons["crush"] = meleeWeapon;
            weapons["stab"] = meleeWeapon;
        }

        // Add ranged weapon
        if (rangedWeapon != null)
        {
            weapons["range"] = rangedWeapon;
        }

        // Add magic weapon
        if (magicWeapon != null)
        {
            weapons["magic"] = magicWeapon;
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
        // Default: use whatever is available
        if (meleeWeapon != null) return "slash";
        if (rangedWeapon != null) return "range";
        if (magicWeapon != null) return "magic";
        
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
    /// 
    /// LOGIC:
    /// 1. Calculate direction toward player (sign of dx/dy)
    /// 2. If player is UNDER mob → Random movement
    /// 3. If can reach diagonally → Move diagonal
    /// 4. Otherwise → Move on one axis only (allows corner safespotting)
    /// 5. No movement if just used melee special (attackDelay > attackSpeed)
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
            // SDK Reference: Mob.ts lines 113-130
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
            // SDK Reference: Mob.ts lines 131-133
            dy = gridPosition.y;
        }

        // No movement right after melee special attack (8 ticks)
        // SDK Reference: Mob.ts lines 135-139
        if (attackDelay > GetAttackSpeed())
        {
            dx = gridPosition.x;
            dy = gridPosition.y;
        }

        return new Vector2Int(dx, dy);
    }

    /// <summary>
    /// Get tiles to check for X-axis movement.
    /// SDK Reference: Mob.getXMovementTiles() in Mob.ts lines 155-170
    /// </summary>
    protected List<Vector2Int> GetXMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // Determine Y range based on yOff
        int start = yOff == -1 ? -1 : 0;
        int end = yOff == 1 ? size + 1 : size;

        if (xOff == -1)
        {
            // Moving west
            for (int i = start; i < end; i++)
            {
                tiles.Add(new Vector2Int(gridPosition.x - 1, gridPosition.y - i));
            }
        }
        else if (xOff == 1)
        {
            // Moving east
            for (int i = start; i < end; i++)
            {
                tiles.Add(new Vector2Int(gridPosition.x + size, gridPosition.y - i));
            }
        }

        return tiles;
    }

    /// <summary>
    /// Get tiles to check for Y-axis movement.
    /// SDK Reference: Mob.getYMovementTiles() in Mob.ts lines 172-187
    /// </summary>
    protected List<Vector2Int> GetYMovementTiles(int xOff, int yOff)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // Determine X range based on xOff
        int start = xOff == -1 ? -1 : 0;
        int end = xOff == 1 ? size + 1 : size;

        if (yOff == -1)
        {
            // Moving south
            for (int i = start; i < end; i++)
            {
                tiles.Add(new Vector2Int(gridPosition.x + i, gridPosition.y + 1));
            }
        }
        else if (yOff == 1)
        {
            // Moving north
            for (int i = start; i < end; i++)
            {
                tiles.Add(new Vector2Int(gridPosition.x + i, gridPosition.y - size));
            }
        }

        return tiles;
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

        // Don't move if can't move or no aggro
        if (!CanMove() || aggro == null)
            return;

        // Get target tile
        Vector2Int targetTile = GetNextMovementStep();
        
        int xOff = targetTile.x - gridPosition.x;
        int yOff = gridPosition.y - targetTile.y; // Note: inverted for OSRS coords

        // Get tiles needed for movement
        List<Vector2Int> xTiles = GetXMovementTiles(xOff, yOff);
        List<Vector2Int> yTiles = GetYMovementTiles(xOff, yOff);

        // Check if tiles are walkable
        // CRITICAL: Pass 'this' so mob checks collision with other mobs
        bool xSpace = xTiles.All(tile => 
            Pathing.CanTileBePathedTo(tile, 1, this));
        bool ySpace = yTiles.All(tile => 
            Pathing.CanTileBePathedTo(tile, 1, this));

        bool both = xSpace && ySpace;

        // If diagonal movement blocked, try single-axis
        if (!both)
        {
            xTiles = GetXMovementTiles(xOff, 0);
            xSpace = xTiles.All(tile => 
                Pathing.CanTileBePathedTo(tile, 1, this));

            if (!xSpace)
            {
                yTiles = GetYMovementTiles(0, yOff);
                ySpace = yTiles.All(tile => 
                    Pathing.CanTileBePathedTo(tile, 1, this));
            }
        }

        // Execute movement
        if (both)
        {
            // Can move diagonally
            gridPosition = targetTile;
        }
        else if (xSpace)
        {
            // Can only move on X axis
            gridPosition.x = targetTile.x;
        }
        else if (ySpace)
        {
            // Can only move on Y axis
            gridPosition.y = targetTile.y;
        }
        // Else: Can't move at all (blocked)
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

        // Tick down combat timers (handled in base.AttackStep)
        // frozen--;
        // stunned--;
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

        // Check if weapon is area attack (doesn't require LOS)
        bool weaponIsAreaAttack = false;
        if (weapons.ContainsKey(attackStyle) && weapons[attackStyle] != null)
        {
            // Area attacks can hit even if player is under mob
            weaponIsAreaAttack = false; // We don't have area attacks yet
        }

        // Check if player is under mob
        bool isUnderAggro = false;
        if (!weaponIsAreaAttack)
        {
            isUnderAggro = Collision.CollisionMath(
                gridPosition.x, gridPosition.y, size,
                aggro.gridPosition.x, aggro.gridPosition.y, 1);
        }

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

    #region Animation

    /// <summary>
    /// Attack animation: pulse to 120% scale.
    /// SDK Reference: Not in SDK (Unity-specific visual)
    /// </summary>
    protected override System.Collections.IEnumerator AttackAnimationCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f; // 120% pulse

        // Scale up over 0.15 seconds
        float elapsed = 0;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / 0.15f);
            yield return null;
        }

        // Scale down over 0.15 seconds
        elapsed = 0;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / 0.15f);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Death animation: fade to 10% scale.
    /// SDK Reference: Not in SDK (Unity-specific visual)
    /// </summary>
    public override void Dead()
    {
        base.Dead();
        
        // Start death fade animation
        StartCoroutine(DeathFadeCoroutine());
    }

    private System.Collections.IEnumerator DeathFadeCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 0.1f; // 10% scale

        float duration = GetDeathAnimationLength() * 0.6f; // Ticks to seconds
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, percent);
            
            // Also fade out children (if they have renderers)
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Color color = renderer.material.color;
                color.a = 1f - percent; // Fade out alpha
                renderer.material.color = color;
            }
            
            yield return null;
        }

        transform.localScale = targetScale;
    }

    #endregion
}
