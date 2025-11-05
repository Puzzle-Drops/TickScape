using UnityEngine;

/// <summary>
/// Ray - Hard food that heals 22 HP.
/// </summary>
public class Ray : Food
{
    public Ray()
    {
        healAmount = 22;
        Weight = 0.226f;
        defaultAction = "Eat";
        itemName = ItemName.RAY;
    }
}
