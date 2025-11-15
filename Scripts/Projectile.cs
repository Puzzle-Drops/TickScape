using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Projectile motion interpolator for different travel patterns.
/// SDK Reference: ProjectileMotionInterpolator in Projectile.ts
/// </summary>
public interface IProjectileMotionInterpolator
{
    Vector3 Interpolate(Vector3 from, Vector3 to, float percent);
    float InterpolatePitch(Vector3 from, Vector3 to, float percent);
}

/// <summary>
/// Linear projectile motion (straight line).
/// SDK Reference: LinearProjectileMotionInterpolator in Projectile.ts
/// </summary>
public class LinearProjectileMotion : IProjectileMotionInterpolator
{
    public Vector3 Interpolate(Vector3 from, Vector3 to, float percent)
    {
        return Vector3.Lerp(from, to, percent);
    }

    public float InterpolatePitch(Vector3 from, Vector3 to, float percent)
    {
        return 0f;
    }
}

/// <summary>
/// Arc projectile motion (curved path, like arrows).
/// SDK Reference: ArcProjectileMotionInterpolator in Projectile.ts
/// </summary>
public class ArcProjectileMotion : IProjectileMotionInterpolator
{
    private float height;

    public ArcProjectileMotion(float arcHeight)
    {
        this.height = arcHeight;
    }

    public Vector3 Interpolate(Vector3 from, Vector3 to, float percent)
    {
        Vector3 linear = Vector3.Lerp(from, to, percent);

        // Add arc height using sine wave
        float arcOffset = Mathf.Sin(percent * Mathf.PI) * height;
        linear.y += arcOffset;

        return linear;
    }

    public float InterpolatePitch(Vector3 from, Vector3 to, float percent)
    {
        // Pitch follows arc derivative
        return Mathf.Sin(-(0.75f + percent * 0.5f) * Mathf.PI);
    }
}

/// <summary>
/// Projectile options for customizing behavior.
/// SDK Reference: ProjectileOptions in Projectile.ts
/// </summary>
[System.Serializable]
public class ProjectileOptions
{
    public bool hidden = false;
    public bool checkPrayerAtHit = false;
    public int setDelay = -1;  // -1 means use calculated delay
    public int reduceDelay = 0;
    public bool cancelOnDeath = false;
    public float size = 0.5f;
    public Color color = Color.black;
    public IProjectileMotionInterpolator motionInterpolator = null;
    public int visualDelayTicks = 0;
    public int visualHitEarlyTicks = 0;
    public AudioClip attackSound = null;
    public AudioClip projectileSound = null;
    public AudioClip hitSound = null;
    public GameObject modelPrefab = null;
    public float modelScale = 1.0f;
    public float verticalOffset = 0f;
}

/// <summary>
/// Projectile that travels from attacker to target and deals damage.
/// SDK Reference: Projectile.ts
/// 
/// LIFECYCLE:
/// 1. Created by Weapon.RegisterProjectile()
/// 2. Travels over multiple ticks (remainingDelay)
/// 3. Lands and applies damage (beforeHit -> damage applied)
/// 4. Destroyed after hit
/// </summary>
public class Projectile
{
    // Weapon that fired this projectile (can be null for melee)
    private Weapon weapon;

    // Damage to deal
    public int damage;

    // Source and target
    public Unit from;
    public Unit to;

    // Attack style ("melee", "range", "magic", "heal", "recoil")
    public string attackStyle;

    // Travel state
    public int remainingDelay;
    public int totalDelay;
    public int age = 0;
    public float distance;

    // Separate tracking for visual and hit timing
    public int hitDelay;          // When damage should apply (countdown)
    public int visualTotalDelay;  // Total ticks for visual interpolation

    // Position tracking
    public Vector2 startLocation;
    public Vector2 currentLocation;
    public float currentHeight;

    // Options
    public ProjectileOptions options;

    // Motion interpolator
    private IProjectileMotionInterpolator interpolator;

    // Hitsplat positioning (for stacking multiple hitsplats)
    public float offsetX = 0;
    public float offsetY = 0;

    // Visual representation (self-managed)
    private GameObject visualInstance;
    private bool hasSpawnedVisual = false;

