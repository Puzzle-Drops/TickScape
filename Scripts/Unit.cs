using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all combat units (Players and Mobs).
/// Extends Entity with stats, combat mechanics, and equipment.
/// Matches SDK's Unit.ts structure.
/// SDK Reference: Unit.ts
/// </summary>
public abstract class Unit : Entity
{
    [Header("Stats")]
    [Tooltip("Base stats (never changes)")]
    public UnitStats stats;

    [Tooltip("Current stats (can be boosted/drained/damaged)")]
    public UnitStats currentStats;

    [Header("Combat State")]
    [Tooltip("Unit this is fighting (null = not in combat)")]
    public Unit aggro;

    [Tooltip("Ticks until next attack (-1 or 0 = can attack)")]
    public int attackDelay = 0;

    [Tooltip("Frozen timer (ticks remaining)")]
    public int frozen = 0;

    [Tooltip("Stunned timer (ticks remaining)")]
    public int stunned = 0;

    [Tooltip("Does this unit have line of sight to target?")]
    public bool hasLOS = false;

    [Header("Prayer System")]
    [Tooltip("Prayer controller for this unit")]
    public PrayerController prayerController;

    [Header("Combat Tracking")]
    [Tooltip("Visual position for smooth interpolation")]
    public Vector2 perceivedLocation;

    [Tooltip("Incoming projectiles that will damage this unit")]
    public List<Projectile> incomingProjectiles = new List<Projectile>();

    [Tooltip("XP multiplier for this NPC (default 1.0)")]
    public float xpBonusMultiplier = 1.0f;

    [Header("Movement Blocking")]
    [Tooltip("Does this unit block OTHER MOBS from moving through it? (Players always walk through mobs)")]
    public bool consumesSpace = true;

    [Header("Equipment & Bonuses")]
    [Tooltip("Calculated equipment bonuses")]
    public UnitBonuses bonuses;

    [Header("Behavior")]
    [Tooltip("Auto-retaliate when attacked?")]
    public bool autoRetaliate = true;

    [Header("Visual Feedback")]
    [Tooltip("Queue of damage numbers to display")]
    public List<Hitsplat> hitsplatQueue = new List<Hitsplat>();

    [Tooltip("Sound to play when hit")]
    public AudioClip hitSound;

    [Tooltip("Audio source for combat sounds")]
    private AudioSource audioSource;

    /// Last unit that interacted with this unit.
    public Unit lastInteraction;

    /// Ticks since last interaction (0 = this tick).
    public int lastInteractionAge = 0;

    // Internal state
    protected int spawnDelay = 0;



    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // Initialize stats
        SetStats();

        // Clone base stats to current stats
        if (stats != null)
        {
            currentStats = stats.Clone();
            currentStats.hitpoint = stats.hitpoint; // Start at full HP
        }

        // Initialize empty bonuses
        bonuses = UnitBonuses.Empty();

