using UnityEngine;

/// <summary>
/// Empty placeholder panel for testing.
/// SDK Reference: EmptyControls.ts
/// </summary>
public class EmptyPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/empty_panel";
    protected override string TabTexturePath => "UI/Tabs/empty_tab";

    protected override Color PanelFallbackColor => new Color(0.2f, 0.2f, 0.25f);
    protected override Color TabFallbackColor => new Color(0.3f, 0.3f, 0.35f);

    public override bool IsAvailable => true;

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        // Draw placeholder text
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.RoundToInt(16 * scale);
        style.normal.textColor = Color.gray;
        style.alignment = TextAnchor.MiddleCenter;

        Rect textRect = new Rect(x, y + 100 * scale, 204 * scale, 50 * scale);
        GUI.Label(textRect, "Panel\nNot Implemented", style);
    }
}