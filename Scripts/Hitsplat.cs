using UnityEngine;

/// <summary>
/// Visual damage number that appears above units when hit.
/// Matches OSRS hitsplat behavior.
/// </summary>
[System.Serializable]
public class Hitsplat
{
    public int damage;
    public Color color;
    public float age; // Time since created
    public Vector2 offset; // For stacking multiple hitsplats

    // Constants
    public const float LIFETIME = 2.0f; // Seconds before fading
    public const float RISE_SPEED = 0.5f; // Units per second
}