using UnityEngine;

/// <summary>
/// Stats panel showing player stats with click-to-edit functionality.
/// SDK Reference: StatsControls.ts
/// </summary>
public class StatsPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/stats_panel";
    protected override string TabTexturePath => "UI/Tabs/stats_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? KeyCode.F2 : KeyCode.None;

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // OSRS font style
    private GUIStyle statStyle;
    private GUIStyle statStyleShadow;

    public override void Initialize()
    {
        base.Initialize();

        // Create stat text style
        statStyle = new GUIStyle();
        statStyle.fontSize = 16;
        statStyle.normal.textColor = Color.yellow;
        statStyle.alignment = TextAnchor.MiddleLeft;

        statStyleShadow = new GUIStyle(statStyle);
        statStyleShadow.normal.textColor = Color.black;
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Update font size based on scale
        statStyle.fontSize = Mathf.RoundToInt(16 * scale);
        statStyleShadow.fontSize = statStyle.fontSize;

        // Left column (Attack, Strength, Defence, Range, Prayer, Magic)
        DrawStat(player, "attack", x + 48 * scale, y + 21 * scale, scale);
        DrawStat(player, "strength", x + 48 * scale, y + (21 + 32) * scale, scale);
        DrawStat(player, "defence", x + 48 * scale, y + (21 + 64) * scale, scale);
        DrawStat(player, "range", x + 48 * scale, y + (21 + 96) * scale, scale);
        DrawStat(player, "prayer", x + 48 * scale, y + (21 + 128) * scale, scale);
        DrawStat(player, "magic", x + 48 * scale, y + (21 + 160) * scale, scale);

        // Right column (Hitpoint, Agility)
        DrawStat(player, "hitpoint", x + (48 + 63) * scale, y + 21 * scale, scale);

        // Agility (only show if PlayerStats)
        if (player.currentStats is PlayerStats)
        {
            DrawStat(player, "agility", x + (48 + 63) * scale, y + (21 + 32) * scale, scale);
        }
    }

    /// <summary>
    /// Draw a single stat with current/base values.
    /// SDK Reference: StatsControls.draw() in StatsControls.ts lines 85-176
    /// </summary>
    private void DrawStat(Player player, string statName, float x, float y, float scale)
    {
        int currentValue = GetStatValue(player.currentStats, statName);
        int baseValue = GetStatValue(player.stats, statName);

        // Current stat (top line) - shadow then main
        GUI.Label(new Rect(x + 1, y + 1, 50, 20), currentValue.ToString(), statStyleShadow);
        GUI.Label(new Rect(x, y, 50, 20), currentValue.ToString(), statStyle);

        // Base stat (bottom line) - shadow then main
        GUI.Label(new Rect(x + 13, y + 15, 50, 20), baseValue.ToString(), statStyleShadow);
        GUI.Label(new Rect(x + 12, y + 14, 50, 20), baseValue.ToString(), statStyle);
    }

    /// <summary>
    /// Get stat value by name using reflection.
    /// </summary>
    private int GetStatValue(UnitStats stats, string statName)
    {
        if (stats == null) return 0;

        switch (statName.ToLower())
        {
            case "attack": return stats.attack;
            case "strength": return stats.strength;
            case "defence": return stats.defence;
            case "range": return stats.range;
            case "magic": return stats.magic;
            case "hitpoint": return stats.hitpoint;
            case "prayer": return stats.prayer;
            case "agility":
                if (stats is PlayerStats pStats)
                    return pStats.agility;
                return 0;
            default: return 0;
        }
    }

    /// <summary>
    /// Handle clicks to edit stats.
    /// SDK Reference: StatsControls.panelClickUp() in StatsControls.ts lines 42-83
    /// </summary>
    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        // Left column clickable areas
        if (relativeX > 9 && relativeX < 73)
        {
            if (relativeY > 12 && relativeY < 44)
                PromptStatChange(player, "attack");
            else if (relativeY > 44 && relativeY < 76)
                PromptStatChange(player, "strength");
            else if (relativeY > 76 && relativeY < 108)
                PromptStatChange(player, "defence");
            else if (relativeY > 108 && relativeY < 140)
                PromptStatChange(player, "range");
            else if (relativeY > 140 && relativeY < 172)
                PromptStatChange(player, "prayer");
            else if (relativeY > 172 && relativeY < 204)
                PromptStatChange(player, "magic");
        }
        // Right column clickable areas
        else if (relativeX > 74 && relativeX < 138)
        {
            if (relativeY > 12 && relativeY < 44)
                PromptStatChange(player, "hitpoint");
            else if (relativeY > 44 && relativeY < 76)
                PromptStatChange(player, "agility");
        }
    }

    /// <summary>
    /// Prompt user to change a stat level.
    /// SDK Reference: StatsControls.levelPrompt() in StatsControls.ts lines 30-40
    /// 
    /// NOTE: Unity's GUI doesn't have prompt dialogs, so we'll use Debug for now.
    /// TODO: Implement proper input dialog later.
    /// </summary>
    private void PromptStatChange(Player player, string statName)
    {
        Debug.Log($"[StatsPanel] Stat change requested for {statName}. Implement input dialog!");
        // TODO: Create custom input dialog popup
        // For now, stats can be changed via Inspector
    }

    private Player FindPlayer()
    {
        Player player = Object.FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("[StatsPanel] No player found!");
        }
        return player;
    }

}