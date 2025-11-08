using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Container for compass, quick prayers, and run orbs.
/// Positioned left of the HP bar.
/// </summary>
public static class OrbsPanel
{
    // Cached textures
    private static Texture2D mapNumOrb;
    private static Texture2D mapSelectedNumOrb;
    private static Texture2D mapPrayerOrb;
    private static Texture2D mapPrayerOnOrb;
    private static Texture2D mapPrayerIcon;
    private static Texture2D mapRunOrb;
    private static Texture2D mapNoSpecOrb;
    private static Texture2D mapRunIcon;
    private static Texture2D mapWalkIcon;
    private static Texture2D mapStamIcon;
    private static Texture2D compassIcon;
    private static Texture2D mapComOrb;
    private static Texture2D mapSelectedComOrb;

    private static bool texturesLoaded = false;

    // Hover states
    private enum OrbHover
    {
        NONE,
        COMPASS,
        PRAYER,
        RUN
    }
    private static OrbHover hovering = OrbHover.NONE;

    /// <summary>
    /// Load all orb textures.
    /// </summary>
    private static void LoadTextures()
    {
        if (texturesLoaded) return;

        mapNumOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_num_orb", Color.gray, 57, 34);
        mapSelectedNumOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_selected_num_orb", Color.yellow, 57, 34);
        mapPrayerOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_prayer_orb", Color.cyan, 26, 26);
        mapPrayerOnOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_prayer_on_orb", new Color(0.8f, 0.7f, 0.4f), 26, 26);
        mapPrayerIcon = TextureLoader.LoadTexture("UI/MapOrbs/map_prayer_icon", Color.cyan, 26, 26);
        mapRunOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_run_orb", new Color(0.8f, 0.8f, 0.2f), 26, 26);
        mapNoSpecOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_no_spec_orb", Color.gray, 26, 26);
        mapRunIcon = TextureLoader.LoadTexture("UI/MapOrbs/map_run_icon", Color.yellow, 26, 26);
        mapWalkIcon = TextureLoader.LoadTexture("UI/MapOrbs/map_walk_icon", Color.gray, 26, 26);
        mapStamIcon = TextureLoader.LoadTexture("UI/MapOrbs/map_stam_icon", Color.green, 26, 26);
        compassIcon = TextureLoader.LoadTexture("UI/MapOrbs/compass_icon", Color.white, 51, 51);
        mapComOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_com_orb", Color.gray, 34, 34);
        mapSelectedComOrb = TextureLoader.LoadTexture("UI/MapOrbs/map_selected_com_orb", Color.yellow, 34, 34);

        texturesLoaded = true;
    }

    /// <summary>
    /// Draw all orbs.
    /// </summary>
    public static void Draw(float scale, Player player, bool panelsMinimized)
    {
        if (player == null) return;

        LoadTextures();

        // Calculate base position (left of HP bar with 6px gap)
        Vector2 basePos;
        float orbWidth = 57f * scale;  // Width of orb background

        if (panelsMinimized)
        {
            // When minimized: orbs are left of HP bar which is left of prayer bar
            Vector2 prayerBarPos = UILayout.GetPrayerBarPosition();
            float hpBarWidth = 16f * scale;
            // Prayer bar -> 4px gap -> HP bar -> 6px gap -> Orbs
            basePos = new Vector2(
                prayerBarPos.x - hpBarWidth - (4f * scale) - orbWidth - (6f * scale),
                prayerBarPos.y
            );
        }
        else
        {
            // Normal layout: HP bar is directly attached to panel (no gap)
            // So we need: Panel -> HP bar (attached) -> 6px gap -> Orbs
            Vector2 panelPos = UILayout.GetPanelPosition();
            float hpBarWidth = 16f * scale;

            // FIX: Calculate from panel position, not HP position
            basePos = new Vector2(
                panelPos.x - hpBarWidth - orbWidth - (6f * scale),  // CHANGED: Use panel position
                panelPos.y
            );
        }

        // Update hover state
        UpdateHover(basePos, scale);

        // Draw compass at top (34x34, right-aligned with orbs)
        // Right-align by adding 23px offset (57 - 34 = 23)
        DrawCompass(basePos.x + (23f * scale), basePos.y, scale, player);

        // Draw prayer orb below compass
        float prayerY = basePos.y + (38f * scale);  // Below smaller compass
        DrawPrayerOrb(basePos.x, prayerY, scale, player);

        // Draw run orb below prayer orb  
        float runY = prayerY + (38f * scale);
        DrawRunOrb(basePos.x, runY, scale, player);
    }

