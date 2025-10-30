using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Core pathfinding system for unit movement.
/// Matches SDK's Pathing.ts implementation exactly.
/// SDK Reference: Pathing.ts
/// </summary>
public static class Pathing
{
    #region Tile Caching System
    // SDK Reference: Pathing.ts lines 14-18
    // Cache for tile walkability checks to avoid repeated collision checks
    // Key format: "{serialNumber}-{x}-{y}-{size}-{mobToAvoidSerial}"
    private static Dictionary<string, bool> tileCache = new Dictionary<string, bool>();

    /// <summary>
    /// Clear the pathfinding cache. Call this when the world state changes
    /// (e.g., entities added/removed, tile properties changed).
    /// SDK Reference: Pathing.ts lines 20-22
    /// </summary>
    public static void PurgeTileCache()
    {
        tileCache.Clear();
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Get all entities at a specific point.
    /// SDK Reference: Pathing.ts lines 5-17
    /// </summary>
    public static List<Entity> EntitiesAtPoint(Vector2Int pos, int size)
    {
        List<Entity> entities = new List<Entity>();

        if (GridManager.Instance == null)
            return entities;

        List<Entity> allEntities = GridManager.Instance.GetAllEntities();

        foreach (Entity entity in allEntities)
        {
            if (Collision.CollisionMath(
                pos.x, pos.y, size,
                entity.gridPosition.x, entity.gridPosition.y, entity.size))
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    /// <summary>
    /// Linear interpolation between two values.
    /// SDK Reference: Pathing.ts lines 50-52
    /// </summary>
    public static float LinearInterpolation(float x, float y, float a)
    {
        return (y - x) * a + x;
    }

    /// <summary>
    /// Calculate Euclidean distance between two points.
    /// SDK Reference: Pathing.ts lines 54-58 (references LocationUtils.dist)
    /// </summary>
    public static float Dist(float x, float y, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x2 - x, 2) + Mathf.Pow(y2 - y, 2));
    }

    /// <summary>
    /// Calculate angle in radians from point 1 to point 2.
    /// SDK Reference: Pathing.ts lines 60-62 (references LocationUtils.angle)
    /// </summary>
    public static float Angle(float x, float y, float x2, float y2)
    {
        return Mathf.Atan2(y2 - y, x2 - x);
    }
    #endregion

    #region Core Pathfinding

    /// <summary>
    /// Check if a tile can be pathed to (walkable and no collision).
    /// Uses caching to avoid repeated checks.
    /// SDK Reference: Pathing.ts lines 64-78
    /// 
    /// CRITICAL: This caches results per region/position/size/unitToAvoid combination.
    /// The cache must be purged when world state changes.
    /// 
    /// IMPORTANT SDK BEHAVIOR:
    /// - unitToAvoid == null: PLAYER movement - don't check mob collisions  
    /// - unitToAvoid != null: MOB movement - check mob collisions
    /// </summary>
    public static bool CanTileBePathedTo(Vector2Int pos, int size, Unit unitToAvoid = null)
    {
        if (GridManager.Instance == null)
            return false;

        // Build cache key
        string cacheKey = $"{pos.x}-{pos.y}-{size}-{(unitToAvoid != null ? unitToAvoid.GetInstanceID() : 0)}";

        // Check cache first
        if (tileCache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        // Check tile exists and is walkable FIRST
        Tile tile = GridManager.Instance.GetTileAt(pos);
        if (tile == null || !tile.isWalkable)
        {
            tileCache[cacheKey] = false;
            return false;
        }

        // Check collision with static entities (walls, objects)
        bool collision = Collision.CollidesWithAnyEntities(pos, size);

        // CRITICAL SDK BEHAVIOR: Only check mob collisions if unitToAvoid is provided
        // SDK Reference: Pathing.ts lines 71-76
        // "if (mobToAvoid) { // Player can walk under mobs"
        if (unitToAvoid != null)
        {
            // MOB is checking movement - check for other mobs
            Unit blockingUnit = Collision.CollidesWithAnyUnits(pos, size, unitToAvoid);
            collision = collision || (blockingUnit != null);
        }
        // If unitToAvoid is null, this is a PLAYER - skip mob collision checks entirely!
        // Players can walk through ALL mobs

        bool result = !collision;

        // Cache the result
        tileCache[cacheKey] = result;

        return result;
    }

    /// <summary>
    /// Represents a node in the pathfinding graph.
    /// SDK Reference: Pathing.ts lines 80-86 (interface PathingNode)
    /// </summary>
    private class PathingNode
    {
        public int x;
        public int y;
        public PathingNode parent;
        public int pathLength;

        public PathingNode(int x, int y, PathingNode parent = null, int pathLength = 0)
        {
            this.x = x;
            this.y = y;
            this.parent = parent;
            this.pathLength = pathLength;
        }
    }

    /// <summary>
    /// Result of pathfinding operation.
    /// SDK Reference: Pathing.ts lines 88-105 (return type of constructPaths)
    /// </summary>
    public class PathResult
    {
        public Vector2Int? destination;  // null if no path found
        public List<Vector2Int> path;    // Path from start to destination (reversed)

        public PathResult()
        {
            destination = null;
            path = new List<Vector2Int>();
        }
    }

    /// <summary>
    /// Core pathfinding algorithm using Dijkstra's algorithm.
    /// Finds the shortest path from startPoint to any of the endPoints.
    /// 
    /// SDK Reference: Pathing.ts lines 88-201
    /// 
    /// CRITICAL IMPLEMENTATION NOTES:
    /// 1. Searches up to 1000 nodes before giving up
    /// 2. Uses 8-directional movement (including diagonals)
    /// 3. Direction order matters for tie-breaking (straight moves before diagonal)
    /// 4. Validates diagonal moves by checking adjacent tiles
    /// 5. If no direct path, finds backup tile within 21x21 grid of SW tile
    /// 6. Backup tile has shortest path length and closest Euclidean distance
    /// </summary>
    public static PathResult ConstructPaths(Vector2Int startPoint, List<Vector2Int> endPoints)
    {
        PathResult result = new PathResult();

        if (endPoints == null || endPoints.Count == 0)
            return result;

        // Check if any end points are pathable
        // SDK Reference: Pathing.ts lines 93-99
        List<Vector2Int> unpathableEndPoints = endPoints.Where(pos =>
            !CanTileBePathedTo(pos, 1)).ToList();

        if (unpathableEndPoints.Count == endPoints.Count)
        {
            // All end points are blocked - no path possible
            return result;
        }

        List<Vector2Int> pathableEndPoints = endPoints.Where(pos =>
            CanTileBePathedTo(pos, 1)).ToList();

        // Initialize search
        // SDK Reference: Pathing.ts lines 101-107
        Queue<PathingNode> nodes = new Queue<PathingNode>();
        nodes.Enqueue(new PathingNode(startPoint.x, startPoint.y, null, 0));

        // Direction order is CRITICAL - determines which path is chosen in ties
        // SDK Reference: Pathing.ts lines 109-118
        // Straight directions first, then diagonals
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // west
            new Vector2Int(1, 0),   // east
            new Vector2Int(0, 1),   // south (y+ in OSRS = south)
            new Vector2Int(0, -1),  // north (y- in OSRS = north)
            new Vector2Int(-1, 1),  // southwest
            new Vector2Int(1, 1),   // southeast
            new Vector2Int(-1, -1), // northwest
            new Vector2Int(1, -1)   // northeast
        };

        // Explored nodes: explored[x][y] = PathingNode
        // SDK Reference: Pathing.ts lines 120-127
        Dictionary<int, Dictionary<int, PathingNode>> explored =
            new Dictionary<int, Dictionary<int, PathingNode>>();

        // Initialize starting node
        explored[startPoint.x] = new Dictionary<int, PathingNode>();
        explored[startPoint.x][startPoint.y] = new PathingNode(startPoint.x, startPoint.y, null, 0);

        // Track bounds of explored area (for backup tile search)
        int minExploredX = startPoint.x;
        int minExploredY = startPoint.y;
        int maxExploredX = startPoint.x;
        int maxExploredY = startPoint.y;

        // Dijkstra search
        // SDK Reference: Pathing.ts lines 129-197
        while (nodes.Count > 0 && nodes.Count < 1000)
        {
            PathingNode parentNode = nodes.Dequeue();

            // Check if we reached any destination
            // SDK Reference: Pathing.ts lines 131-140
            foreach (Vector2Int dest in pathableEndPoints)
            {
                if (dest.x == parentNode.x && dest.y == parentNode.y)
                {
                    // Found destination! Unwind path from parent nodes
                    List<Vector2Int> path = new List<Vector2Int>();
                    PathingNode current = parentNode;
                    while (current != null)
                    {
                        path.Add(new Vector2Int(current.x, current.y));
                        current = current.parent;
                    }
                    result.destination = dest;
                    result.path = path;
                    return result;
                }
            }

            // Get current path length
            int currentDistance = explored[parentNode.x][parentNode.y].pathLength;

            // Explore all 8 directions
            // SDK Reference: Pathing.ts lines 142-195
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int dir = directions[i];
                int pathX = parentNode.x + dir.x;
                int pathY = parentNode.y + dir.y;

                // Check if destination is walkable
                if (!CanTileBePathedTo(new Vector2Int(pathX, pathY), 1, null))
                {
                    continue;
                }

                // For diagonal moves (i >= 4), check adjacent tiles
                // SDK Reference: Pathing.ts lines 153-167
                // This prevents cutting corners through walls
                if (i >= 4)
                {
                    // Check the two adjacent tiles for diagonal movement
                    int neighbourX = parentNode.x;
                    int neighbourY = parentNode.y + dir.y;
                    if (!CanTileBePathedTo(new Vector2Int(neighbourX, neighbourY), 1, null))
                    {
                        continue;
                    }

                    neighbourX = parentNode.x + dir.x;
                    neighbourY = parentNode.y;
                    if (!CanTileBePathedTo(new Vector2Int(neighbourX, neighbourY), 1, null))
                    {
                        continue;
                    }
                }

                // Add to explored if not already explored
                // SDK Reference: Pathing.ts lines 169-189
                if (!explored.ContainsKey(pathX))
                {
                    explored[pathX] = new Dictionary<int, PathingNode>();
                    if (pathX < minExploredX) minExploredX = pathX;
                    if (pathX > maxExploredX) maxExploredX = pathX;
                }

                if (explored[pathX].ContainsKey(pathY))
                {
                    // Already explored this tile
                    continue;
                }
                else
                {
                    explored[pathX][pathY] = new PathingNode(
                        pathX, pathY, parentNode, currentDistance + 1);
                    if (pathY < minExploredY) minExploredY = pathY;
                    if (pathY > maxExploredY) maxExploredY = pathY;
                }

                // Add to queue
                nodes.Enqueue(new PathingNode(pathX, pathY, parentNode));
            }
        }

        // No direct path found - search for backup tile
        // SDK Reference: Pathing.ts lines 199-244
        // 
        // BACKUP TILE SYSTEM:
        // If no direct path exists, find the "best" explored tile within a 21x21 grid
        // centered on the SW (first) end point. This tile must:
        // 1. Have path length < 100
        // 2. Have shortest path distance
        // 3. Be closest in Euclidean distance to nearest requested tile
        Vector2Int swTile = endPoints[0];  // SW tile is always first
        int minX = Mathf.Max(minExploredX, swTile.x - 10);
        int minY = Mathf.Max(minExploredY, swTile.y - 10);
        int maxX = Mathf.Min(maxExploredX, swTile.x + 10);
        int maxY = Mathf.Min(maxExploredY, swTile.y + 10);

        Vector2Int? bestBackupTile = null;
        float minEuclideanDistance = float.MaxValue;
        int minPathLength = 100;

        // Search 21x21 grid for best backup tile
        // SDK Reference: Pathing.ts lines 212-238
        for (int x = minX; x <= maxX; x++)
        {
            if (!explored.ContainsKey(x))
                continue;

            for (int y = minY; y <= maxY; y++)
            {
                if (!explored[x].ContainsKey(y))
                    continue;

                int pathLength = explored[x][y].pathLength;

                // Check distance to all end points, pick closest
                foreach (Vector2Int endPoint in endPoints)
                {
                    float dist = Dist(x, y, endPoint.x, endPoint.y);

                    // Prefer closer Euclidean distance, or shorter path if same distance
                    if (dist < minEuclideanDistance ||
                        (dist == minEuclideanDistance && pathLength < minPathLength))
                    {
                        bestBackupTile = new Vector2Int(x, y);
                        minPathLength = pathLength;
                        minEuclideanDistance = dist;
                    }
                }
            }
        }

        // Build path to backup tile if found
        // SDK Reference: Pathing.ts lines 240-252
        if (bestBackupTile.HasValue)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathingNode node = explored[bestBackupTile.Value.x][bestBackupTile.Value.y];
            while (node != null)
            {
                path.Add(new Vector2Int(node.x, node.y));
                node = node.parent;
            }
            result.destination = bestBackupTile;
            result.path = path;
            return result;
        }

