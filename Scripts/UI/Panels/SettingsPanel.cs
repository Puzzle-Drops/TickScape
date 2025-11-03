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
    private Texture2D metronomeTexture;
    private Texture2D disabledOverlay;

    // Slider state
    private bool isDraggingPing = false;
    private bool isDraggingUIScale = false;

    // Temporary values while dragging (don't apply until mouse up)
    private float tempUIScale = 1.0f;
    private int tempPing = 20;

    // Key binding state
    private string bindingKey = null;

    // Text styles
    private GUIStyle labelStyle;
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

        metronomeTexture = TextureLoader.LoadTexture(
            "UI/Elements/metronome",
            new Color(0.6f, 0.5f, 0.4f),
            32, 32
        );

        disabledOverlay = TextureLoader.LoadTexture(
            "UI/Elements/disabled_option_overlay",
            new Color(0, 0, 0, 0.5f),
            32, 32
        );

        // Text styles
        labelStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        valueStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
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

        // Update font sizes and fonts
        labelStyle.fontSize = Mathf.RoundToInt(16 * scale);
        valueStyle.fontSize = Mathf.RoundToInt(16 * scale);
        labelStyle.font = UIFonts.VT323;
        valueStyle.font = UIFonts.VT323;

        // === TOP SECTION: Sound Toggles ===
        // Audio toggle button (20, 20)
        DrawToggleButton(x, y, scale, 20, 20, soundTexture,
            UISettings.Instance.playsAudio && UISettings.Instance.playsAreaAudio);

        // Metronome toggle button (60, 20) - to the right of audio
        DrawToggleButton(x, y, scale, 60, 20, metronomeTexture, UISettings.Instance.metronome);

        // === UI SCALE SLIDER ===
        DrawUIScaleSlider(x, y, scale);

        // === PING SLIDER ===
        DrawPingSlider(x, y, scale);

        // === KEY BINDINGS SECTION ===
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
    /// Draw UI Scale slider (50% - 300%).
    /// Position: Below sound toggles
    /// </summary>
    private void DrawUIScaleSlider(float panelX, float panelY, float scale)
    {
        float startY = 70; // Below sound toggles

        // Label above slider
        string label = "UI Scale";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + startY * scale, 100 * scale, 20),
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        float displayValue = isDraggingUIScale ? tempUIScale : UISettings.Instance.maxUiScale;

        // Current value on left (convert 0.5-3.0 to 50%-300%)
        int scalePercent = Mathf.RoundToInt(displayValue * 100);
        string valueText = $"{scalePercent}%";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + (startY + 20) * scale, 50 * scale, 20),
            valueText, valueStyle, scale);

        // Slider bar on right
        float sliderX = 75;
        float sliderY = startY + 20;
        float sliderWidth = 110; // ~80% of panel width minus value text space
        float sliderHeight = 10;

        DrawSlider(
            panelX, panelY, scale,
            sliderX, sliderY, sliderWidth, sliderHeight,
            displayValue,
            0.5f, 3.0f,
            isDraggingUIScale
        );

        // Draw tick marks (every 2.5% = every 0.025 in scale)
        DrawTickMarks(panelX, panelY, scale, sliderX, sliderY + sliderHeight + 2, sliderWidth, 0.5f, 3.0f, 0.025f);
    }

    /// <summary>
    /// Draw Ping slider (0 - 200ms).
    /// Position: Below UI Scale slider
    /// </summary>
    private void DrawPingSlider(float panelX, float panelY, float scale)
    {
        float startY = 125; // Below UI scale slider

        // Label above slider
        string label = "Ping";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + startY * scale, 100 * scale, 20),
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        int displayValue = isDraggingPing ? tempPing : UISettings.Instance.inputDelay;

        // Current value on left
        string valueText = $"{displayValue}ms";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + (startY + 20) * scale, 50 * scale, 20),
            valueText, valueStyle, scale);

        // Slider bar on right
        float sliderX = 75;
        float sliderY = startY + 20;
        float sliderWidth = 110;
        float sliderHeight = 10;

        DrawSlider(
            panelX, panelY, scale,
            sliderX, sliderY, sliderWidth, sliderHeight,
            displayValue,
            0, 200,
            isDraggingPing
        );

        // Draw tick marks (every 5ms)
        DrawTickMarks(panelX, panelY, scale, sliderX, sliderY + sliderHeight + 2, sliderWidth, 0, 200, 5);
    }

    /// <summary>
    /// Draw a horizontal slider with draggable handle.
    /// </summary>
    private void DrawSlider(float panelX, float panelY, float scale,
                            float x, float y, float width, float height,
                            float currentValue, float minValue, float maxValue,
                            bool isDragging)
    {
        Rect sliderRect = new Rect(panelX + x * scale, panelY + y * scale, width * scale, height * scale);

        // Draw slider background (dark bar)
        Color oldColor = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f);
        GUI.DrawTexture(sliderRect, Texture2D.whiteTexture);

        // Draw slider fill (lighter bar up to current value)
        float fillPercent = (currentValue - minValue) / (maxValue - minValue);
        Rect fillRect = new Rect(sliderRect.x, sliderRect.y, sliderRect.width * fillPercent, sliderRect.height);
        GUI.color = new Color(0.5f, 0.5f, 0.5f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw handle (small square at current position)
        float handleSize = 8 * scale;
        float handleX = sliderRect.x + (sliderRect.width * fillPercent) - handleSize / 2f;
        float handleY = sliderRect.y + sliderRect.height / 2f - handleSize / 2f;
        Rect handleRect = new Rect(handleX, handleY, handleSize, handleSize);

        GUI.color = new Color(0.8f, 0.8f, 0.8f);
        GUI.DrawTexture(handleRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw tick marks below slider.
    /// </summary>
    private void DrawTickMarks(float panelX, float panelY, float scale,
                               float x, float y, float width,
                               float minValue, float maxValue, float interval)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f);

        float range = maxValue - minValue;
        int tickCount = Mathf.RoundToInt(range / interval) + 1;

        for (int i = 0; i < tickCount; i++)
        {
            float percent = i / (float)(tickCount - 1);
            float tickX = panelX + (x + width * percent) * scale;
            float tickY = panelY + y * scale;

            Rect tickRect = new Rect(tickX - 0.5f * scale, tickY, 1 * scale, 3 * scale);
            GUI.DrawTexture(tickRect, Texture2D.whiteTexture);
        }

        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw key binding buttons at bottom of panel.
    /// SDK Reference: SettingsControls.draw() key bindings section
    /// </summary>
    private void DrawKeyBindingSection(float panelX, float panelY, float scale)
    {
        // Position 4px above where edit buttons currently sit (250 - 4 = 246)
        float startY = 246 - 30; // Subtract button height to position bottom edge

        // Title above buttons
        string title = bindingKey == null ? "Key Bindings" : "Press Key To Bind";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 10 * scale, panelY + (startY - 18) * scale, 100 * scale, 20),
            title, labelStyle, scale);

        // Draw key binding buttons with their current keys
        float buttonSize = 30;
        float gap = 8; // Gap between buttons
        float totalWidth = buttonSize * 5 + gap * 4; // 150 + 32 = 182px
        float startX = 10; // Fixed left margin to move buttons to the right

        // Combat key
        DrawKeyButton(panelX + startX * scale, panelY + startY * scale, buttonSize * scale, "combat",
                      UISettings.Instance.combatKey.ToString(), "Cmb", scale);

        // Inventory key
        DrawKeyButton(panelX + (startX + buttonSize + gap) * scale, panelY + startY * scale, buttonSize * scale, "inventory",
                      UISettings.Instance.inventoryKey.ToString(), "Inv", scale);

        // Equipment key
        DrawKeyButton(panelX + (startX + (buttonSize + gap) * 2) * scale, panelY + startY * scale, buttonSize * scale, "equipment",
                      UISettings.Instance.equipmentKey.ToString(), "Eqp", scale);

        // Prayer key
        DrawKeyButton(panelX + (startX + (buttonSize + gap) * 3) * scale, panelY + startY * scale, buttonSize * scale, "prayer",
                      UISettings.Instance.prayerKey.ToString(), "Pry", scale);

        // Spellbook key
        DrawKeyButton(panelX + (startX + (buttonSize + gap) * 4) * scale, panelY + startY * scale, buttonSize * scale, "spellbook",
                      UISettings.Instance.spellbookKey.ToString(), "Mag", scale);
    }

    private void DrawKeyButton(float x, float y, float size, string keyName, string currentKey, string label, float scale)
    {
        Rect buttonRect = new Rect(x, y, size, size);

        // Draw button background
        Color oldColor = GUI.color;
        if (bindingKey == keyName)
        {
            GUI.color = Color.yellow; // Highlight when binding
        }
        GUI.Box(buttonRect, "");
        GUI.color = oldColor;

        // Draw label
        GUIStyle keyStyle = new GUIStyle(labelStyle);
        keyStyle.font = UIFonts.VT323;
        keyStyle.fontSize = Mathf.RoundToInt(10 * scale);
        keyStyle.alignment = TextAnchor.UpperCenter;

        UIFonts.DrawShadowedText(new Rect(x, y + 2 * scale, size, size / 2), label, keyStyle, scale);
        UIFonts.DrawShadowedText(new Rect(x, y + size / 2, size, size / 2), currentKey, keyStyle, scale);
    }

    /// <summary>
    /// Handle settings clicks.
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        if (UISettings.Instance == null) return;

        // Sound toggle (20, 20, 32x32) - toggles BOTH audio settings
        if (relativeX > 20 && relativeX < 52 && relativeY > 20 && relativeY < 52)
        {
            bool newState = !(UISettings.Instance.playsAudio && UISettings.Instance.playsAreaAudio);
            UISettings.Instance.playsAudio = newState;
            UISettings.Instance.playsAreaAudio = newState;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Audio: {newState}");
            return;
        }
        // Metronome toggle (60, 20, 32x32)
        else if (relativeX > 60 && relativeX < 92 && relativeY > 20 && relativeY < 52)
        {
            UISettings.Instance.metronome = !UISettings.Instance.metronome;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Metronome: {UISettings.Instance.metronome}");
            return;
        }

        // Check UI Scale slider click
        if (relativeX > 75 && relativeX < 185 && relativeY > 90 && relativeY < 105)
        {
            isDraggingUIScale = true;
            tempUIScale = UISettings.Instance.maxUiScale; // Start with current value
            HandleSliderDrag(relativeX, 75, 110, 0.5f, 3.0f, true, out float newValue);
            tempUIScale = newValue;
            return;
        }

        // Check Ping slider click
        if (relativeX > 75 && relativeX < 185 && relativeY > 145 && relativeY < 160)
        {
            isDraggingPing = true;
            tempPing = UISettings.Instance.inputDelay; // Start with current value
            HandleSliderDrag(relativeX, 75, 110, 0, 200, false, out float newValue);
            tempPing = Mathf.RoundToInt(newValue);
            return;
        }

        // Check for key binding button clicks
        float buttonSize = 30;
        float gap = 8;
        float startX = 20;
        float startY = 246 - 30;

        if (relativeY >= startY && relativeY <= startY + buttonSize)
        {
            if (relativeX >= startX && relativeX <= startX + buttonSize)
            {
                StartKeyBinding("combat");
            }
            else if (relativeX >= startX + buttonSize + gap && relativeX <= startX + (buttonSize + gap) + buttonSize)
            {
                StartKeyBinding("inventory");
            }
            else if (relativeX >= startX + (buttonSize + gap) * 2 && relativeX <= startX + (buttonSize + gap) * 2 + buttonSize)
            {
                StartKeyBinding("equipment");
            }
            else if (relativeX >= startX + (buttonSize + gap) * 3 && relativeX <= startX + (buttonSize + gap) * 3 + buttonSize)
            {
                StartKeyBinding("prayer");
            }
            else if (relativeX >= startX + (buttonSize + gap) * 4 && relativeX <= startX + (buttonSize + gap) * 4 + buttonSize)
            {
                StartKeyBinding("spellbook");
            }
        }
    }

    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        // Handle slider dragging - update temp values only
        if (isDraggingUIScale)
        {
            HandleSliderDrag(relativeX, 75, 110, 0.5f, 3.0f, true, out float newValue);
            tempUIScale = newValue;
        }

        if (isDraggingPing)
        {
            HandleSliderDrag(relativeX, 75, 110, 0, 200, false, out float newValue);
            tempPing = Mathf.RoundToInt(newValue);
        }

        // Handle mouse wheel for sliders
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Check if mouse is over UI Scale slider area
            if (relativeX > 20 && relativeX < 185 && relativeY > 70 && relativeY < 105)
            {
                float change = scroll > 0 ? 0.1f : -0.1f; // +/- 10%
                UISettings.Instance.maxUiScale = Mathf.Clamp(UISettings.Instance.maxUiScale + change, 0.5f, 3.0f);
                UISettings.Instance.SaveSettings();
                Debug.Log($"[SettingsPanel] UI Scale: {UISettings.Instance.maxUiScale:F2}");
            }
            // Check if mouse is over Ping slider area
            else if (relativeX > 20 && relativeX < 185 && relativeY > 125 && relativeY < 160)
            {
                int change = scroll > 0 ? 10 : -10; // +/- 10ms
                UISettings.Instance.inputDelay = Mathf.Clamp(UISettings.Instance.inputDelay + change, 0, 200);
                UISettings.Instance.SaveSettings();
                Debug.Log($"[SettingsPanel] Ping: {UISettings.Instance.inputDelay}ms");
            }
        }
    }

    public override void OnMouseUp()
    {
        // Apply temp values and save on mouse release
        if (isDraggingUIScale)
        {
            UISettings.Instance.maxUiScale = tempUIScale;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] UI Scale set to: {tempUIScale:F2}");
        }

        if (isDraggingPing)
        {
            UISettings.Instance.inputDelay = tempPing;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Ping set to: {tempPing}ms");
        }

        isDraggingUIScale = false;
        isDraggingPing = false;
    }

    /// <summary>
    /// Handle slider drag input.
    /// </summary>
    private void HandleSliderDrag(float mouseX, float sliderX, float sliderWidth,
                                   float minValue, float maxValue, bool isUIScale, out float newValue)
    {
        float percent = Mathf.Clamp01((mouseX - sliderX) / sliderWidth);
        newValue = Mathf.Lerp(minValue, maxValue, percent);

        // Round to whole numbers or appropriate intervals
        if (isUIScale)
        {
            // Round to nearest 0.05 for UI scale (5%)
            newValue = Mathf.Round(newValue * 20) / 20f;
        }
        else
        {
            // Round to whole number for ping
            newValue = Mathf.Round(newValue);
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