    /// <summary>
    /// Draw compass with background orb and rotation based on camera yaw.
    /// </summary>
    private static void DrawCompass(float x, float y, float scale, Player player)
    {
        if (compassIcon == null) return;

        // Draw background orb (34x34)
        Texture2D orbBg = hovering == OrbHover.COMPASS ? mapSelectedComOrb : mapComOrb;
        float bgSize = 34f * scale;
        GUI.DrawTexture(new Rect(x, y, bgSize, bgSize), orbBg);

        // Draw compass icon (26x26, right-aligned within background -4px)
        float compassSize = 26f * scale;
        // Right edge of background is at x + bgSize
        // Compass should be at right edge - compassSize - 4px
        float compassX = x + bgSize - compassSize - (4f * scale);
        float compassY = y + (4f * scale); // Center vertically with 4px padding
        Rect compassRect = new Rect(compassX, compassY, compassSize, compassSize);

        // Get camera yaw - compass rotates to show direction
        float yawDegrees = 0f;
        if (Camera.main != null)
        {
            CameraController camController = Object.FindAnyObjectByType<CameraController>();
            if (camController != null)
            {
                // Get the yaw and rotate compass opposite to show correct direction
                yawDegrees = camController.GetYawDegrees();
            }
        }

        // Draw rotated compass icon
        if (Mathf.Abs(yawDegrees) > 0.01f)
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            Vector2 pivot = new Vector2(compassX + compassSize / 2f, compassY + compassSize / 2f);
            GUIUtility.RotateAroundPivot(-yawDegrees, pivot);
            GUI.DrawTexture(compassRect, compassIcon);
            GUI.matrix = oldMatrix;
        }
        else
        {
            GUI.DrawTexture(compassRect, compassIcon);
        }
    }

    /// <summary>
    /// Draw prayer orb.
    /// </summary>
    private static void DrawPrayerOrb(float x, float y, float scale, Player player)
    {
        PlayerStats pStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;
        if (pStats == null || baseStats == null) return;

        // Draw background orb (57x34)
        Texture2D orbBg = hovering == OrbHover.PRAYER ? mapSelectedNumOrb : mapNumOrb;
        float orbWidth = 57f * scale;
        float orbHeight = 34f * scale;
        GUI.DrawTexture(new Rect(x, y, orbWidth, orbHeight), orbBg);

        // Calculate prayer fill percentage
        float fillPercent = Mathf.Clamp01((float)pStats.prayer / (float)baseStats.prayer);

        // Draw filled prayer orb (right-aligned within background, -4px)
        float orbSize = 26f * scale;
        // Right edge of background is at x + orbWidth
        // Orb should be at right edge - orbSize - 4px
        float orbX = x + orbWidth - orbSize - (4f * scale);  // CHANGED: Right-aligned -4px
        float orbY = y + (4f * scale);

        // Use different orb texture if quick prayers active
        Texture2D prayerOrbTexture = (player.prayerController != null && player.prayerController.hasQuickPrayersActivated)
            ? mapPrayerOnOrb : mapPrayerOrb;

        // Draw filled portion (masked to percentage)
        if (fillPercent > 0.01f)
        {
            float fillHeight = orbSize * fillPercent;
            GUI.BeginClip(new Rect(orbX, orbY + (orbSize - fillHeight), orbSize, fillHeight));
            GUI.DrawTexture(new Rect(0, -(orbSize - fillHeight), orbSize, orbSize), prayerOrbTexture);
            GUI.EndClip();
        }

        // Draw icon
        GUI.DrawTexture(new Rect(orbX, orbY, orbSize, orbSize), mapPrayerIcon);

        // Draw text (on the left side, moved down 4px)
        string prayerText = pStats.prayer.ToString();
        GUIStyle textStyle = UIFonts.CreateTextStyle(
            Mathf.RoundToInt(16 * scale),
            Color.white,
            TextAnchor.MiddleCenter
        );

        Color textColor = GetOrbTextColor(fillPercent);
        textStyle.normal.textColor = textColor;

        // Text positioned on left side of background
        Rect textRect = new Rect(x - (3f * scale), y + (4f * scale), 35f * scale, orbHeight);  // CHANGED: +4px down
        UIFonts.DrawShadowedText(textRect, prayerText, textStyle, scale);
    }

    /// <summary>
    /// Draw run orb.
    /// </summary>
    private static void DrawRunOrb(float x, float y, float scale, Player player)
    {
        PlayerStats pStats = player.currentStats as PlayerStats;
        if (pStats == null) return;

        // Draw background orb (57x34)
        Texture2D orbBg = hovering == OrbHover.RUN ? mapSelectedNumOrb : mapNumOrb;
        float orbWidth = 57f * scale;
        float orbHeight = 34f * scale;
        GUI.DrawTexture(new Rect(x, y, orbWidth, orbHeight), orbBg);

        // Calculate run fill percentage
        float fillPercent = Mathf.Clamp01(pStats.run / 10000f);

        // Draw filled run orb (right-aligned within background, -4px)
        float orbSize = 26f * scale;
        // Right edge of background is at x + orbWidth
        // Orb should be at right edge - orbSize - 4px
        float orbX = x + orbWidth - orbSize - (4f * scale);  // CHANGED: Right-aligned -4px
        float orbY = y + (4f * scale);

        // Choose orb texture based on running state
        Texture2D runOrbTexture = player.running ? mapRunOrb : mapNoSpecOrb;

        // Draw filled portion (masked to percentage)
        if (fillPercent > 0.01f)
        {
            float fillHeight = orbSize * fillPercent;
            GUI.BeginClip(new Rect(orbX, orbY + (orbSize - fillHeight), orbSize, fillHeight));
            GUI.DrawTexture(new Rect(0, -(orbSize - fillHeight), orbSize, orbSize), runOrbTexture);
            GUI.EndClip();
        }

        // Draw icon (walk/run/stamina)
        Texture2D icon;
        if (player.effects.stamina > 0)
            icon = mapStamIcon;
        else if (player.running)
            icon = mapRunIcon;
        else
            icon = mapWalkIcon;

        GUI.DrawTexture(new Rect(orbX, orbY, orbSize, orbSize), icon);

        // Draw text (on the left side, moved down 4px)
        int runPercent = Mathf.FloorToInt(pStats.run / 100f);
        string runText = runPercent.ToString();
        GUIStyle textStyle = UIFonts.CreateTextStyle(
            Mathf.RoundToInt(16 * scale),
            Color.white,
            TextAnchor.MiddleCenter
        );

        Color textColor = GetOrbTextColor(fillPercent);
        textStyle.normal.textColor = textColor;

        // Text positioned on left side of background
        Rect textRect = new Rect(x - (3f * scale), y + (4f * scale), 35f * scale, orbHeight);  // CHANGED: +4px down
        UIFonts.DrawShadowedText(textRect, runText, textStyle, scale);
    }

    /// <summary>
    /// Update hover state based on mouse position.
    /// </summary>
    private static void UpdateHover(Vector2 basePos, float scale)
    {
        Vector2 mousePos = Event.current.mousePosition;
        hovering = OrbHover.NONE;

        // Check compass (34x34, offset by 23px for right-alignment)
        Rect compassRect = new Rect(basePos.x + (23f * scale), basePos.y, 34f * scale, 34f * scale);
        if (compassRect.Contains(mousePos))
        {
            hovering = OrbHover.COMPASS;
            return;
        }

        // Check prayer orb
        float prayerY = basePos.y + (38f * scale);
        Rect prayerRect = new Rect(basePos.x, prayerY, 57f * scale, 34f * scale);
        if (prayerRect.Contains(mousePos))
        {
            hovering = OrbHover.PRAYER;
            return;
        }

        // Check run orb
        float runY = prayerY + (38f * scale);
        Rect runRect = new Rect(basePos.x, runY, 57f * scale, 34f * scale);
        if (runRect.Contains(mousePos))
        {
            hovering = OrbHover.RUN;
        }
    }

    /// <summary>
    /// Check if mouse is over any orb.
    /// </summary>
    public static bool IsMouseOverOrbs(Vector2 guiMousePos)
    {
        return hovering != OrbHover.NONE;
    }

    /// <summary>
    /// Handle left click on orbs.
    /// </summary>
    public static bool HandleClick(Vector2 guiMousePos, Player player)
    {
        if (hovering == OrbHover.COMPASS)
        {
            // Left-click compass to face north
            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
            {
                cam.SetCameraDirection(false); // false = face north
                Debug.Log("[Compass] Camera set to face north");
            }
            else
            {
                Debug.LogError("[Compass] Could not find CameraController!");
            }
            return true;
        }
        else if (hovering == OrbHover.PRAYER)
        {
            // Toggle quick prayers
            if (player.prayerController != null)
            {
                player.prayerController.ToggleQuickPrayers();
            }
            return true;
        }
        else if (hovering == OrbHover.RUN)
        {
            // Toggle running
            player.running = !player.running;
            Debug.Log($"[Run] Running: {player.running}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handle right click on orbs.
    /// </summary>
    public static bool HandleRightClick(Vector2 screenPos, Player player)
    {
        if (hovering == OrbHover.COMPASS)
        {
            // Right-click compass to face south
            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
            {
                cam.SetCameraDirection(true); // true = south
                Debug.Log("[Compass] Camera set to face south");
            }
            return true;
        }
        else if (hovering == OrbHover.RUN)
        {
            // Right-click also toggles run (same as left click)
            player.running = !player.running;
            Debug.Log($"[Run] Running: {player.running}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get color for orb text based on fill percentage.
    /// </summary>
    private static Color GetOrbTextColor(float fillPercent)
    {
        if (fillPercent > 0.5f)
            return new Color(0, 1, 0); // Green
        else if (fillPercent > 0.25f)
            return new Color(1, 0.73f, 0); // Yellow
        else
            return new Color(1, 0, 0); // Red
    }
}