using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages delayed input actions with queue system.
/// Simulates network latency for authentic OSRS feel.
/// SDK Reference: InputController.ts
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Queue Settings")]
    [Tooltip("Maximum actions that can be queued")]
    private const int MAX_QUEUED_ACTIONS = 8;

    [Header("State")]
    private Queue<System.Action> actionQueue = new Queue<System.Action>();
    private List<System.Action> actionsToExecute = new List<System.Action>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Queue an action to be executed after input delay.
    /// SDK Reference: InputController.queueAction() in Input.ts
    /// </summary>
    public void QueueAction(System.Action action)
    {
        if (actionQueue.Count >= MAX_QUEUED_ACTIONS)
        {
            Debug.LogWarning("[InputManager] Action queue full! Ignoring action.");
            return;
        }

        // Get input delay from settings
        float delaySeconds = UISettings.Instance != null 
            ? UISettings.Instance.inputDelay / 1000f 
            : 0.02f; // Default 20ms

        StartCoroutine(DelayedQueueCoroutine(action, delaySeconds));
    }

    /// <summary>
    /// Coroutine to delay action queuing.
    /// </summary>
    private IEnumerator DelayedQueueCoroutine(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        actionQueue.Enqueue(action);
    }

    /// <summary>
    /// Execute all queued actions on world tick.
    /// Called by WorldManager each game tick.
    /// SDK Reference: InputController.onWorldTick() in Input.ts
    /// </summary>
    public void OnWorldTick()
    {
        // Move queue to execution list (thread-safe)
        actionsToExecute.Clear();
        while (actionQueue.Count > 0)
        {
            actionsToExecute.Add(actionQueue.Dequeue());
        }

        // Execute all actions
        foreach (var action in actionsToExecute)
        {
            try
            {
                action.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputManager] Error executing action: {e.Message}");
            }
        }

        if (actionsToExecute.Count > 0)
        {
            Debug.Log($"[InputManager] Executed {actionsToExecute.Count} queued actions");
        }
    }

    /// <summary>
    /// Get current queue size (for debugging).
    /// </summary>
    public int GetQueueSize()
    {
        return actionQueue.Count;
    }
}