    // Tracking
    public bool createdThisTick = true;  // Prevents processing on creation frame

    /// <summary>
    /// Create a projectile.
    /// SDK Reference: Projectile constructor in Projectile.ts lines 88-165
    /// </summary>
    public Projectile(
        Weapon weapon,
        int damage,
        Unit from,
        Unit to,
        string attackStyle,
        ProjectileOptions options = null)
    {
        this.weapon = weapon;
        this.damage = Mathf.FloorToInt(damage);
        this.from = from;
        this.to = to;
        this.attackStyle = attackStyle;
        this.options = options ?? new ProjectileOptions();

        // Clamp damage to target's current HP
        if (this.damage > to.currentStats.hitpoint)
        {
            this.damage = to.currentStats.hitpoint;
        }

        // Initialize start location (center of attacker's VISUAL position)
        // Using perceived location so projectile spawns where attacker LOOKS
        Vector2 fromPerceivedStart = from.GetPerceivedLocation(0);
        this.startLocation = new Vector2(
            fromPerceivedStart.x + (from.size - 1) / 2f,
            fromPerceivedStart.y + (from.size - 1) / 2f
        );
        this.currentLocation = this.startLocation;
        this.currentHeight = 0.75f + this.options.verticalOffset; // Projectile origin height

        // Calculate distance using Chebyshev distance
        this.distance = 0;

        if (weapon == null || IsMeleeStyle())
        {
            // Melee hits instantly
            this.distance = 0;
            this.remainingDelay = 1;
        }
        else
        {
            // Calculate Chebyshev distance
            Vector2Int closestTileFrom = from.GetClosestTileToVector(to.gridPosition);
            Vector2Int closestTileTo = to.GetClosestTileToVector(from.gridPosition);

            int dx = Mathf.Abs(closestTileTo.x - closestTileFrom.x);
            int dy = Mathf.Abs(closestTileTo.y - closestTileFrom.y);
            this.distance = Mathf.Max(dx, dy);

            // Calculate hit delay based on weapon type
            this.remainingDelay = weapon.CalculateHitDelay((int)this.distance);

            // Players get +1 delay
            if (from is Player)
            {
                this.remainingDelay++;
            }

            // Apply delay modifiers
            if (this.options.reduceDelay > 0)
            {
                this.remainingDelay -= this.options.reduceDelay;
                if (this.remainingDelay < 1)
                {
                    this.remainingDelay = 1;
                }
            }
        }

        // Override delay if specified
        if (this.options.setDelay >= 0)
        {
            this.remainingDelay = this.options.setDelay;
        }

        // CRITICAL: Track hit and visual timing separately
        // hitDelay = when damage applies (should match visual impact!)
        // totalDelay = total lifetime including visual delay
        // visualTotalDelay = just the travel time for interpolation
        this.visualTotalDelay = this.remainingDelay;  // For visual interpolation
        this.totalDelay = this.remainingDelay + this.options.visualDelayTicks;  // Total lifetime
        this.hitDelay = this.totalDelay;  // Hit when visual journey completes!
        this.remainingDelay = this.totalDelay;  // Countdown from total

        Debug.Log($"[PROJECTILE] Created: age={age}, visualDelay={options.visualDelayTicks} ticks, " +
                  $"travelTime={visualTotalDelay} ticks, willAppearOnAge={options.visualDelayTicks}");

        // Set up motion interpolator
        if (this.options.motionInterpolator != null)
        {
            this.interpolator = this.options.motionInterpolator;
        }
        else
        {
            this.interpolator = new LinearProjectileMotion();
        }

        // Set color based on attack style
        if (this.options.color == Color.black)
        {
            this.options.color = GetDefaultColor();
        }

        // Play projectile sound (if delayed)
        if (this.options.projectileSound != null && this.options.visualDelayTicks > 0)
        {
            // Sound will play when age == visualDelayTicks
        }

        // Play attack sound immediately
        if (this.options.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(this.options.attackSound, from.transform.position, 0.3f);
        }

        // Spawn visual immediately if not hidden
        // This ensures projectile appears on the same frame it's created
        if (!this.options.hidden)
        {
            SpawnVisual();
        }
    }

