using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test unit that attacks with ranged projectiles.
/// Uses OSRS-accurate NPC movement.
/// </summary>
public class RangedTestUnit : Unit
{
    [Header("Ranged Attack Settings")]
    [Tooltip("Attack range in tiles")]
    public int attackRange = 8;

    [Tooltip("Max hit for ranged attacks")]
    public int maxHit = 15;

    [Tooltip("Attack speed in ticks")]
    public int attackSpeedTicks = 4;

    [Tooltip("Projectile color")]
    public Color projectileColor = Color.green;

    [Header("Behavior Settings")]
    [Tooltip("Should this unit move toward target when out of range?")]
    public bool canMove = true;

    [Tooltip("Automatically aggro the player on spawn?")]
    public bool autoAggroPlayer = true;

    [Tooltip("Ticks to wait before auto-aggro")]
    public int aggroDelayTicks = 2;

    [Header("Ranged Attack Settings")]
    public GameObject arrowPrefab; // Assign in Inspector!

    private MeshRenderer meshRenderer;
    private int aggroTimer = 0;
    private bool hasAggroed = false;

    void Start()
    {
        // Initialize visual position to match starting grid position
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GridToWorld(gridPosition);
            GridManager.Instance.RegisterEntity(this);
            Debug.Log($"RangedTestUnit spawned at {gridPosition} with {currentStats.hitpoint} HP");
        }

