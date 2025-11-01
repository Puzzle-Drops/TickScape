using UnityEngine;

/// <summary>
/// Prayer panel with 5x7 grid and quick prayers.
/// SDK Reference: PrayerControls.ts
/// </summary>
public class PrayerPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/prayer_panel";
    protected override string TabTexturePath => "UI/Tabs/prayer_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.prayerKey : KeyCode.F5;

    public override bool IsAvailable => true;

    // Quick prayers state
    // SDK Reference: PrayerControls.ts line 15
    private bool hasQuickPrayersActivated = false;

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return;

        // Draw all prayers in 5x7 grid
        var prayers = player.prayerController.prayers;

        for (int i = 0; i < prayers.Count && i < 35; i++)
        {
            int col = i % 5;
            int row = i / 5;

            float prayerX = x + (10 + col * 36.8f) * scale;
            float prayerY = y + (16 + row * 37) * scale;

            DrawPrayer(prayers[i], prayerX, prayerY, scale, player);
        }
    }

    /// <summary>
    /// Draw a single prayer icon with activation state.
    /// SDK Reference: PrayerControls.draw() in PrayerControls.ts lines 66-89
    /// </summary>
    private void DrawPrayer(Prayer prayer, float x, float y, float scale, Player player)
    {
        // Prayer icons are on the background image, we just draw overlays

        float centerX = x + (18 * scale);
        float centerY = y + (18 * scale);
        float radius = 18 * scale;

        // Draw activation highlight (golden circle)
        if (prayer.isActive)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.82f, 0.73f, 0.47f, 0.45f); // #D1BB7773 from SDK

            Rect circleRect = new Rect(x, y, 36 * scale, 36 * scale);
            GUI.DrawTexture(circleRect, Texture2D.whiteTexture);

            GUI.color = oldColor;
        }

        // Draw level requirement overlay (dark if can't use)
        if (player.stats.prayer < prayer.GetLevelRequirement())
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.45f); // 45% black overlay

            Rect circleRect = new Rect(x, y, 36 * scale, 36 * scale);
            GUI.DrawTexture(circleRect, Texture2D.whiteTexture);

            GUI.color = oldColor;
        }

        // Draw prayer name on hover (for debugging)
        // TODO: Replace with proper tooltip system
        Vector2 mousePos = Event.current.mousePosition;
        Rect prayerRect = new Rect(x, y, 36 * scale, 36 * scale);

        if (prayerRect.Contains(mousePos))
        {
            GUIStyle tooltipStyle = new GUIStyle();
            tooltipStyle.fontSize = Mathf.RoundToInt(12 * scale);
            tooltipStyle.normal.textColor = Color.yellow;
            tooltipStyle.normal.background = Texture2D.blackTexture;

            GUI.Label(new Rect(mousePos.x + 10, mousePos.y - 20, 150, 20),
                      prayer.GetName(), tooltipStyle);
        }
    }

    /// <summary>
    /// Handle prayer clicks to activate/deactivate.
    /// SDK Reference: PrayerControls.panelClickDown() in PrayerControls.ts lines 36-52
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return;

        // Check if out of prayer points
        if (player.currentStats.prayer <= 0)
        {
            Debug.Log("[PrayerPanel] Out of prayer points!");
            return;
        }

        // Calculate grid position
        float gridX = relativeX - 14;
        float gridY = relativeY - 22;

        int col = Mathf.FloorToInt(gridX / 35);
        int row = Mathf.FloorToInt(gridY / 35);
        int prayerIndex = row * 5 + col;

        // Check bounds
        if (prayerIndex < 0 || prayerIndex >= player.prayerController.prayers.Count)
            return;

        Prayer clickedPrayer = player.prayerController.prayers[prayerIndex];

        // Check level requirement
        if (player.stats.prayer < clickedPrayer.GetLevelRequirement())
        {
            Debug.Log($"[PrayerPanel] Need {clickedPrayer.GetLevelRequirement()} Prayer to use {clickedPrayer.GetName()}");
            return;
        }

        // Toggle prayer
        TogglePrayer(clickedPrayer, player);
    }

    /// <summary>
    /// Toggle prayer on/off with group logic.
    /// SDK Reference: Prayer.toggle() in BasePrayer.ts
    /// </summary>
    private void TogglePrayer(Prayer prayer, Player player)
    {
        if (prayer.isActive)
        {
            // Deactivate
            prayer.isActive = false;
            Debug.Log($"[PrayerPanel] Deactivated {prayer.GetName()}");
        }
        else
        {
            // Deactivate conflicting prayers in same groups
            var groups = prayer.GetGroups();
            foreach (var group in groups)
            {
                var conflicting = player.prayerController.MatchGroup(group);
                if (conflicting != null && conflicting != prayer)
                {
                    conflicting.isActive = false;
                    Debug.Log($"[PrayerPanel] Deactivated conflicting {conflicting.GetName()}");
                }
            }

            // Activate
            prayer.isActive = true;
            Debug.Log($"[PrayerPanel] Activated {prayer.GetName()}");
        }

        // Update quick prayers state
        if (hasQuickPrayersActivated && player.prayerController.ActivePrayers().Count == 0)
        {
            hasQuickPrayersActivated = false;
        }
    }

    /// <summary>
    /// Deactivate all prayers.
    /// SDK Reference: PrayerControls.deactivateAllPrayers() in PrayerControls.ts lines 26-30
    /// </summary>
    public void DeactivateAllPrayers(Player player)
    {
        hasQuickPrayersActivated = false;
        if (player != null && player.prayerController != null)
        {
            player.prayerController.DeactivateAll();
        }
    }

    /// <summary>
    /// Activate quick prayers (preset selection).
    /// SDK Reference: PrayerControls.activateQuickPrayers() in PrayerControls.ts lines 32-42
    /// 
    /// TODO: Make this configurable. For now, activates common PvM prayers.
    /// </summary>
    public void ActivateQuickPrayers(Player player)
    {
        hasQuickPrayersActivated = true;

        if (player != null && player.prayerController != null)
        {
            // Activate Protect from Magic and Rigour as default quick prayers
            Prayer protMage = player.prayerController.FindPrayerByType(PrayerType.PROTECT_FROM_MAGIC);
            Prayer rigour = player.prayerController.FindPrayerByType(PrayerType.RIGOUR);

            if (protMage != null)
            {
                protMage.isActive = true;
                Debug.Log("[PrayerPanel] Quick Prayer: Protect from Magic");
            }

            if (rigour != null)
            {
                rigour.isActive = true;
                Debug.Log("[PrayerPanel] Quick Prayer: Rigour");
            }
        }
    }

    private Player FindPlayer()
    {
        return Object.FindObjectOfType<Player>();
    }
}