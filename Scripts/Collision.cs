using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static collision detection system for units and tiles.
/// Matches SDK's Collision.ts logic.
/// SDK Reference: Collision.ts
/// </summary>
public static class Collision
{
    /// <summary>
    /// Core OSRS collision math. Checks if two rectangles overlap.
    /// 
    /// Parameters:
    /// - (x, y, s): First rectangle's position and size
    /// - (x2, y2, s2): Second rectangle's position and size
    /// 
    /// Returns: true if rectangles overlap
    /// 
    /// SDK Reference: Collision.collisionMath() in Collision.ts lines 7-11
    /// </summary>
    public static bool CollisionMath(int x, int y, int s, int x2, int y2, int s2)
    {
        // OSRS collision logic
        // Returns true if the two areas OVERLAP
        return !(x > x2 + s2 - 1 ||
                 x + s - 1 < x2 ||
                 y - s + 1 > y2 ||
                 y < y2 - s2 + 1);
    }

    /// <summary>
    /// Check if position collides with a specific unit.
    /// SDK Reference: Collision.collidesWithMob() in Collision.ts
    /// </summary>
    public static bool CollidesWithUnit(Vector2Int pos, int size, Unit unit)
    {
        return CollisionMath(pos.x, pos.y, size, unit.gridPosition.x, unit.gridPosition.y, unit.size);
    }