        // Set visual color
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = new Color(0.5f, 1f, 0.5f); // Light green for archer
        }

        // Initialize prayer controller
        prayerController = new PrayerController(this);

        // Initialize aggro timer if auto-aggro is enabled
        if (autoAggroPlayer)
        {
            aggroTimer = aggroDelayTicks;
        }

        // In RangedTestUnit.cs Start() or Player.cs Start()
        if (GetComponent<ProjectileRenderer>() == null)
        {
            gameObject.AddComponent<ProjectileRenderer>();
        }
    }

    /// <summary>
    /// Set stats for this ranged unit.
    /// </summary>
    public override void SetStats()
    {
        stats = new UnitStats
        {
            attack = 1,      // Not used for ranged
            strength = 1,    // Not used for ranged
            defence = 50,
            range = 75,      // Good range level
            magic = 1,
            hitpoint = 60,   // Lower HP than melee unit
            prayer = 0       // NPCs don't use prayer
        };
    }

    /// <summary>
    /// Get attack range for this unit.
    /// </summary>
    public override int GetAttackRange()
    {
        return attackRange;
    }

    /// <summary>
    /// Get attack speed for this unit.
    /// </summary>
    public override int GetAttackSpeed()
    {
        return attackSpeedTicks;
    }

    /// <summary>
    /// Timer step - handle auto-aggro.
    /// </summary>
    public override void TimerStep()
    {
        base.TimerStep();

        // Handle auto-aggro timer
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

    /// <summary>
    /// Perform ranged attack.
    /// </summary>
    public override void PerformAttack()
    {
        if (aggro == null || aggro.IsDying())
            return;

        int damage = Random.Range(0, maxHit + 1);

        ProjectileOptions options = new ProjectileOptions
        {
            hidden = false,
            size = 0.3f,
            color = projectileColor,
            visualDelayTicks = 0,
            visualHitEarlyTicks = 0,
            motionInterpolator = new ArcProjectileMotion(1.5f),
            modelPrefab = arrowPrefab
        };

        Projectile arrow = new Projectile(null, damage, this, aggro, "range", options);

    // Calculate proper delay based on distance
    int distance = Mathf.Max(
            Mathf.Abs(aggro.gridPosition.x - gridPosition.x),
            Mathf.Abs(aggro.gridPosition.y - gridPosition.y)
        );
        arrow.remainingDelay = Mathf.FloorToInt((3 + distance) / 6f) + 1;
        arrow.totalDelay = arrow.remainingDelay;

        aggro.AddProjectile(arrow);
    }

    /// <summary>
    /// Movement using OSRS NPC movement rules.
    /// SDK Reference: Mob.movementStep() in Mob.ts
    /// </summary>
    public override void MovementStep()
    {
        base.MovementStep();

        if (IsDying() || !canMove || aggro == null)
            return;

        // For ranged units, check if we need to move
        int distance = Mathf.Max(
            Mathf.Abs(aggro.gridPosition.x - gridPosition.x),
            Mathf.Abs(aggro.gridPosition.y - gridPosition.y)
        );

        // Only move if out of range or no LOS
        if (distance > attackRange || !hasLOS)
        {
            // Get next movement position OSRS-style
            Vector2Int nextMove = GetNextMovementStep();

            // Check if we can actually move there
            if (nextMove != gridPosition)
            {
                if (Collision.IsTileWalkable(nextMove, size, this))
                {
                    Vector2Int oldPos = gridPosition;
                    gridPosition = nextMove;

                    // Update perceived location for smooth interpolation
                    perceivedLocation = new Vector2(oldPos.x, oldPos.y);

                    Debug.Log($"[NPC] Moved from {oldPos} to {gridPosition} (distance: {distance})");
                }
            }
        }
    }

    /// <summary>
    /// Get next movement step using OSRS NPC movement logic.
    /// SDK Reference: Mob.getNextMovementStep() in Mob.ts lines 147-184
    /// </summary>
    private Vector2Int GetNextMovementStep()
    {
        if (aggro == null)
            return gridPosition;

        // Calculate desired movement (toward player)
        int dx = gridPosition.x + System.Math.Sign(aggro.gridPosition.x - gridPosition.x);
        int dy = gridPosition.y + System.Math.Sign(aggro.gridPosition.y - gridPosition.y);

        // Check if we're on top of the player (collision)
        bool onTopOfPlayer = Collision.CollisionMath(
            gridPosition.x, gridPosition.y, size,
            aggro.gridPosition.x, aggro.gridPosition.y, 1
        );

        if (onTopOfPlayer)
        {
            // Random movement when under/on player
            if (Random.Range(0f, 1f) < 0.5f)
            {
                dy = gridPosition.y;
                dx = Random.Range(0f, 1f) < 0.5f ? gridPosition.x + 1 : gridPosition.x - 1;
            }
            else
            {
                dx = gridPosition.x;
                dy = Random.Range(0f, 1f) < 0.5f ? gridPosition.y + 1 : gridPosition.y - 1;
            }
        }
        else
        {
            // Check if diagonal movement would hit the player
            // This allows "corner safespotting" - OSRS mechanic!
            bool diagonalHitsPlayer = Collision.CollisionMath(
                dx, dy, size,
                aggro.gridPosition.x, aggro.gridPosition.y, 1
            );

            if (diagonalHitsPlayer)
            {
                // Don't move diagonally - just move east/west
                dy = gridPosition.y;
            }
        }

        // Don't move if we just attacked (for melee units)
        // Ranged units can move immediately after attacking

        return new Vector2Int(dx, dy);
    }

    /// <summary>
    /// Make this unit aggressive toward the player.
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
                Debug.Log("[RANGED UNIT] Now aggressive to player!");
            }
            else
            {
                Debug.LogWarning("[RANGED UNIT] Found player object but no Player component!");
            }
        }
        else
        {
            Debug.LogWarning("[RANGED UNIT] Could not find player in scene!");
        }
    }

    public override string UnitName()
    {
        return "Ranged Test Unit";
    }

    /// <summary>
    /// Draw debug info in scene view.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if (GridManager.Instance != null)
        {
            Vector3 center = GridManager.Instance.GridToWorld(gridPosition);
            center += new Vector3(0.5f, 0, 0.5f);

            // Draw range circle (approximate with a square for now)
            for (int x = -attackRange; x <= attackRange; x++)
            {
                for (int y = -attackRange; y <= attackRange; y++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) == attackRange)
                    {
                        Vector3 pos = center + new Vector3(x, 0, y);
                        Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
                    }
                }
            }
        }

        // Draw line to target
        if (aggro != null && GridManager.Instance != null)
        {
            Gizmos.color = hasLOS ? Color.green : Color.red;
            Vector3 myPos = GridManager.Instance.GridToWorld(gridPosition) + new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 targetPos = GridManager.Instance.GridToWorld(aggro.gridPosition) + new Vector3(0.5f, 0.5f, 0.5f);
            Gizmos.DrawLine(myPos, targetPos);
        }
    }
}