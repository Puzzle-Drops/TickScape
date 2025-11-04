using UnityEngine;

/// <summary>
/// Random number generator wrapper for combat calculations.
/// SDK Reference: Random.ts
/// </summary>
public static class RandomHelper
{
    private static int callCount = 0;

    /// <summary>
    /// Get random value between 0.0 and 1.0.
    /// SDK Reference: Random.get() in Random.ts
    /// </summary>
    public static float Get()
    {
        callCount++;
        return UnityEngine.Random.value;
    }

    /// <summary>
    /// Reset call counter (for testing).
    /// </summary>
    public static void Reset()
    {
        callCount = 0;
    }

    /// <summary>
    /// Get current call count (for debugging).
    /// </summary>
    public static int GetCallCount()
    {
        return callCount;
    }
}