    /// <summary>
    /// Get default color based on attack style.
    /// SDK Reference: Projectile.getColor() in Projectile.ts
    /// </summary>
    private Color GetDefaultColor()
    {
        switch (attackStyle)
        {
            case "melee": return new Color(1f, 0f, 0f); // Red
            case "range": return new Color(0f, 1f, 0f); // Green
            case "magic": return new Color(0f, 0f, 1f); // Blue
            case "heal": return new Color(0.59f, 0.07f, 0.66f); // Purple
            case "recoil": return Color.black;
            default: return Color.black;
        }
    }

    private bool IsMeleeStyle()
    {
        return attackStyle == "slash" || attackStyle == "crush" || attackStyle == "stab";
    }

    /// <summary>
    /// Get target destination (where projectile is traveling to).
    /// SDK Reference: Projectile.getTargetDestination() in Projectile.ts
    /// </summary>
    public Vector3 GetTargetDestination(float tickPercent)
    {
        Vector2 toPerceivedLoc = to.GetPerceivedLocation(tickPercent);
        float endHeight = 0.5f; // Target center height
        int targetSize = to.size;

        float x = toPerceivedLoc.x + (targetSize - 1) / 2f;
        float y = toPerceivedLoc.y + (targetSize - 1) / 2f;

        return new Vector3(x, endHeight, y);
    }

    /// <summary>
    /// Get current projectile position with interpolation.
    /// SDK Reference: Projectile.getPerceivedLocation() in Projectile.ts
    /// 
    /// CRITICAL: Use FROZEN startLocation (set at creation), not attacker's current position.
    /// This prevents the projectile from "jumping" when the attacker moves after firing.
    /// </summary>
    public Vector3 GetPerceivedLocation(float tickPercent)
    {
        // Use FROZEN start location (set once at projectile creation)
        // DO NOT use from.GetPerceivedLocation() - that changes as attacker moves!
        Vector3 start = new Vector3(startLocation.x, currentHeight, startLocation.y);

        // Target can still move - we track them
        Vector3 end = GetTargetDestination(tickPercent);

        float percent = GetPercent(tickPercent);

        return interpolator.Interpolate(start, end, percent);
    }

    /// <summary>
    /// Get interpolation percentage (0.0 to 1.0).
    /// SDK Reference: Projectile.getPercent()
    /// </summary>
    private float GetPercent(float tickPercent)
    {
        // Not visible yet
        if (age < options.visualDelayTicks)
        {
            return 0f;
        }

        // Calculate travel progress after visual delay
        float numerator = (age - options.visualDelayTicks) + tickPercent;
        float denominator = visualTotalDelay - options.visualHitEarlyTicks;

        // Special case for instant/melee projectiles
        if (denominator <= 0)
        {
            return 1f;  // Instant arrival
        }

        float result = numerator / denominator;
        return Mathf.Clamp01(result);
    }

    /// <summary>
    /// Is this projectile visible?
    /// SDK Reference: Projectile.visible()
    /// </summary>
    public bool IsVisible(float tickPercent)
    {
        // Simple check: age must be >= visualDelayTicks
        if (age < options.visualDelayTicks)
        {
            return false;
        }

        // Visible while traveling
        float percent = GetPercent(tickPercent);
        return percent >= 0 && percent <= 1;
    }

    /// <summary>
    /// Tick projectile forward one game tick.
    /// SDK Reference: Projectile.onTick() in Projectile.ts
    /// </summary>
    public void OnTick()
    {
        // Skip the first tick to prevent processing on creation frame
        if (createdThisTick)
        {
            createdThisTick = false;
            return;  // Don't process anything on creation tick
        }

        remainingDelay--;
        hitDelay--;
        age++;

        // Play projectile sound when visible
        if (options.projectileSound != null && age == options.visualDelayTicks)
        {
            AudioSource.PlayClipAtPoint(options.projectileSound,
                from.transform.position, 0.3f);
        }
    }

    /// <summary>
    /// Called right before damage is applied.
    /// SDK Reference: Projectile.beforeHit() in Projectile.ts
    /// </summary>
    public void BeforeHit()
    {
        // Play hit sound
        if (options.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(options.hitSound, to.transform.position, 0.3f);
        }

        // Check prayer at hit time (for some attacks)
        if (options.checkPrayerAtHit && weapon != null)
        {
            if (weapon.IsBlockable(from, to, attackStyle))
            {
                damage = 0;
            }
        }
    }

