using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public float tileSize = 1f;
    public int gridWidth;
    public int gridHeight;

    // Tile storage - fast O(1) lookup by grid position
    private Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

    // Entity storage (matches SDK's Region.entities)
    private List<Entity> entities = new List<Entity>();

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region Tile Management

    /// <summary>
    /// Register all tiles with the grid manager. Called by GridBuilder in Edit Mode.
    /// </summary>
    public void RegisterTiles(Dictionary<Vector2Int, Tile> tileDict)
    {
        tiles = tileDict;
        Debug.Log($"GridManager: Registered {tiles.Count} tiles");
    }

    /// <summary>
    /// Register a single tile at runtime. Called by Tile.Start().
    /// CRITICAL: This allows tiles to register themselves when Play mode starts.
    /// </summary>
    public void RegisterTile(Tile tile)
    {
        if (tile == null)
            return;

        if (tiles.ContainsKey(tile.gridPosition))
        {
            Debug.LogWarning($"GridManager: Tile at {tile.gridPosition} already registered! Overwriting.");
            tiles[tile.gridPosition] = tile;
        }
        else
        {
            tiles.Add(tile.gridPosition, tile);
        }
    }

    /// <summary>
    /// Check if a grid position is within valid bounds.
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    /// <summary>
    /// Get tile at position, with bounds checking.
    /// </summary>
    public bool TryGetTileAt(Vector2Int gridPos, out Tile tile)
    {
        tile = GetTileAt(gridPos);
        return tile != null;
    }

    /// <summary>
    /// Get tile at grid position. Returns null if no tile exists.
    /// SDK Reference: Used constantly by Pathing.ts and Collision.ts
    /// </summary>
    public Tile GetTileAt(Vector2Int gridPos)
    {
        tiles.TryGetValue(gridPos, out Tile tile);
        return tile;
    }

    /// <summary>
    /// Check if tile at position is walkable.
    /// SDK Reference: Pathing.canTileBePathedTo() in Pathing.ts
    /// </summary>
    public bool IsWalkable(Vector2Int gridPos)
    {
        Tile tile = GetTileAt(gridPos);
        return tile != null && tile.isWalkable;
    }

    /// <summary>
    /// Check if tile at position blocks line of sight.
    /// SDK Reference: Used by LineOfSight.ts for raycasting
    /// </summary>
    public bool BlocksLineOfSight(Vector2Int gridPos)
    {
        Tile tile = GetTileAt(gridPos);
        return tile != null && tile.blocksLineOfSight;
    }

    #endregion

    #region Entity Management

    /// <summary>
    /// Register entity with the grid. Explicit registration matches SDK pattern.
    /// SDK Reference: Region.addEntity() in Region.ts line 93
    /// </summary>
    public void RegisterEntity(Entity entity)
    {
        if (!entities.Contains(entity))
        {
            entities.Add(entity);
            Debug.Log($"GridManager: Registered entity at {entity.gridPosition}");
        }
    }

    /// <summary>
    /// Unregister entity from the grid.
    /// SDK Reference: Region.removeEntity() in Region.ts line 97
    /// </summary>
    public void UnregisterEntity(Entity entity)
    {
        entities.Remove(entity);
    }

    /// <summary>
    /// Get all registered entities (for WorldManager to tick).
    /// </summary>
    public List<Entity> GetAllEntities()
    {
        return entities;
    }

    #endregion

    #region Coordinate Conversion Helpers

    /// <summary>
    /// Convert grid position to world position.
    /// NOTE: gridPos.x maps to world X-axis, gridPos.y maps to world Z-axis
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * tileSize,  // X = east-west
            0f,                     // Y = elevation (always 0 for flat grid)
            gridPos.y * tileSize   // Z = north-south (gridPos.y is actually Z!)
        );
    }

    /// <summary>
    /// Convert world position to grid position.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / tileSize),
            Mathf.RoundToInt(worldPos.z / tileSize)  // Z maps to gridPos.y
        );
    }

    #endregion
}
