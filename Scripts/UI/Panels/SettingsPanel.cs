using UnityEngine;

/// <summary>
/// Settings panel with volume, UI scale, input delay controls, and checkboxes.
/// SDK Reference: SettingsControls.ts
/// </summary>
public class SettingsPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/settings_panel";
    protected override string TabTexturePath => "UI/Tabs/settings_tab";

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Slider state
    private bool isDraggingVolume = false;
    private bool isDraggingPing = false;
    private bool isDraggingUIScale = false;

    // Temporary values while dragging (don't apply until mouse up)
    private float tempVolume = 1.0f;
    private float tempUIScale = 1.0f;
    private int tempPing = 20;

    // Key binding state
    private string bindingKey = null;

    // Text styles
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle checkboxLabelStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Text styles
        labelStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        valueStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        checkboxLabelStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleLeft);
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
        checkboxLabelStyle.fontSize = Mathf.RoundToInt(14 * scale);
        labelStyle.font = UIFonts.VT323;
        valueStyle.font = UIFonts.VT323;
        checkboxLabelStyle.font = UIFonts.VT323;

        // === TOP SECTION: Checkboxes ===
        // Metronome checkbox (20, 20)
        DrawCheckbox(x, y, scale, 20, 20, "Metronome", UISettings.Instance.metronome);

        // WASD Camera checkbox (20, 40)
        DrawCheckbox(x, y, scale, 20, 40, "WASD Camera", UISettings.Instance.wasdCamera);

        // === VOLUME SLIDER ===
        DrawVolumeSlider(x, y, scale);

        // === UI SCALE SLIDER ===
        DrawUIScaleSlider(x, y, scale);

        // === PING SLIDER ===
        DrawPingSlider(x, y, scale);

        // === KEY BINDINGS SECTION ===
        DrawKeyBindingSection(x, y, scale);
    }

    /// <summary>
    /// Draw a checkbox with label.
    /// </summary>
    private void DrawCheckbox(float panelX, float panelY, float scale, float x, float y, string label, bool isChecked)
    {
        float checkboxSize = 12 * scale;
        Rect checkboxRect = new Rect(panelX + x * scale, panelY + y * scale, checkboxSize, checkboxSize);

        // Draw checkbox outline
        Color oldColor = GUI.color;
        GUI.color = UIFonts.YellowText;
        GUI.Box(checkboxRect, "");

        // Draw checkmark if checked (filled square)
        if (isChecked)
        {
            Rect fillRect = new Rect(
                checkboxRect.x + 2 * scale,
                checkboxRect.y + 2 * scale,
                checkboxRect.width - 4 * scale,
                checkboxRect.height - 4 * scale
            );
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        }

        GUI.color = oldColor;

        // Draw label to the right of checkbox
        Rect labelRect = new Rect(
            panelX + (x + 16) * scale,
            panelY + (y - 2) * scale,
            150 * scale,
            20
        );
        GUI.Label(labelRect, label, checkboxLabelStyle);

        // Hover highlight
        Rect hoverRect = new Rect(panelX + x * scale, panelY + y * scale, 120 * scale, 16 * scale);
        if (UILayout.IsMouseOverRect(hoverRect))
        {
            oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.DrawTexture(hoverRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Draw Volume slider (0% - 100%).
    /// Position: Top of panel
    /// </summary>
    private void DrawVolumeSlider(float panelX, float panelY, float scale)
    {
        float startY = 70; // Below checkboxes

        // Label above slider
        string label = "Volume";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + startY * scale, 100 * scale, 20),
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        float displayValue = isDraggingVolume ? tempVolume : UISettings.Instance.volume;

        // Current value on left (convert 0.0-1.0 to 0%-100%)
        int volumePercent = Mathf.RoundToInt(displayValue * 100);
        string valueText = $"{volumePercent}%";
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
            0.0f, 1.0f,
            isDraggingVolume
        );

        // Draw tick marks (every 10% = every 0.1)
        DrawTickMarks(panelX, panelY, scale, sliderX, sliderY + sliderHeight + 2, sliderWidth, 0.0f, 1.0f, 0.1f);
    }

    /// <summary>
    /// Draw UI Scale slider (50% - 300%).
    /// Position: Below volume slider
    /// </summary>
    private void DrawUIScaleSlider(float panelX, float panelY, float scale)
    {
        float startY = 125; // Below volume slider

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
        float sliderWidth = 110;
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
        float startY = 180; // Below UI scale slider

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
        float startY = 234; // Adjusted position

        // Title above buttons
        string title = bindingKey == null ? "Key Bindings" : "Press Key To Bind";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 10 * scale, panelY + (startY - 18) * scale, 100 * scale, 20),
            title, labelStyle, scale);

        // Draw key binding buttons with their current keys
        float buttonSize = 30;
        float gap = 8;
        float startX = 10;

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

        // Metronome checkbox (20, 20, ~120x16 clickable area)
        if (relativeX > 20 && relativeX < 140 && relativeY > 20 && relativeY < 36)
        {
            UISettings.Instance.metronome = !UISettings.Instance.metronome;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Metronome: {UISettings.Instance.metronome}");
            return;
        }

        // WASD Camera checkbox (20, 40, ~120x16 clickable area)
        if (relativeX > 20 && relativeX < 140 && relativeY > 40 && relativeY < 56)
        {
            UISettings.Instance.wasdCamera = !UISettings.Instance.wasdCamera;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] WASD Camera: {UISettings.Instance.wasdCamera}");
            return;
        }

        // Check Volume slider click
        if (relativeX > 75 && relativeX < 185 && relativeY > 90 && relativeY < 105)
        {
            isDraggingVolume = true;
            tempVolume = UISettings.Instance.volume;
            HandleSliderDrag(relativeX, 75, 110, 0.0f, 1.0f, true, out float newValue);
            tempVolume = newValue;
            return;
        }

        // Check UI Scale slider click
        if (relativeX > 75 && relativeX < 185 && relativeY > 145 && relativeY < 160)
        {
            isDraggingUIScale = true;
            tempUIScale = UISettings.Instance.maxUiScale;
            HandleSliderDrag(relativeX, 75, 110, 0.5f, 3.0f, true, out float newValue);
            tempUIScale = newValue;
            return;
        }

        // Check Ping slider click
        if (relativeX > 75 && relativeX < 185 && relativeY > 200 && relativeY < 215)
        {
            isDraggingPing = true;
            tempPing = UISettings.Instance.inputDelay;
            HandleSliderDrag(relativeX, 75, 110, 0, 200, false, out float newValue);
            tempPing = Mathf.RoundToInt(newValue);
            return;
        }

        // Check for key binding button clicks
        float buttonSize = 30;
        float gap = 8;
        float startX = 10;
        float startY = 234;

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
        if (isDraggingVolume)
        {
            HandleSliderDrag(relativeX, 75, 110, 0.0f, 1.0f, true, out float newValue);
            tempVolume = newValue;
        }

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
            // Check if mouse is over Volume slider area
            if (relativeX > 20 && relativeX < 185 && relativeY > 70 && relativeY < 105)
            {
                float change = scroll > 0 ? 0.05f : -0.05f; // +/- 5%
                UISettings.Instance.volume = Mathf.Clamp(UISettings.Instance.volume + change, 0.0f, 1.0f);
                UISettings.Instance.SaveSettings();
                Debug.Log($"[SettingsPanel] Volume: {UISettings.Instance.volume:F2}");
            }
            // Check if mouse is over UI Scale slider area
            else if (relativeX > 20 && relativeX < 185 && relativeY > 125 && relativeY < 160)
            {
                float change = scroll > 0 ? 0.1f : -0.1f; // +/- 10%
                UISettings.Instance.maxUiScale = Mathf.Clamp(UISettings.Instance.maxUiScale + change, 0.5f, 3.0f);
                UISettings.Instance.SaveSettings();
                Debug.Log($"[SettingsPanel] UI Scale: {UISettings.Instance.maxUiScale:F2}");
            }
            // Check if mouse is over Ping slider area
            else if (relativeX > 20 && relativeX < 185 && relativeY > 180 && relativeY < 215)
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
        if (isDraggingVolume)
        {
            UISettings.Instance.volume = tempVolume;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Volume set to: {tempVolume:F2}");
        }

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

        isDraggingVolume = false;
        isDraggingUIScale = false;
        isDraggingPing = false;
    }

    /// <summary>
    /// Handle slider drag input.
    /// </summary>
    private void HandleSliderDrag(float mouseX, float sliderX, float sliderWidth,
                                   float minValue, float maxValue, bool useDecimals, out float newValue)
    {
        float percent = Mathf.Clamp01((mouseX - sliderX) / sliderWidth);
        newValue = Mathf.Lerp(minValue, maxValue, percent);

        // Round appropriately
        if (useDecimals)
        {
            if (maxValue <= 1.0f) // Volume slider (0.0-1.0)
            {
                newValue = Mathf.Round(newValue * 20) / 20f; // Round to 0.05
            }
            else // UI Scale slider (0.5-3.0)
            {
                newValue = Mathf.Round(newValue * 20) / 20f; // Round to 0.05
            }
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
