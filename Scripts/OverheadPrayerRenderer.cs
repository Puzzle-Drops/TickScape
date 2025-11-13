using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders overhead prayer icons as UI elements above units.
/// Follows the unit's actual transform position (already interpolated).
/// SDK Reference: Unit.drawOverheadPrayers() in Unit.ts lines 501-513
/// </summary>
[RequireComponent(typeof(Unit))]
public class OverheadPrayerRenderer : MonoBehaviour
{
    private Unit unit;

    // Cache loaded textures
    private static Dictionary<string, Texture2D> textureCache =
        new Dictionary<string, Texture2D>();

    // Icon settings
    private const float ICON_SIZE = 48f; // 150% of original 32px
    private const float HEIGHT_OFFSET = 2.5f; // World units above unit

    // Cache transform for performance
    private Transform unitTransform;
    private Camera mainCamera;

    void Start()
    {
        unit = GetComponent<Unit>();
        unitTransform = transform;
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Render overhead prayer using the unit's current transform position.
    /// This avoids recalculating perceived location and ensures synchronization.
    /// </summary>
    void OnGUI()
    {
        // Skip if no camera, unit, or prayer controller
        if (mainCamera == null || unit == null || unit.prayerController == null)
            return;

        // Skip if unit is dying
        if (unit.IsDying())
            return;

        Prayer overhead = unit.prayerController.GetOverhead();

        if (overhead != null && overhead.isActive)
        {
            // Get texture
            Texture2D prayerTexture = GetOverheadTexture(overhead);

            if (prayerTexture != null)
            {
                // Use the unit's ACTUAL transform position (already interpolated)
                // Just add height offset - ignore rotation completely
                Vector3 worldPos = unitTransform.position + Vector3.up * HEIGHT_OFFSET;

                // Convert world position to screen position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // Check if position is on screen (and in front of camera)
                if (screenPos.z > 0 &&
                    screenPos.x >= -ICON_SIZE && screenPos.x <= Screen.width + ICON_SIZE &&
                    screenPos.y >= -ICON_SIZE && screenPos.y <= Screen.height + ICON_SIZE)
                {
                    // Invert Y coordinate (OnGUI uses top-left origin)
                    screenPos.y = Screen.height - screenPos.y;

                    // Draw the overhead prayer icon centered at position
                    Rect iconRect = new Rect(
                        screenPos.x - ICON_SIZE / 2f,
                        screenPos.y - ICON_SIZE / 2f,
                        ICON_SIZE,
                        ICON_SIZE
                    );

                    GUI.DrawTexture(iconRect, prayerTexture);
                }
            }
        }
    }

    /// <summary>
    /// Load overhead prayer texture from Resources.
    /// Caches textures for performance.
    /// </summary>
    private Texture2D GetOverheadTexture(Prayer prayer)
    {
        string textureName = prayer.GetSpriteName() + "_overhead";

        if (!textureCache.ContainsKey(textureName))
        {
            // Load from Resources/UI/Prayers/
            Texture2D texture = Resources.Load<Texture2D>($"UI/Prayers/{textureName}");

            if (texture != null)
            {
                textureCache[textureName] = texture;
            }
            else
            {
                textureCache[textureName] = null; // Cache the null to avoid repeated loads
            }
        }

        return textureCache[textureName];
    }
}