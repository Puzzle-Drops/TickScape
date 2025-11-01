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
        // Ice Barrage position: (18, 222) - size (23, 25) - moved left 1px, up 1px
        // Blood Barrage position: (18, 187) - size (23, 25) - moved left 1px, up 1px

        // TODO: When we implement manual spell casting, draw selections here
        // For now, draw clickable areas for testing

        DrawSpellSlot(x, y, scale, 18, 222);
        DrawSpellSlot(x, y, scale, 18, 187);

        // TODO: Highlight selected spell
        // SDK Reference: AncientsSpellbookControls.draw() in AncientsSpellbookControls.ts lines 36-48
    }

    /// <summary>
    /// Draw spell slot hitbox (spells are on background panel).
    /// </summary>
    private void DrawSpellSlot(float panelX, float panelY, float scale, float x, float y)
    {
        Rect spellRect = new Rect(panelX + x * scale, panelY + y * scale, 23 * scale, 25 * scale);

        // Draw hover highlight
        if (UILayout.IsMouseOverRect(spellRect))
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(spellRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
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

        // Check Ice Barrage click (18, 222, 23x25)
        if (relativeX >= 18 && relativeX <= 41 && relativeY >= 222 && relativeY <= 247)
        {
            Debug.Log("[SpellbookPanel] Selected Ice Barrage");
            // TODO: player.manualSpellCastSelection = new IceBarrageSpell();
        }
        // Check Blood Barrage click (18, 187, 23x25)
        else if (relativeX >= 18 && relativeX <= 41 && relativeY >= 187 && relativeY <= 212)
        {
            Debug.Log("[SpellbookPanel] Selected Blood Barrage");
            // TODO: player.manualSpellCastSelection = new BloodBarrageSpell();
        }
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }
}