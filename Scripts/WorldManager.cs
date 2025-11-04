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
        // This MUST happen every tick or pathfinding uses stale tile data!
        // Without this, changing tile properties (like isWalkable) won't affect pathing!
        Pathing.PurgeTileCache();

        // Get all entities from GridManager
        var entities = GridManager.Instance.GetAllEntities();

        // ===== PHASE 0: Save Positions for Lerping =====
        foreach (var entity in entities)
        {
            entity.OnTickStart();
        }

        // ===== PHASE 1: Timer Step (Pre-Movement) =====
        // SDK Reference: Unit.timerStep() is called before movement
        foreach (var entity in entities)
        {
            if (entity is Unit unit)
            {
                unit.TimerStep();
            }
        }

        // ===== PHASE 2: Movement Step =====
        // SDK Reference: Unit.movementStep() handles all movement
        foreach (var entity in entities)
        {
            if (entity is Unit unit)
            {
                unit.MovementStep();
            }
            else
            {
                // Non-combat entities still use simple Tick()
                entity.Tick();
            }
        }

        // ===== PHASE 3: Attack Step (Combat & Death) =====
        // SDK Reference: Unit.attackStep() handles combat and death detection
        foreach (var entity in entities)
        {
            if (entity is Unit unit)
            {
                unit.AttackStep();
            }
        }

        // ===== PHASE 4: Cleanup Dead Units =====
        // Remove any units marked for destruction (dying == 0)
        // We do this AFTER the loop to avoid "collection modified" errors
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

        // Now remove them (this calls RemovedFromWorld internally)
        foreach (var entity in toRemove)
        {
            Debug.Log($"Removing dead unit: {(entity as Unit).UnitName()}");
            // Don't call RemovedFromWorld here - it will be called by OnDestroy
            // Just destroy the gameobject
            Destroy(entity.gameObject);
        }

        // Debug feedback every tick (for debugging walkability issues)
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
