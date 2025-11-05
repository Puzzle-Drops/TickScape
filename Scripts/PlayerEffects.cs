using UnityEngine;

/// <summary>
/// Player effects (poison, venom, stamina).
/// SDK Reference: PlayerEffects class in Player.ts lines 39-43
/// </summary>
[System.Serializable]
public class PlayerEffects
{
    [Tooltip("Poison timer (ticks remaining)")]
    public int poisoned = 0;

    [Tooltip("Venom timer (ticks remaining)")]
    public int venomed = 0;

    [Tooltip("Stamina effect timer (ticks remaining, 200 = 2 minutes)")]
    public int stamina = 0;
}