        // Initialize perceived location to grid position
        perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);

        // Initialize projectile list
        incomingProjectiles = new List<Projectile>();

        // Initialize hitsplat queue
        hitsplatQueue = new List<Hitsplat>();

        // ADD THIS NEW CODE:
        // Add overhead prayer renderer if this unit can pray
        // This allows both players and mobs to display overhead prayers
        StartCoroutine(AddRenderersWhenReady());
    }

    /// <summary>
    /// Add combat renderers after initialization.
    /// Allows both players and mobs to display prayers, projectiles, and hitsplats.
    /// </summary>
    private System.Collections.IEnumerator AddRenderersWhenReady()
    {
        // Wait one frame for derived classes to initialize
        yield return null;

        // Add projectile renderer (all units can receive projectiles)
        if (GetComponent<ProjectileRenderer>() == null)
        {
            gameObject.AddComponent<ProjectileRenderer>();
        }

        // Add hitsplat renderer (all units show damage numbers)
        if (GetComponent<HitsplatRenderer>() == null)
        {
            gameObject.AddComponent<HitsplatRenderer>();
        }

        // Only add prayer renderer if unit has prayer capability
        if (prayerController != null && GetComponent<OverheadPrayerRenderer>() == null)
        {
            gameObject.AddComponent<OverheadPrayerRenderer>();
        }
    }

    #endregion

    #region Tick System (SDK: Unit.ts lines 260-268)

    /// <summary>
    /// Called FIRST each tick, before movement.
    /// SDK Reference: Unit.timerStep() in Unit.ts
    /// </summary>
    public virtual void TimerStep()
    {
        // Override in subclasses for pre-movement logic
        if (lastInteraction != null)
        {
            lastInteractionAge++;
        }
    }

    /// <summary>
    /// Called SECOND each tick, handles movement.
    /// SDK Reference: Unit.movementStep() in Unit.ts
    /// </summary>
    public virtual void MovementStep()
    {
        // Override in subclasses for movement logic
    }

    /// <summary>
    /// Called THIRD each tick, handles combat and death.
    /// SDK Reference: Unit.attackStep() in Unit.ts
    /// </summary>
    public virtual void AttackStep()
    {
        // Tick down combat timers
        attackDelay--;
        frozen--;
        stunned--;

        // Process incoming attacks BEFORE death check
        ProcessIncomingAttacks();

        // Check for death BEFORE trying to attack
        DetectDeath();

        // Don't attack if dying
        if (IsDying())
            return;

        // Check if we can attack our target
        if (CanAttack() && aggro != null && attackDelay <= 0)
        {
            // Update line of sight
            SetHasLOS();

            // Attack if we have line of sight and target isn't dying
            if (hasLOS && !aggro.IsDying())
            {
                PerformAttack();
                DidAttack();
            }
        }
    }
    #endregion

    #region Stats Management

    /// <summary>
    /// Set base stats for this unit. Override in subclasses.
    /// SDK Reference: Unit.setStats() in Unit.ts
    /// </summary>
    public abstract void SetStats();

    #endregion

    #region Combat State Queries

    /// <summary>
    /// Can this unit move? (Not frozen, stunned, or dying)
    /// SDK Reference: Unit.canMove() in Unit.ts
    /// </summary>
    public bool CanMove()
    {
        return !hasLOS && !IsFrozen() && !IsStunned() && !IsDying();
    }

    /// <summary>
    /// Can this unit attack? (Not stunned or dying)
    /// SDK Reference: Unit.canAttack() in Unit.ts
    /// </summary>
    public bool CanAttack()
    {
        return !IsDying() && !IsStunned();
    }

    /// <summary>
    /// Is this unit frozen?
    /// SDK Reference: Unit.isFrozen() in Unit.ts
    /// </summary>
    public bool IsFrozen()
    {
        return frozen > 0;
    }

    /// <summary>
    /// Is this unit stunned?
    /// SDK Reference: Unit.isStunned() in Unit.ts
    /// </summary>
    public bool IsStunned()
    {
        return stunned > 0;
    }

    /// <summary>
    /// Is this unit in death animation?
    /// SDK Reference: Unit.isDying() in Unit.ts
    /// </summary>
    public new bool IsDying()
    {
        return dying > 0;
    }

    /// <summary>
    /// Should this unit be removed from the world?
    /// SDK Reference: Unit.shouldDestroy() in Unit.ts
    /// </summary>
    public new bool ShouldDestroy()
    {
        return dying == 0;
    }

    #endregion

    #region Line of Sight

    /// <summary>
    /// Update line of sight to current aggro target.
    /// Must be called before attack checks.
    /// </summary>
    public void SetHasLOS()
    {
        if (aggro == null)
        {
            hasLOS = false;
            return;
        }

        // Check line of sight based on unit type
        if (this is Player)
        {
            hasLOS = LineOfSight.PlayerHasLineOfSightOfMob(gridPosition, aggro, GetAttackRange());
        }
        else
        {
            // NPC line of sight (when we add NPCs)
            hasLOS = LineOfSight.MobHasLineOfSightOfPlayer(this, aggro.gridPosition, GetAttackRange());
        }
    }

    /// <summary>
    /// Check if unit is within melee range of target.
    /// Used by NPCs to decide between melee and ranged attacks.
    /// </summary>
    public bool IsWithinMeleeRange()
    {
        if (aggro == null) return false;

        return Collision.CollisionMath(
            gridPosition.x, gridPosition.y, size,
            aggro.gridPosition.x, aggro.gridPosition.y, aggro.size
        );
    }

    #endregion

    #region Attack Execution

    /// <summary>
    /// Perform attack against current aggro target.
    /// Override in Player/Mob for specific attack logic.
    /// </summary>
    public abstract void PerformAttack();

    /// <summary>
    /// Called after successful attack to reset attack delay.
    /// SDK Reference: Unit.didAttack() in Unit.ts
    /// </summary>
    public void DidAttack()
    {
        attackDelay = GetAttackSpeed();

        // Trigger attack animation (scale pulse)
        StartAttackAnimation();
    }

    /// <summary>
    /// Get attack range for this unit.
    /// Override in Player to check weapon.
    /// </summary>
    public virtual int GetAttackRange()
    {
        return 1; // Default melee range
    }

    /// <summary>
    /// Visual attack animation (scale pulse).
    /// </summary>
    protected void StartAttackAnimation()
    {
        // Simple scale pulse animation
        StartCoroutine(AttackAnimationCoroutine());
    }

    protected virtual System.Collections.IEnumerator AttackAnimationCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.25f;

        // Scale up over 0.1 seconds
        float elapsed = 0;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / 0.1f);
            yield return null;
        }

        // Scale down over 0.1 seconds
        elapsed = 0;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / 0.1f);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    #endregion

    #region Combat Actions

    /// <summary>
    /// Set aggro target (start combat).
    /// SDK Reference: Unit.setAggro() in Unit.ts
    /// </summary>
    public virtual void SetAggro(Unit target)
    {
        aggro = target;
    }

    /// <summary>
    /// Grant XP to this unit (only Players actually track XP).
    /// SDK Reference: Unit.grantXp() in Unit.ts lines 282-284
    /// </summary>
    public virtual void GrantXp(XpDrop xpDrop)
    {
        // Override in Player class
    }

    /// <summary>
    /// Add incoming projectile to this unit.
    /// SDK Reference: Unit.addProjectile() in Unit.ts lines 314-318
    /// </summary>
    public void AddProjectile(Projectile projectile)
    {
        // Auto-retaliate if not already in combat
        if (spawnDelay > 0 && autoRetaliate && aggro == null)
        {
            SetAggro(projectile.from);
        }
        incomingProjectiles.Add(projectile);
    }

    /// <summary>
    /// Process all incoming projectiles and apply damage.
    /// SDK Reference: Unit.processIncomingAttacks() in Unit.ts lines 352-378
    /// </summary>
    public void ProcessIncomingAttacks()
    {
        // Remove destroyed projectiles
        incomingProjectiles.RemoveAll(p => p.ShouldDestroy());

        // Process each projectile
        foreach (var projectile in incomingProjectiles)
        {
            projectile.OnTick();

            if (projectile.remainingDelay == 0)
            {
                // Apply damage
                projectile.BeforeHit();

                // Check if attack was cancelled (e.g., attacker died)
                if (projectile.options.cancelOnDeath && projectile.from != null && projectile.from.IsDying())
                {
                    continue;
                }

                // Apply damage or healing
                if (projectile.damage < 0)
                {
                    // Healing
                    if (currentStats.hitpoint < stats.hitpoint)
                    {
                        currentStats.hitpoint -= projectile.damage; // Subtracting negative = healing
                        currentStats.hitpoint = Mathf.Min(currentStats.hitpoint, stats.hitpoint);

                        // Add green hitsplat for healing
                        AddHitsplat(Mathf.Abs(projectile.damage), new Color(0, 0.8f, 0));
                    }
                }
                else
                {
                    // Damage
                    currentStats.hitpoint -= projectile.damage;

                    // Add hitsplat
                    if (projectile.damage == 0)
                    {
                        // Blue for blocked hits
                        AddHitsplat(0, new Color(0, 0.5f, 1));
                    }
                    else
                    {
                        // Red for damage
                        AddHitsplat(projectile.damage, Color.red);
                    }

                    // Play hit sound if available
                    PlayHitSound();

                    // Check for Redemption prayer trigger
                    // SDK Reference: Player.damageTaken() triggers redemption check
                    if (prayerController != null)
                    {
                        prayerController.CheckRedemption(this);
                    }

                }

                // Auto-retaliate if not in combat (NPCs only, player has autoRetaliate = false)
                if (autoRetaliate && aggro == null && projectile.damage > 0)
                {
                    SetAggro(projectile.from);

                    // Reset attack delay for flinch
                    if (attackDelay < GetFlinchDelay() + 1)
                    {
                        attackDelay = GetFlinchDelay() + 1;
                    }
                }
            }
        }

        currentStats.hitpoint = Mathf.Max(0, currentStats.hitpoint);
    }

    /// <summary>
    /// Add hitsplat to display queue.
    /// </summary>
    public void AddHitsplat(int damage, Color color)
    {
        // Offset each new hitsplat slightly
        float yOffset = hitsplatQueue.Count * 0.3f;

        hitsplatQueue.Add(new Hitsplat
        {
            damage = damage,
            color = color,
            age = 0,
            offset = new Vector2(0, yOffset)
        });
    }

    /// <summary>
    /// Play hit sound effect if available.
    /// </summary>
    protected void PlayHitSound()
    {
        if (hitSound != null)
        {
            // Get or create audio source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1.0f; // 3D sound
                    audioSource.maxDistance = 20f;
                }
            }

            audioSource.PlayOneShot(hitSound, 0.5f);
        }
    }

    /// <summary>
    /// Get flinch delay (half of attack speed).
    /// SDK Reference: Unit.flinchDelay getter in Unit.ts
    /// </summary>
    public virtual int GetFlinchDelay()
    {
        return GetAttackSpeed() / 2;
    }

    /// <summary>
    /// Get attack speed for this unit.
    /// SDK Reference: Unit.attackSpeed getter in Unit.ts
    /// Override in subclasses.
    /// </summary>
    public virtual int GetAttackSpeed()
    {
        return 4; // Default 4 ticks
    }

    /// <summary>
    /// Freeze this unit for X ticks.
    /// SDK Reference: Unit.freeze() in Unit.ts
    /// </summary>
    public void Freeze(int ticks)
    {
        if (ticks <= frozen)
            return;

        frozen = ticks;
    }

    #endregion

    #region Death System

    /// <summary>
    /// Kill this unit (start death animation).
    /// SDK Reference: Unit.dead() in Unit.ts
    /// </summary>
    public virtual void Dead()
    {
        dying = GetDeathAnimationLength();
        aggro = null;

        Debug.Log($"{gameObject.name} has died!");
    }

    /// <summary>
    /// Check if unit should die (HP <= 0).
    /// SDK Reference: Unit.detectDeath() in Unit.ts
    /// </summary>
    protected void DetectDeath()
    {
        // Check if should start dying
        if (dying == -1 && currentStats.hitpoint <= 0)
        {
            Dead();
            return;
        }

        // Tick down death animation
        if (dying > 0)
        {
            dying--;
        }

        // Remove from world when animation complete
        if (dying == 0)
        {
            RemovedFromWorld();
        }
    }

    /// <summary>
    /// Get death animation length in ticks.
    /// SDK Reference: Unit.deathAnimationLength getter in Unit.ts
    /// </summary>
    protected virtual int GetDeathAnimationLength()
    {
        return 3; // Default 3 ticks (1.8 seconds)
    }

    /// <summary>
    /// Called when unit is removed from world (after death).
    /// SDK Reference: Unit.removedFromWorld() in Unit.ts
    /// </summary>
    protected virtual void RemovedFromWorld()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterEntity(this);
        }

        // Note: Don't call Destroy here - WorldManager handles that
        // This is just for cleanup logic
    }

    #endregion

    #region Coordinate Helpers

    /// <summary>
    /// Is this unit on the specified tile?
    /// Accounts for unit size (1x1, 2x2, etc).
    /// SDK Reference: Unit.isOnTile() in Unit.ts
    /// </summary>
    public new bool IsOnTile(Vector2Int pos)
    {
        return pos.x >= gridPosition.x &&
               pos.x < gridPosition.x + size &&
               pos.y >= gridPosition.y &&
               pos.y < gridPosition.y + size;
    }

    /// <summary>
    /// Get the closest tile on this unit to the specified point.
    /// Returns as array [x, y] to match SDK signature.
    /// SDK Reference: Entity.getClosestTileTo() in Entity.ts lines 62-66
    /// </summary>
    public int[] GetClosestTileTo(int x, int y)
    {
        int clampedX = Mathf.Clamp(x, gridPosition.x, gridPosition.x + size - 1);
        int clampedY = Mathf.Clamp(y, gridPosition.y - size + 1, gridPosition.y);
        return new int[] { clampedX, clampedY };
    }

    // Keep the Vector2Int version for internal use
    public Vector2Int GetClosestTileToVector(Vector2Int target)
    {
        int clampedX = Mathf.Clamp(target.x, gridPosition.x, gridPosition.x + size - 1);
        // UNITY FIX: Y+ = North, so entity occupies [y, y+size-1], not [y-size+1, y]
        int clampedY = Mathf.Clamp(target.y, gridPosition.y, gridPosition.y + size - 1);
        return new Vector2Int(clampedX, clampedY);
    }

    /// <summary>
    /// Get perceived location with interpolation.
    /// SDK Reference: Unit.getPerceivedLocation() in Unit.ts lines 382-387
    /// </summary>
    public virtual Vector2 GetPerceivedLocation(float tickPercent)
    {
        float perceivedX = Pathing.LinearInterpolation(perceivedLocation.x, gridPosition.x, tickPercent);
        float perceivedY = Pathing.LinearInterpolation(perceivedLocation.y, gridPosition.y, tickPercent);
        return new Vector2(perceivedX, perceivedY);
    }

    #endregion

    #region Combat Level (OSRS Formula)

    /// <summary>
    /// Calculate combat level using OSRS formula.
    /// SDK Reference: Unit.combatLevel getter in Unit.ts
    /// </summary>
    public virtual int CombatLevel
    {
        get
        {
            float baseLevel = 0.25f * (stats.defence + stats.hitpoint + Mathf.Floor((stats.prayer) * 0.5f));
            float melee = (13f / 40f) * (stats.attack + stats.strength);
            float range = (13f / 40f) * Mathf.Floor(stats.range * (3f / 2f));
            float mage = (13f / 40f) * Mathf.Floor(stats.magic * (3f / 2f));

            return Mathf.FloorToInt(baseLevel + Mathf.Max(melee, Mathf.Max(range, mage)));
        }
    }

    #endregion

    #region Debug Helpers

    /// <summary>
    /// Get display name for this unit.
    /// Override in subclasses (Player, Mob).
    /// </summary>
    public virtual string UnitName()
    {
        return gameObject.name;
    }

    #endregion

}



