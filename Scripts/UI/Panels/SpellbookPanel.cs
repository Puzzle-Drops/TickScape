using UnityEngine;

/// <summary>
/// Ancient spellbook panel with spell selection.
/// SDK Reference: AncientsSpellbookControls.ts
/// </summary>
public class SpellbookPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/spellbook_ancients";
    protected override string TabTexturePath => "UI/Tabs/spellbook_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.spellbookKey : KeyCode.F6;

    public override bool IsAvailable => true;

    // Spell selection (stored on player)
    // SDK Reference: Player.manualSpellCastSelection in Player.ts

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Draw spell selections
        // Ice Barrage position: (21, 229) - size (21, 21)
        // Blood Barrage position: (166, 194) - size (21, 21)

        // TODO: When we implement manual spell casting, draw selections here
        // For now, draw clickable areas for testing

        DrawSpellSlot(x, y, scale, 21, 229, "Ice\nBarrage");
        DrawSpellSlot(x, y, scale, 166, 194, "Blood\nBarrage");

        // TODO: Highlight selected spell
        // SDK Reference: AncientsSpellbookControls.draw() in AncientsSpellbookControls.ts lines 36-48
    }

    /// <summary>
    /// Draw spell slot with name (temporary - spells are on background).
    /// </summary>
    private void DrawSpellSlot(float panelX, float panelY, float scale, float x, float y, string name)
    {
        GUIStyle spellStyle = new GUIStyle();
        spellStyle.fontSize = Mathf.RoundToInt(8 * scale);
        spellStyle.normal.textColor = new Color(0.7f, 0.7f, 1f); // Light blue
        spellStyle.alignment = TextAnchor.MiddleCenter;

        Rect spellRect = new Rect(panelX + x * scale, panelY + y * scale, 21 * scale, 21 * scale);

        // Draw clickable area
        if (UILayout.IsMouseOverRect(spellRect))
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(spellRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        GUI.Label(spellRect, name, spellStyle);
    }

    /// <summary>
    /// Handle spell selection clicks.
    /// SDK Reference: AncientsSpellbookControls.panelClickDown() in AncientsSpellbookControls.ts lines 28-34
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        // Clear previous selection
        // TODO: player.manualSpellCastSelection = null;

        // Check Ice Barrage click (21, 229, 21x21)
        if (relativeX >= 21 && relativeX <= 42 && relativeY >= 229 && relativeY <= 249)
        {
            Debug.Log("[SpellbookPanel] Selected Ice Barrage");
            // TODO: player.manualSpellCastSelection = new IceBarrageSpell();
        }
        // Check Blood Barrage click (166, 194, 21x21)
        else if (relativeX >= 166 && relativeX <= 187 && relativeY >= 194 && relativeY <= 214)
        {
            Debug.Log("[SpellbookPanel] Selected Blood Barrage");
            // TODO: player.manualSpellCastSelection = new BloodBarrageSpell();
        }
    }

    private Player FindPlayer()
    {
        return Object.FindObjectOfType<Player>();
    }
}