using UnityEngine;

/// <summary>
/// Renders hitsplats above units.
/// Add this component to Units that need to display damage.
/// </summary>
[RequireComponent(typeof(Unit))]
public class HitsplatRenderer : MonoBehaviour
{
    private Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        // Initialize hitsplat queue if needed
        if (unit.hitsplatQueue == null)
        {
            unit.hitsplatQueue = new System.Collections.Generic.List<Hitsplat>();
        }
    }

    void OnGUI()
    {
        if (unit == null || unit.hitsplatQueue == null)
            return;

        // Remove old hitsplats
        unit.hitsplatQueue.RemoveAll(h => h.age > Hitsplat.LIFETIME);

        // Update and draw each hitsplat
        foreach (var hitsplat in unit.hitsplatQueue)
        {
            // Update age
            hitsplat.age += Time.deltaTime;

            // Calculate position
            Vector3 worldPos = transform.position + Vector3.up * (2.0f + hitsplat.offset.y);
            worldPos.y += hitsplat.age * Hitsplat.RISE_SPEED;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Skip if behind camera
            if (screenPos.z < 0)
                continue;

            // Calculate alpha (fade out)
            float alpha = 1.0f;
            if (hitsplat.age > Hitsplat.LIFETIME * 0.75f)
            {
                alpha = 1.0f - (hitsplat.age - Hitsplat.LIFETIME * 0.75f) / (Hitsplat.LIFETIME * 0.25f);
            }

            // Draw hitsplat
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;

            Color displayColor = hitsplat.color;
            displayColor.a = alpha;
            style.normal.textColor = displayColor;

            // Add shadow for visibility
            Rect shadowRect = new Rect(screenPos.x - 48, Screen.height - screenPos.y - 18, 100, 40);
            style.normal.textColor = new Color(0, 0, 0, alpha * 0.5f);
            GUI.Label(shadowRect, hitsplat.damage.ToString(), style);

            // Draw main text
            Rect rect = new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 40);
            style.normal.textColor = displayColor;
            GUI.Label(rect, hitsplat.damage.ToString(), style);
        }
    }
}