    /// <summary>
    /// Check if position collides with ANY unit in the grid (except optional unit to avoid).
    /// CRITICAL: Only returns units that have consumesSpace = true
    /// SDK Reference: Collision.collidesWithAnyMobs() in Collision.ts lines 27-41
    /// </summary>
    public static Unit CollidesWithAnyUnits(Vector2Int pos, int size, Unit unitToAvoid = null)
    {
        if (GridManager.Instance == null)
            return null;

        List<Entity> allEntities = GridManager.Instance.GetAllEntities();

        foreach (Entity entity in allEntities)
        {
            // Skip if not a unit
            if (!(entity is Unit unit))
                continue;

            // Skip the unit we're avoiding (usually ourself)
            if (unit == unitToAvoid)
                continue;

            // CRITICAL: Only check units that consume space (block movement)
            // SDK Reference: Collision.ts line 37-39
            if (!unit.consumesSpace)
                continue;

            // Check collision
            if (CollidesWithUnit(pos, size, unit))
            {
                return unit;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if position collides with any blocking entities (NOT units/mobs).
    /// SDK Reference: Collision.collidesWithAnyEntities() in Collision.ts
    /// </summary>
    public static bool CollidesWithAnyEntities(Vector2Int pos, int size)
    {
        if (GridManager.Instance == null)
            return false;

        List<Entity> allEntities = GridManager.Instance.GetAllEntities();

        foreach (Entity entity in allEntities)
        {
            // Skip units - they're handled separately
            if (entity is Unit)
                continue;

            // Only check entities with collision enabled
            if (entity.collisionType == CollisionType.NONE)
                continue;

            if (CollisionMath(pos.x, pos.y, size,
                             entity.gridPosition.x, entity.gridPosition.y, entity.size))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a tile is walkable (no blocking entities or unwalkable terrain).
    /// Used by pathfinding.
    /// 
    /// CRITICAL SDK BEHAVIOR:
    /// - If unitToAvoid is null: Player movement - DON'T check mob collisions
    /// - If unitToAvoid is provided: Mob movement - check mob collisions
    /// 
    /// SDK Reference: Pathing.canTileBePathedTo() in Pathing.ts lines 64-78
    /// </summary>
    public static bool IsTileWalkable(Vector2Int pos, int size, Unit unitToAvoid = null)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogWarning("[Collision] GridManager.Instance is null!");
            return false;
        }

        // Check if tile exists and is walkable
        Tile tile = GridManager.Instance.GetTileAt(pos);
        if (tile == null)
        {
            Debug.LogWarning($"[Collision] No tile at {pos}");
            return false;
        }

        if (!tile.isWalkable)
        {
            Debug.LogWarning($"[Collision] Tile at {pos} is not walkable!");
            return false;
        }

        // Check collision with static entities (walls, objects, etc)
        if (CollidesWithAnyEntities(pos, size))
        {
            Debug.LogWarning($"[Collision] Position {pos} blocked by static entity");
            return false;
        }

        // CRITICAL: Only check mob collisions if unitToAvoid is provided
        // SDK Reference: Pathing.ts lines 71-76
        // "if (mobToAvoid) { // Player can walk under mobs"
        if (unitToAvoid != null)
        {
            // This is a MOB checking movement - check for other mobs
            Unit blockingUnit = CollidesWithAnyUnits(pos, size, unitToAvoid);
            if (blockingUnit != null)
            {
                Debug.LogWarning($"[Collision] Position {pos} blocked by unit: {blockingUnit.UnitName()} at {blockingUnit.gridPosition}");
                return false;
            }
        }
        // If unitToAvoid is null, this is a PLAYER - don't check mob collisions at all
        // Players can walk through/under ALL mobs

        return true;
    }

    /// <summary>
    /// Get all entities at a specific point.
    /// SDK Reference: Pathing.entitiesAtPoint() in Pathing.ts lines 19-31
    /// </summary>
    public static List<Entity> EntitiesAtPoint(Vector2Int pos, int size)
    {
        List<Entity> result = new List<Entity>();

        if (GridManager.Instance == null)
            return result;

        List<Entity> allEntities = GridManager.Instance.GetAllEntities();

        foreach (Entity entity in allEntities)
        {
            if (entity is Unit unit)
            {
                if (CollisionMath(pos.x, pos.y, size,
                                 unit.gridPosition.x, unit.gridPosition.y, unit.size))
                {
                    result.Add(entity);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get all entities at a specific point that have collision enabled.
    /// Filters EntitiesAtPoint to only return entities that block movement.
    /// SDK Reference: Collision.collideableEntitiesAtPoint() in Collision.ts lines 43-48
    /// </summary>
    public static List<Entity> CollideableEntitiesAtPoint(Vector2Int pos, int size)
    {
        List<Entity> result = new List<Entity>();

        if (GridManager.Instance == null)
            return result;

        List<Entity> allEntities = GridManager.Instance.GetAllEntities();

        foreach (Entity entity in allEntities)
        {
            // Only include entities with collision enabled
            if (entity.collisionType == CollisionType.NONE)
                continue;

            if (CollisionMath(pos.x, pos.y, size,
                             entity.gridPosition.x, entity.gridPosition.y, entity.size))
            {
                result.Add(entity);
            }
        }

        return result;
    }

    #region Debug Visualization

    /// <summary>
    /// Draw collision bounds for debugging.
    /// </summary>
    public static void DrawCollisionBounds(Vector2Int pos, int size, Color color, float duration = 0.1f)
    {
        if (GridManager.Instance == null)
            return;

        Vector3 worldPos = GridManager.Instance.GridToWorld(pos);
        float tileSize = GridManager.Instance.tileSize;

        Vector3 offset = new Vector3(size * tileSize / 2f, 0.1f, size * tileSize / 2f);
        Vector3 center = worldPos + offset;
        Vector3 extents = new Vector3(size * tileSize / 2f, 0.05f, size * tileSize / 2f);

        Debug.DrawLine(center + new Vector3(-extents.x, 0, -extents.z),
                      center + new Vector3(extents.x, 0, -extents.z), color, duration);
        Debug.DrawLine(center + new Vector3(extents.x, 0, -extents.z),
                      center + new Vector3(extents.x, 0, extents.z), color, duration);
        Debug.DrawLine(center + new Vector3(extents.x, 0, extents.z),
                      center + new Vector3(-extents.x, 0, extents.z), color, duration);
        Debug.DrawLine(center + new Vector3(-extents.x, 0, extents.z),
                      center + new Vector3(-extents.x, 0, -extents.z), color, duration);
    }

    #endregion
}
