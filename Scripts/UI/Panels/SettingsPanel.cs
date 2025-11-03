using UnityEngine;

/// <summary>
/// Settings panel with audio, UI scale, input delay controls.
/// SDK Reference: SettingsControls.ts
/// </summary>
public class SettingsPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/settings_panel";
    protected override string TabTexturePath => "UI/Tabs/settings_tab";

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Button textures
    private Texture2D soundTexture;
    private Texture2D areaSoundTexture;
    private Texture2D disabledOverlay;
    private Texture2D redUpTexture;
    private Texture2D greenDownTexture;

    // Key binding state
    private string bindingKey = null;

    // Text styles
    private GUIStyle labelStyle;
    private GUIStyle labelStyleShadow;
    private GUIStyle valueStyle;


    public override void Initialize()
    {
        base.Initialize();

        // Load textures
        soundTexture = TextureLoader.LoadTexture(
            "UI/Elements/sound_effect_volume",
            new Color(0.5f, 0.4f, 0.3f),
            32, 32
        );

        areaSoundTexture = TextureLoader.LoadTexture(
            "UI/Elements/area_sound_volume",
            new Color(0.5f, 0.4f, 0.3f),
            32, 32
        );

        disabledOverlay = TextureLoader.LoadTexture(
            "UI/Elements/disabled_option_overlay",
            new Color(0, 0, 0, 0.5f),
            32, 32
        );

        redUpTexture = TextureLoader.LoadTexture(
            "UI/Elements/button_red_up",
            new Color(0.8f, 0.2f, 0.2f),
            16, 16
        );

        greenDownTexture = TextureLoader.LoadTexture(
            "UI/Elements/button_green_down",
            new Color(0.2f, 0.8f, 0.2f),
            16, 16
        );

        // Text styles
        labelStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleCenter);
        labelStyleShadow = UIFonts.CreateShadowStyle(16, TextAnchor.MiddleCenter);

        valueStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleCenter);
    }

    public void HandleKeyBinding()
    {
        if (bindingKey != null && UISettings.Instance.isKeybinding)
        {
            // Check for any key press
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key) &&
                    key != KeyCode.Mouse0 &&
                    key != KeyCode.Mouse1 &&
                    key != KeyCode.Escape)
                {
                    switch (bindingKey)
                    {
                        case "combat":
                            UISettings.Instance.combatKey = key;
                            break;
                        case "inventory":
                            UISettings.Instance.inventoryKey = key;
                            break;
                        case "equipment":
                            UISettings.Instance.equipmentKey = key;
                            break;
                        case "prayer":
                            UISettings.Instance.prayerKey = key;
                            break;
                        case "spellbook":
                            UISettings.Instance.spellbookKey = key;
                            break;
                    }

                    UISettings.Instance.SaveSettings();
                    bindingKey = null;
                    UISettings.Instance.isKeybinding = false;
                    break;
                }
            }
        }
    }


    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        if (UISettings.Instance == null) return;

        // Update font sizes
        labelStyle.fontSize = Mathf.RoundToInt(16 * scale);
        labelStyleShadow.fontSize = labelStyle.fontSize;
        valueStyle.fontSize = labelStyle.fontSize;

        // Sound toggle button (20, 20)
        DrawToggleButton(x, y, scale, 20, 20, soundTexture, UISettings.Instance.playsAudio);

        // Area sound toggle (20, 60)
        DrawToggleButton(x, y, scale, 20, 60, areaSoundTexture, UISettings.Instance.playsAreaAudio);

        // Metronome toggle (140, 20)
        DrawTextToggle(x, y, scale, 140, 20, "Metronome", UISettings.Instance.metronome);

        // UI Scale controls (74, 20-67)
        DrawScaleControls(x, y, scale, 74, 20);

        // Input Delay controls (100, 20-67)
        DrawInputDelayControls(x, y, scale, 100, 20);

        // Key binding section (centered at y=163)
        DrawKeyBindingSection(x, y, scale);
    }

    /// <summary>
    /// Draw toggle button with icon.
    /// </summary>
    private void DrawToggleButton(float panelX, float panelY, float scale,
                                    float x, float y, Texture2D icon, bool isEnabled)
    {
        Rect buttonRect = new Rect(panelX + x * scale, panelY + y * scale, 32 * scale, 32 * scale);

        // Draw icon
        GUI.DrawTexture(buttonRect, icon);

        // Draw disabled overlay if off
        if (!isEnabled)
        {
            GUI.DrawTexture(buttonRect, disabledOverlay);
        }

        // Highlight on hover
        if (UILayout.IsMouseOverRect(buttonRect))
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Draw toggle with text label.
    /// </summary>
    private void DrawTextToggle(float panelX, float panelY, float scale,
                                  float x, float y, string label, bool isEnabled)
    {
        Rect buttonRect = new Rect(panelX + x * scale, panelY + y * scale, 50 * scale, 20 * scale);

        // Draw background
        Color oldColor = GUI.color;
        GUI.color = isEnabled ? new Color(0.2f, 0.8f, 0.2f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw label
        GUI.Label(buttonRect, label, labelStyle);
    }

    /// <summary>
    /// Draw UI scale up/down buttons with value.
    /// SDK Reference: SettingsControls.draw() UI scale section in SettingsControls.ts lines 112-128
    /// </summary>
    private void DrawScaleControls(float panelX, float panelY, float scale, float x, float baseY)
    {
        // Up button
        Rect upRect = new Rect(panelX + x * scale, panelY + baseY * scale, 15 * scale, 16 * scale);
        GUI.DrawTexture(upRect, redUpTexture);

        // Value display - centered between arrows
        int scalePercent = Mathf.RoundToInt((UISettings.Instance.maxUiScale - 1.0f) * 100);
        string scaleText = $"{scalePercent}%";
        Rect valueRect = new Rect(panelX + (x - 10) * scale, panelY + (baseY + 20) * scale, 35 * scale, 16 * scale);
        GUI.Label(valueRect, scaleText, valueStyle);

        // Down button
        Rect downRect = new Rect(panelX + x * scale, panelY + (baseY + 36) * scale, 15 * scale, 16 * scale);
        GUI.DrawTexture(downRect, greenDownTexture);

        // Label
        GUI.Label(new Rect(panelX + (x - 10) * scale, panelY + (baseY + 56) * scale, 35 * scale, 20),
                  "UI", labelStyle);
    }

    /// <summary>
    /// Draw input delay up/down buttons with value.
    /// SDK Reference: SettingsControls.draw() ping section in SettingsControls.ts lines 130-143
    /// </summary>
    private void DrawInputDelayControls(float panelX, float panelY, float scale, float x, float baseY)
    {
        // Up button
        Rect upRect = new Rect(panelX + x * scale, panelY + baseY * scale, 15 * scale, 16 * scale);
        GUI.DrawTexture(upRect, redUpTexture);

        // Value display - centered between arrows
        string delayText = UISettings.Instance.inputDelay.ToString();
        Rect valueRect = new Rect(panelX + (x - 10) * scale, panelY + (baseY + 20) * scale, 35 * scale, 16 * scale);
        GUI.Label(valueRect, delayText, valueStyle);

        // Down button
        Rect downRect = new Rect(panelX + x * scale, panelY + (baseY + 36) * scale, 15 * scale, 16 * scale);
        GUI.DrawTexture(downRect, greenDownTexture);

        // Label
        GUI.Label(new Rect(panelX + (x - 10) * scale, panelY + (baseY + 56) * scale, 35 * scale, 20),
                  "Ping", labelStyle);
    }

    /// <summary>
    /// Draw key binding buttons.
    /// SDK Reference: SettingsControls.draw() key bindings section in SettingsControls.ts lines 145-165
    /// </summary>
    private void DrawKeyBindingSection(float panelX, float panelY, float scale)
    {
        // Title
        string title = bindingKey == null ? "Key Bindings" : "Press Key To Bind";
        GUI.Label(new Rect(panelX + 100 * scale, panelY + 163 * scale, 100, 20), title, labelStyle);

        // Draw key binding buttons with their current keys
        float buttonSize = 30 * scale;
        float spacing = 35 * scale;
        float startX = 30 * scale;
        float startY = 180 * scale;

        // Combat key
        DrawKeyButton(panelX + startX, panelY + startY, buttonSize, "combat",
                      UISettings.Instance.combatKey.ToString(), "Cmb");

        // Inventory key
        DrawKeyButton(panelX + startX + spacing, panelY + startY, buttonSize, "inventory",
                      UISettings.Instance.inventoryKey.ToString(), "Inv");

        // Equipment key
        DrawKeyButton(panelX + startX + spacing * 2, panelY + startY, buttonSize, "equipment",
                      UISettings.Instance.equipmentKey.ToString(), "Eqp");

        // Prayer key
        DrawKeyButton(panelX + startX + spacing * 3, panelY + startY, buttonSize, "prayer",
                      UISettings.Instance.prayerKey.ToString(), "Pry");

        // Spellbook key
        DrawKeyButton(panelX + startX + spacing * 4, panelY + startY, buttonSize, "spellbook",
                      UISettings.Instance.spellbookKey.ToString(), "Mag");
    }

    private void DrawKeyButton(float x, float y, float size, string keyName, string currentKey, string label)
    {
        // Draw button background
        Color oldColor = GUI.color;
        if (bindingKey == keyName)
        {
            GUI.color = Color.yellow; // Highlight when binding
        }
        GUI.Box(new Rect(x, y, size, size), "");
        GUI.color = oldColor;

        // Draw label
        GUIStyle keyStyle = new GUIStyle(labelStyle);
        keyStyle.font = UIFonts.VT323;
        keyStyle.fontSize = Mathf.RoundToInt(10 * UIScale.GetPanelScale());
        keyStyle.alignment = TextAnchor.UpperCenter;

        GUI.Label(new Rect(x, y + 2, size, size / 2), label, keyStyle);
        GUI.Label(new Rect(x, y + size / 2, size, size / 2), currentKey, keyStyle);
    }

    /// <summary>
    /// Handle settings clicks.
    /// SDK Reference: SettingsControls.panelClickDown() in SettingsControls.ts lines 84-110
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        if (UISettings.Instance == null) return;

        // Sound toggle (20, 20, 32x32)
        if (relativeX > 20 && relativeX < 52 && relativeY > 20 && relativeY < 52)
        {
            UISettings.Instance.playsAudio = !UISettings.Instance.playsAudio;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Audio: {UISettings.Instance.playsAudio}");
        }
        // Area sound toggle (20, 60, 32x32)
        else if (relativeX > 20 && relativeX < 52 && relativeY > 60 && relativeY < 92)
        {
            UISettings.Instance.playsAreaAudio = !UISettings.Instance.playsAreaAudio;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Area Audio: {UISettings.Instance.playsAreaAudio}");
        }
        // Metronome toggle (140, 20, 50x20)
        else if (relativeX > 140 && relativeX < 190 && relativeY > 20 && relativeY < 40)
        {
            UISettings.Instance.metronome = !UISettings.Instance.metronome;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Metronome: {UISettings.Instance.metronome}");
        }
        // UI Scale up button (74, 20, 15x16)
        else if (relativeX > 74 && relativeX < 89 && relativeY > 20 && relativeY < 36)
        {
            UISettings.Instance.maxUiScale += 0.05f;
            UISettings.Instance.maxUiScale = Mathf.Min(2.0f, UISettings.Instance.maxUiScale);
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] UI Scale: {UISettings.Instance.maxUiScale:F2}");
        }
        // UI Scale down button (74, 51, 15x16)
        else if (relativeX > 74 && relativeX < 89 && relativeY > 51 && relativeY < 67)
        {
            UISettings.Instance.maxUiScale -= 0.05f;
            UISettings.Instance.maxUiScale = Mathf.Max(0.5f, UISettings.Instance.maxUiScale);
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] UI Scale: {UISettings.Instance.maxUiScale:F2}");
        }
        // Input Delay up button (100, 20, 15x16)
        else if (relativeX > 100 && relativeX < 115 && relativeY > 20 && relativeY < 36)
        {
            UISettings.Instance.inputDelay += 20;
            UISettings.Instance.inputDelay = Mathf.Min(200, UISettings.Instance.inputDelay);
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Input Delay: {UISettings.Instance.inputDelay}ms");
        }
        // Input Delay down button (100, 51, 15x16)
        else if (relativeX > 100 && relativeX < 115 && relativeY > 51 && relativeY < 67)
        {
            UISettings.Instance.inputDelay -= 20;
            UISettings.Instance.inputDelay = Mathf.Max(0, UISettings.Instance.inputDelay);
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Input Delay: {UISettings.Instance.inputDelay}ms");
        }

        // Check for key binding button clicks
        float buttonSize = 30;
        float spacing = 35;
        float startX = 30;
        float startY = 180;

        // Check each key binding button
        if (relativeY >= startY && relativeY <= startY + buttonSize)
        {
            if (relativeX >= startX && relativeX <= startX + buttonSize)
            {
                StartKeyBinding("combat");
            }
            else if (relativeX >= startX + spacing && relativeX <= startX + spacing + buttonSize)
            {
                StartKeyBinding("inventory");
            }
            else if (relativeX >= startX + spacing * 2 && relativeX <= startX + spacing * 2 + buttonSize)
            {
                StartKeyBinding("equipment");
            }
            else if (relativeX >= startX + spacing * 3 && relativeX <= startX + spacing * 3 + buttonSize)
            {
                StartKeyBinding("prayer");
            }
            else if (relativeX >= startX + spacing * 4 && relativeX <= startX + spacing * 4 + buttonSize)
            {
                StartKeyBinding("spellbook");
            }
        }
    }

    private void StartKeyBinding(string key)
    {
        UISettings.Instance.isKeybinding = true;
        bindingKey = key;
        Debug.Log($"[SettingsPanel] Started binding key for: {key}");
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }
}