    /// <summary>
    /// Should this projectile be destroyed?
    /// SDK Reference: Projectile.shouldDestroy() in Projectile.ts
    /// </summary>
    public bool ShouldDestroy()
    {
        // Destroy after total lifetime (includes visual delay)
        // Keep alive a bit longer if visualHitEarlyTicks is negative
        return remainingDelay <= Mathf.Min(0, options.visualHitEarlyTicks);
    }

    /// <summary>
    /// Get rotation angle toward target.
    /// SDK Reference: Projectile.getPerceivedRotation()
    /// 
    /// CRITICAL FIX: Use attacker's perceived location for consistency.
    /// </summary>
    public float GetPerceivedRotation(float tickPercent)
    {
        Vector2 fromPerceived = from.GetPerceivedLocation(tickPercent);
        Vector3 target = GetTargetDestination(tickPercent);
        return -Pathing.Angle(fromPerceived.x, fromPerceived.y, target.x, target.z);
    }

    #region Visual Management (Self-Contained)

    /// <summary>
    /// Spawn the visual representation of this projectile.
    /// Called automatically in constructor if not hidden.
    /// </summary>
    private void SpawnVisual()
    {
        if (hasSpawnedVisual || options.hidden)
            return;

        if (options.modelPrefab != null)
        {
            // Instantiate the projectile prefab
            visualInstance = GameObject.Instantiate(options.modelPrefab);
            visualInstance.name = $"Projectile_{attackStyle}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            // Apply scale
            visualInstance.transform.localScale = Vector3.one * options.modelScale;
        }
        else
        {
            // Fallback: create simple colored sphere if no prefab
            visualInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualInstance.name = $"Projectile_{attackStyle}_Fallback";
            visualInstance.transform.localScale = Vector3.one * options.size;

            // Apply color
            Renderer renderer = visualInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = options.color;
            }

            // Remove collider (projectiles shouldn't physically collide)
            Collider collider = visualInstance.GetComponent<Collider>();
            if (collider != null)
            {
                GameObject.Destroy(collider);
            }
        }

        hasSpawnedVisual = true;
    }

    /// <summary>
    /// Update visual position and rotation.
    /// Called every frame by ProjectileRenderer.
    /// </summary>
    public void UpdateVisual(float tickPercent)
    {
        // Spawn visual if not already spawned (safety check)
        if (!hasSpawnedVisual && !options.hidden)
        {
            SpawnVisual();
        }

        // Update only if visible and instance exists
        if (visualInstance != null && IsVisible(tickPercent))
        {
            // Get grid position
            Vector3 gridPos = GetPerceivedLocation(tickPercent);

            // Convert to Unity world space
            Vector3 worldPos = gridPos;
            if (GridManager.Instance != null)
            {
                worldPos = new Vector3(
                    gridPos.x * GridManager.Instance.tileSize,
                    gridPos.y,
                    gridPos.z * GridManager.Instance.tileSize
                );
            }

            visualInstance.transform.position = worldPos;

            // Rotate to face direction of travel
            float rotation = GetPerceivedRotation(tickPercent);
            visualInstance.transform.rotation = Quaternion.Euler(0, rotation * Mathf.Rad2Deg + 90f, 0);

            // Make sure it's active
            if (!visualInstance.activeSelf)
            {
                visualInstance.SetActive(true);
            }
        }
        else if (visualInstance != null && !IsVisible(tickPercent))
        {
            // Hide when not visible
            if (visualInstance.activeSelf)
            {
                visualInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Destroy the visual representation.
    /// Called when projectile is destroyed.
    /// </summary>
    public void DestroyVisual()
    {
        if (visualInstance != null)
        {
            GameObject.Destroy(visualInstance);
            visualInstance = null;
        }
        hasSpawnedVisual = false;
    }

    /// <summary>
    /// Check if visual has been spawned.
    /// </summary>
    public bool HasVisual()
    {
        return visualInstance != null;
    }

    #endregion
}