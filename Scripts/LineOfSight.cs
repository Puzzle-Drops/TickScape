using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Line of sight masks for tile blocking.
/// SDK Reference: LineOfSightMask enum in LineOfSight.ts
/// </summary>
public enum LineOfSightMask
{
    NONE = 0x0,
    FULL_MASK = 0x20000,
    EAST_MASK = 0x1000,
    WEST_MASK = 0x10000,
    NORTH_MASK = 0x400,
    SOUTH_MASK = 0x4000
}

/// <summary>
/// Line of sight raycasting system.
/// Uses OSRS's complex raycasting algorithm to determine if units can see each other.
/// SDK Reference: LineOfSight.ts
/// 
/// CRITICAL ALGORITHM NOTES:
/// - Uses bit shifting and masks to check tile edges
/// - Different logic for horizontal vs vertical dominant rays
/// - Checks tiles along the ray path for blocking
/// </summary>
public static class LineOfSight
{
    // Debug visualization
    private static bool debugMode = false;
    private static List<Vector2Int> lastLOSTiles = new List<Vector2Int>();

    /// <summary>
    /// Enable/disable debug visualization.
    /// When enabled, draws green tiles showing LOS area in Scene view.
    /// </summary>
    public static void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }

    /// <summary>
    /// Check if player has line of sight to a mob.
    /// SDK Reference: LineOfSight.playerHasLineOfSightOfMob() in LineOfSight.ts
    /// </summary>
    public static bool PlayerHasLineOfSightOfMob(Vector2Int playerPos, Unit mob, int range = 1)
    {
        // Get closest point on mob to player
        Vector2Int closestPoint = GetClosestPointTo(playerPos, mob);
        return HasLineOfSight(playerPos.x, playerPos.y, closestPoint.x, closestPoint.y, 1, range, false);
    }

    /// <summary>
    /// Check if mob has line of sight to player.
    /// SDK Reference: LineOfSight.mobHasLineOfSightOfPlayer() in LineOfSight.ts
    /// </summary>
    public static bool MobHasLineOfSightOfPlayer(Unit mob, Vector2Int playerPos, int range = 1)
    {
        return HasLineOfSight(mob.gridPosition.x, mob.gridPosition.y,
                             playerPos.x, playerPos.y, mob.size, range, true);
    }

    /// <summary>
    /// Check if mob has line of sight to another mob.
    /// SDK Reference: LineOfSight.mobHasLineOfSightToMob()
    /// </summary>
    public static bool MobHasLineOfSightToMob(Unit mob1, Unit mob2, int range = 1)
    {
        Vector2Int mob1Point = GetClosestPointTo(mob1.gridPosition, mob2);
        Vector2Int mob2Point = GetClosestPointTo(mob2.gridPosition, mob1);
        return HasLineOfSight(mob1Point.x, mob1Point.y, mob2Point.x, mob2Point.y, 1, range, false);
    }

    /// <summary>
    /// Core line of sight algorithm. This is the heart of OSRS raycasting.
    /// SDK Reference: LineOfSight.hasLineOfSight() in LineOfSight.ts lines 47-154
    /// 
    /// ALGORITHM OVERVIEW:
    /// 1. Check if either endpoint is blocked
    /// 2. Check if positions overlap (always true for melee range)
    /// 3. For range > 1, use raycasting algorithm
    /// 4. Cast ray using either horizontal or vertical dominant logic
    /// 5. Check each tile along ray for blocking using masks
    /// 
    /// Parameters:
    /// - (x1, y1): Start position
    /// - (x2, y2): End position  
    /// - s: Size of entity at start (1 for player)
    /// - r: Attack range
    /// - isNPC: true if checking from NPC's perspective
    /// </summary>
    public static bool HasLineOfSight(int x1, int y1, int x2, int y2, int s = 1, int r = 1, bool isNPC = false)
    {
        // Calculate delta
        int dx = x2 - x1;
        int dy = y2 - y1;

        // Check if either position is blocked by LoS-blocking entities
        if (CollidesWithAnyLoSBlockingEntities(x1, y1, 1) != 0 ||
        CollidesWithAnyLoSBlockingEntities(x2, y2, 1) != 0 ||
        Collision.CollisionMath(x1, y1, s, x2, y2, 1))
        {
            return false;
        }

        // Melee range (range == 1) uses simple adjacency check
        // SDK Reference: LineOfSight.ts lines 53-56
        if (r == 1)
        {
            return (dx < s && dx >= 0 && (dy == 1 || dy == -s)) ||
                   (dy > -s && dy <= 0 && (dx == -1 || dx == s));
        }

        // For NPCs with size > 1, recursively check from closest point
        // SDK Reference: LineOfSight.ts lines 57-61
        if (isNPC)
        {
            int tx = Mathf.Max(x1, Mathf.Min(x1 + s - 1, x2));
            int ty = Mathf.Max(y1 - s + 1, Mathf.Min(y1, y2));
            return HasLineOfSight(x2, y2, tx, ty, 1, r, false);
        }

        // Check if target is within range
        int dxAbs = Mathf.Abs(dx);
        int dyAbs = Mathf.Abs(dy);
        if (dxAbs > r || dyAbs > r)
        {
            return false;
        }

        // RAYCASTING ALGORITHM
        // Uses bit-shifted fixed-point math for sub-tile precision
        // SDK Reference: LineOfSight.ts lines 69-145

        if (dxAbs > dyAbs)
        {
            // Horizontal dominant ray
            // SDK Reference: LineOfSight.ts lines 71-107
            int xTile = x1;
            int y = (y1 << 16) + 0x8000; // Fixed point: y * 65536 + 32768
            int slope = (dy << 16) / dxAbs; // Fixed point slope

            int xInc;
            int xMask;
            int yMask;

            if (dx > 0)
            {
                xInc = 1;
                xMask = (int)LineOfSightMask.WEST_MASK | (int)LineOfSightMask.FULL_MASK;
            }
            else
            {
                xInc = -1;
                xMask = (int)LineOfSightMask.EAST_MASK | (int)LineOfSightMask.FULL_MASK;
            }

            if (dy < 0)
            {
                y -= 1; // For correct rounding
                yMask = (int)LineOfSightMask.NORTH_MASK | (int)LineOfSightMask.FULL_MASK;
            }
            else
            {
                yMask = (int)LineOfSightMask.SOUTH_MASK | (int)LineOfSightMask.FULL_MASK;
            }

            while (xTile != x2)
            {
                xTile += xInc;
                int yTile = y >> 16; // Convert fixed point back to int

                // Check if tile blocks with X mask
                if ((CollidesWithAnyLoSBlockingEntities(xTile, yTile, 1) & xMask) != 0)
                {
                    return false;
                }

                y += slope;
                int newYTile = y >> 16;

                // Check if Y changed and blocks with Y mask
                if (newYTile != yTile &&
                    (CollidesWithAnyLoSBlockingEntities(xTile, newYTile, 1) & yMask) != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vertical dominant ray
            // SDK Reference: LineOfSight.ts lines 108-144
            int yTile = y1;
            int x = (x1 << 16) + 0x8000; // Fixed point
            int slope = (dx << 16) / dyAbs;

            int yInc;
            int yMask;
            int xMask;

            if (dy > 0)
            {
                yInc = 1;
                yMask = (int)LineOfSightMask.SOUTH_MASK | (int)LineOfSightMask.FULL_MASK;
            }
            else
            {
                yInc = -1;
                yMask = (int)LineOfSightMask.NORTH_MASK | (int)LineOfSightMask.FULL_MASK;
            }

            if (dx < 0)
            {
                x -= 1;
                xMask = (int)LineOfSightMask.EAST_MASK | (int)LineOfSightMask.FULL_MASK;
            }
            else
            {
                xMask = (int)LineOfSightMask.WEST_MASK | (int)LineOfSightMask.FULL_MASK;
            }

            while (yTile != y2)
            {
                yTile += yInc;
                int xTile = x >> 16;

                // Check if tile blocks with Y mask
                if ((CollidesWithAnyLoSBlockingEntities(xTile, yTile, 1) & yMask) != 0)
                {
                    return false;
                }

                x += slope;
                int newXTile = x >> 16;

                // Check if X changed and blocks with X mask
                if (newXTile != xTile &&
                    (CollidesWithAnyLoSBlockingEntities(newXTile, yTile, 1) & xMask) != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Get closest point on target unit to source position.
    /// SDK Reference: LocationUtils.closestPointTo() in Location.ts
    /// </summary>
    private static Vector2Int GetClosestPointTo(Vector2Int sourcePos, Unit target)
    {
        List<Vector2Int> corners = new List<Vector2Int>();

        for (int xx = 0; xx < target.size; xx++)
        {
            for (int yy = 0; yy < target.size; yy++)
            {
                corners.Add(new Vector2Int(
                    target.gridPosition.x + xx,
                    target.gridPosition.y - yy
                ));
            }
        }

        Vector2Int closest = corners[0];
        float minDist = Pathing.Dist(sourcePos.x, sourcePos.y, closest.x, closest.y);

        foreach (Vector2Int corner in corners)
        {
            float dist = Pathing.Dist(sourcePos.x, sourcePos.y, corner.x, corner.y);
            if (dist < minDist)
            {
                minDist = dist;
                closest = corner;
            }
        }

        return closest;
    }

    /// <summary>
    /// Check if position collides with any LoS-blocking entities.
    /// Returns mask indicating which edges are blocked.
    /// SDK Reference: Collision.collidesWithAnyLoSBlockingEntities() in Collision.ts
    /// </summary>
    private static int CollidesWithAnyLoSBlockingEntities(int x, int y, int size)
    {
        if (GridManager.Instance == null)
            return 0;

        // Check tile walkability - unwalkable tiles block LoS
        Tile tile = GridManager.Instance.GetTileAt(new Vector2Int(x, y));
        if (tile == null || !tile.isWalkable || tile.blocksLineOfSight)
        {
            return (int)LineOfSightMask.FULL_MASK;
        }

        // Check entities
        List<Entity> allEntities = GridManager.Instance.GetAllEntities();
        foreach (Entity entity in allEntities)
        {
            // Skip units - they don't block LoS
            if (entity is Unit)
                continue;

            // Check if entity blocks LoS
            if (entity.collisionType != CollisionType.NONE)
            {
                if (Collision.CollisionMath(x, y, size,
                    entity.gridPosition.x, entity.gridPosition.y, entity.size))
                {
                    return (int)LineOfSightMask.FULL_MASK;
                }
            }
        }

        return 0;
    }

    #region Debug Visualization

    /// <summary>
    /// Draw line of sight area for debugging.
    /// Shows which tiles are in LoS as green in Scene view.
    /// SDK Reference: LineOfSight.drawLOS() in LineOfSight.ts
    /// </summary>
    public static void DrawLOS(Vector2Int center, int size, int range, Color color)
    {
        if (!debugMode || GridManager.Instance == null)
            return;

        lastLOSTiles.Clear();

        // Check all tiles in range
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                if (HasLineOfSight(center.x, center.y, checkPos.x, checkPos.y, size, range, false))
                {
                    lastLOSTiles.Add(checkPos);

                    // Draw debug tile
                    Vector3 worldPos = GridManager.Instance.GridToWorld(checkPos);
                    Vector3 tileCenter = worldPos + new Vector3(0.5f, 0.1f, 0.5f);

                    // Draw square on ground
                    Vector3[] corners = new Vector3[]
                    {
                        worldPos + new Vector3(0, 0.1f, 0),
                        worldPos + new Vector3(1, 0.1f, 0),
                        worldPos + new Vector3(1, 0.1f, 1),
                        worldPos + new Vector3(0, 0.1f, 1)
                    };

                    Debug.DrawLine(corners[0], corners[1], color);
                    Debug.DrawLine(corners[1], corners[2], color);
                    Debug.DrawLine(corners[2], corners[3], color);
                    Debug.DrawLine(corners[3], corners[0], color);
                }
            }
        }
    }

    /// <summary>
    /// Get tiles that were in LoS from last DrawLOS call (for testing).
    /// </summary>
    public static List<Vector2Int> GetLastLOSTiles()
    {
        return new List<Vector2Int>(lastLOSTiles);
    }

    #endregion
}