        // No path or backup tile found
        return result;
    }

    /// <summary>
    /// Result of a movement path calculation.
    /// SDK Reference: Pathing.ts lines 257-265 (return type of path())
    /// </summary>
    public class MoveResult
    {
        public Vector2Int position;           // Where to move this tick
        public List<Vector2Int> path;         // Next 2-3 tiles (for visualization)
        public Vector2Int? destination;       // Final destination (can be null)

        public MoveResult(Vector2Int position)
        {
            this.position = position;
            this.path = new List<Vector2Int>();
            this.destination = null;
        }
    }

    /// <summary>
    /// Calculate movement path and determine where to move this tick.
    /// This is a wrapper around ConstructPaths for actual movement.
    /// 
    /// SDK Reference: Pathing.ts lines 257-280
    /// 
    /// IMPORTANT: 
    /// - speed = 1 for walking, 2 for running
    /// - Returns the tile to move to THIS TICK
    /// - Also returns ONLY the tiles traversed THIS TICK for smooth interpolation
    /// </summary>
    public static MoveResult Path(
        Vector2Int startPoint,
        Vector2Int endPoint,
        int speed,
        Unit seekingUnit = null)
    {
        MoveResult result = new MoveResult(startPoint);

        // Get path from pathfinding
        PathResult pathResult = ConstructPaths(startPoint, new List<Vector2Int> { endPoint });

        if (pathResult.path == null || pathResult.path.Count == 0)
        {
            return result;
        }

        // Check if we're colliding with the unit we're seeking
        if (seekingUnit != null && pathResult.path.Count > 0)
        {
            if (Collision.CollidesWithUnit(pathResult.path[0], 1, seekingUnit))
            {
                pathResult.path.RemoveAt(0);
            }
        }

        if (pathResult.path.Count == 0)
        {
            return result;
        }

        // Determine where to move this tick based on speed
        Vector2Int moveTarget;
        List<Vector2Int> tilesTraversedThisTick = new List<Vector2Int>();

        if (pathResult.path.Count <= speed)
        {
            // Can reach destination this tick
            moveTarget = pathResult.path[0];

            // Path is from destination to start, so reverse to get start->destination order
            pathResult.path.Reverse();

            // Skip the starting position (index 0), take only tiles we're moving through
            tilesTraversedThisTick = pathResult.path.Skip(1).ToList();
        }
        else
        {
            // Move 'speed' steps forward
            moveTarget = pathResult.path[pathResult.path.Count - speed - 1];

            // Path is from destination to start, so reverse to get start->destination order
            pathResult.path.Reverse();

            // Take ONLY the tiles we're actually moving through this tick
            // Skip index 0 (current position), take 'speed' tiles
            tilesTraversedThisTick = pathResult.path.Skip(1).Take(speed).ToList();
        }

        // Build result
        result.position = moveTarget;
        result.destination = pathResult.destination;

        // CRITICAL FIX: Only return the tiles we're ACTUALLY traversing this tick
        // Not arbitrary future tiles from the full path!
        result.path = tilesTraversedThisTick;

        return result;
    }

    #endregion
}
