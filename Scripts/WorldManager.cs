using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the game tick system with three-phase combat updates.
/// Matches SDK's World.ts tick loop.
/// SDK Reference: World.ts lines 80-120
/// </summary>
public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("Tick Settings")]
    [Tooltip("Time between game ticks in seconds. SDK uses 0.6s (600ms)")]
    public float tickInterval = 0.6f;

    private float tickTimer = 0f;
    private int tickCounter = 0;

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

    void Update()
    {
        // Accumulate time
        tickTimer += Time.deltaTime;

        // Tick when interval reached
        if (tickTimer >= tickInterval)
        {
            DoTick();
            tickTimer = 0f;
        }
    }

    /// <summary>
    /// Execute one game tick with three-phase system for combat.
    /// SDK Reference: World.ts lines 124-167
    /// CRITICAL: NPCs complete ALL phases before players!
    /// </summary>
    void DoTick()
    {
        tickCounter++;

        if (GridManager.Instance == null)
        {
            Debug.LogWarning("WorldManager: No GridManager found, skipping tick");
            return;
        }

        // ===== CRITICAL FIX: Purge pathfinding cache =====
        // SDK Reference: World.ts line 125
        Pathing.PurgeTileCache();

        // Get all entities from GridManager
        var entities = GridManager.Instance.GetAllEntities();

        // ===== PHASE 0: Save Positions for Lerping =====
        foreach (var entity in entities)
        {
            entity.OnTickStart();
        }

        // ===== NPCs/MOBS COMPLETE THEIR FULL CYCLE FIRST =====
        // SDK Reference: World.ts lines 127-145

        // Phase 1: NPC Timer Step
        foreach (var entity in entities)
        {
            if (entity is Unit unit && !(entity is Player))
            {
                unit.TimerStep();
            }
        }

        // Phase 2: NPC Movement Step
        foreach (var entity in entities)
        {
            if (entity is Unit unit && !(entity is Player))
            {
                unit.MovementStep();
            }
            else if (!(entity is Unit))
            {
                // Non-combat entities still use simple Tick()
                entity.Tick();
            }
        }

        // Phase 3: NPC Attack Step
        foreach (var entity in entities)
        {
            if (entity is Unit unit && !(entity is Player))
            {
                unit.AttackStep();
            }
        }

        // ===== NOW PLAYERS COMPLETE THEIR FULL CYCLE =====
        // SDK Reference: World.ts lines 157-163

        // Find all players
        List<Player> players = new List<Player>();
        foreach (var entity in entities)
        {
            if (entity is Player player)
            {
                players.Add(player);
            }
        }

        // Phase 1: Player Timer Step
        foreach (var player in players)
        {
            player.TimerStep();
        }

        // Phase 2: Player Movement Step
        foreach (var player in players)
        {
            player.MovementStep();
        }

        // Phase 3: Player Attack Step
        foreach (var player in players)
        {
            player.AttackStep();
        }

        // ===== PHASE 4: Cleanup Dead Units =====
        List<Entity> toRemove = new List<Entity>();
        foreach (var entity in entities)
        {
            if (entity is Unit unit && unit.ShouldDestroy())
            {
                toRemove.Add(entity);
            }
        }

        // ===== PHASE 5: UI Tick =====
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWorldTick();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWorldTick();
        }

        // Now remove dead entities
        foreach (var entity in toRemove)
        {
            Debug.Log($"Removing dead unit: {(entity as Unit).UnitName()}");
            Destroy(entity.gameObject);
        }

        // Debug feedback
        Debug.Log($"[TICK #{tickCounter}] {entities.Count} entities active");
    }

    /// <summary>
    /// Get current interpolation value between ticks (0.0 to 1.0).
    /// Used by Entity.LateUpdate() for smooth visual movement.
    /// SDK Reference: Used in Unit.getPerceivedLocation() for lerping
    /// </summary>
    public float GetTickPercent()
    {
        return Mathf.Clamp01(tickTimer / tickInterval);
    }

    /// <summary>
    /// Get current tick number (for debugging).
    /// </summary>
    public int GetTickCounter()
    {
        return tickCounter;
    }
}
