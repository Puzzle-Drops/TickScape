using UnityEngine;

/// <summary>
/// Manta - Hard food that heals 22 HP.
/// </summary>
public class Manta : Food
{
    public Manta()
    {
        healAmount = 22;
        Weight = 0.226f;
        defaultAction = "Eat";
        itemName = ItemName.MANTA;
    }
}