using UnityEngine;

/// <summary>
/// Collision type for entities.
/// SDK Reference: Collision.ts lines 7-10
/// </summary>
public enum CollisionType
{
    NONE = 0,              // No collision (walkable through)
    BLOCK_MOVEMENT = 1     // Blocks movement
}

/// <summary>
/// Base class for all game entities (players, mobs, objects).
/// Matches SDK's Entity.ts with position lerping system.
/// SDK Reference: Entity.ts
/// </summary>
public class Entity : MonoBehaviour
{
    [Header("Grid Position")]
    [Tooltip("Logical grid position. NOTE: .x = world X-axis, .y = world Z-axis")]
    public Vector2Int gridPosition;

    [Header("Entity Properties")]
    [Tooltip("Size in tiles (1x1, 2x2, etc). Matches SDK's size property.")]
    public int size = 1;

    [Header("Death System")]
    [Tooltip("Death countdown timer. -1 = alive, 0 = should destroy, >0 = dying")]
    public int dying = -1;

    /// <summary>
    /// Unique identifier for this entity, used by caching systems.
    /// SDK Reference: Entity.ts lines 14-20
    /// </summary>
    private string _serialNumber;
    public string SerialNumber
    {
        get
        {
            if (string.IsNullOrEmpty(_serialNumber))
            {
                _serialNumber = System.Guid.NewGuid().ToString();
            }
            return _serialNumber;
        }
    }

    /// <summary>
    /// Collision type for this entity.
    /// SDK Reference: Entity.ts line 39 (get collisionType)
    /// Override in subclasses to change collision behavior.
    /// </summary>
    public virtual CollisionType collisionType
    {
        get { return CollisionType.BLOCK_MOVEMENT; }
    }

    // Visual position tracking (for smooth lerping between ticks)
    // SDK Reference: Unit.perceivedLocation in Unit.ts
    private Vector2Int lastGridPosition;
    private bool initialized = false;

    /// <summary>
    /// Check if this entity is currently dying (death animation playing).
    /// SDK Reference: Entity.ts line 32
    /// </summary>
    public bool IsDying()
    {
        return dying > 0;
    }

    /// <summary>
    /// Check if this entity should be destroyed (death complete).
    /// SDK Reference: Entity.ts line 28
    /// </summary>
    public bool ShouldDestroy()
    {
        return dying == 0;
    }

    /// <summary>
    /// Get the name of this entity (for named entities like bosses).
    /// SDK Reference: Entity.ts line 73
    /// </summary>
    public virtual string EntityName()
    {
        return null;
    }

    /// <summary>
    /// Returns the closest tile on this entity to the specified point.
    /// Used by pathing and combat systems.
    /// SDK Reference: Entity.ts lines 64-70
    /// </summary>
    public Vector2Int GetClosestTileTo(Vector2Int target)
    {
        int clampedX = Mathf.Clamp(target.x, gridPosition.x, gridPosition.x + size - 1);
        // UNITY FIX: Y+ = North, so entity occupies [y, y+size-1], not [y-size+1, y]
        int clampedY = Mathf.Clamp(target.y, gridPosition.y, gridPosition.y + size - 1);
        return new Vector2Int(clampedX, clampedY);
    }

    /// <summary>
    /// Called every game tick (0.6s). Override for gameplay logic.
    /// SDK Reference: Entity.tick() in Entity.ts line 92
    /// </summary>
    public virtual void Tick()
    {
        // Override in subclasses for gameplay logic
    }

    /// <summary>
    /// Called at the START of each tick. Saves position for lerping.
    /// SDK Reference: Matches SDK's pattern where perceivedLocation = location at tick start
    /// </summary>
    public void OnTickStart()
    {
        // FIX: Initialize lastGridPosition to current position on first tick
        // This prevents lerping from (0,0) on spawn
        if (!initialized)
        {
            lastGridPosition = gridPosition;
            initialized = true;
        }
        else
        {
            lastGridPosition = gridPosition;
        }
    }

    /// <summary>
    /// Smooth visual movement between ticks.
    /// SDK Reference: Unit.getPerceivedLocation() in Unit.ts lines 382-387
    /// </summary>
    void LateUpdate()
    {
        if (GridManager.Instance == null || WorldManager.Instance == null)
            return;

        // Don't lerp until we've had our first tick - this prevents lerping from (0,0)
        if (!initialized)
            return;

        // Get interpolation value (0.0 to 1.0 between ticks)
        float tickPercent = WorldManager.Instance.GetTickPercent();

        // Lerp from last position to current position
        Vector3 lastWorldPos = GridManager.Instance.GridToWorld(lastGridPosition);
        Vector3 currentWorldPos = GridManager.Instance.GridToWorld(gridPosition);

        transform.position = Vector3.Lerp(lastWorldPos, currentWorldPos, tickPercent);
    }

    /// <summary>
    /// Auto-cleanup when destroyed.
    /// SDK Reference: Entity.removedFromWorld() in Entity.ts
    /// </summary>
    void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterEntity(this);
        }
    }

    #region Helper Methods (SDK Equivalents)

    /// <summary>
    /// Check if this entity is on the specified tile.
    /// SDK Reference: Entity.isOnTile() in Entity.ts lines 53-60
    /// </summary>
    public bool IsOnTile(Vector2Int pos)
    {
        return pos.x >= gridPosition.x &&
               pos.x < gridPosition.x + size &&
               pos.y >= gridPosition.y &&
               pos.y < gridPosition.y + size;
    }

    #endregion
}
