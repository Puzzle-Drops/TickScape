using UnityEngine;

/// <summary>
/// Settings panel with volume, UI scale, input delay controls, checkboxes, and color pickers.
/// SDK Reference: SettingsControls.ts
/// </summary>
public class SettingsPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/settings_panel";
    protected override string TabTexturePath => "UI/Tabs/settings_tab";

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Settings mode state machine
    private enum SettingsMode
    {
        Normal,
        EditingHoverColor,
        EditingPlayerColor,
        EditingDestinationColor,
        EditingNPCColor
    }

    private SettingsMode currentMode = SettingsMode.Normal;

    // Slider state
    private bool isDraggingVolume = false;
    private bool isDraggingPing = false;
    private bool isDraggingUIScale = false;

    // Temporary values while dragging (don't apply until mouse up)
    private float tempVolume = 1.0f;
    private float tempUIScale = 1.0f;
    private int tempPing = 20;

    // Color picker state
    private Color tempColor = Color.white;
    private Color originalColor = Color.white; // For cancel functionality
    private bool isDraggingR = false;
    private bool isDraggingG = false;
    private bool isDraggingB = false;
    private bool isDraggingA = false;
    private bool isEditingHex = false;
    private string hexInputBuffer = "";

    // Key binding state
    private string bindingKey = null;

    // Text styles
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle checkboxLabelStyle;
    private GUIStyle headerStyle;
    private GUIStyle hexInputStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Text styles
        labelStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        valueStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        checkboxLabelStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleLeft);
        headerStyle = UIFonts.CreateTextStyle(18, UIFonts.YellowText, TextAnchor.MiddleCenter);
        hexInputStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleCenter);
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

        // Update font sizes and ensure VT323 is set
        labelStyle.fontSize = Mathf.RoundToInt(16 * scale);
        labelStyle.font = UIFonts.VT323;
        
        valueStyle.fontSize = Mathf.RoundToInt(16 * scale);
        valueStyle.font = UIFonts.VT323;
        
        checkboxLabelStyle.fontSize = Mathf.RoundToInt(14 * scale);
        checkboxLabelStyle.font = UIFonts.VT323;
        
        headerStyle.fontSize = Mathf.RoundToInt(18 * scale);
        headerStyle.font = UIFonts.VT323;
        
        hexInputStyle.fontSize = Mathf.RoundToInt(14 * scale);
        hexInputStyle.font = UIFonts.VT323;

        // Draw different content based on mode
        switch (currentMode)
        {
            case SettingsMode.Normal:
                DrawNormalSettings(x, y, scale);
                break;
            case SettingsMode.EditingHoverColor:
            case SettingsMode.EditingPlayerColor:
            case SettingsMode.EditingDestinationColor:
            case SettingsMode.EditingNPCColor:
                DrawColorPicker(x, y, scale);
                break;
        }
    }

    /// <summary>
    /// Draw normal settings panel with checkboxes, sliders, key bindings, and color buttons.
    /// </summary>
    private void DrawNormalSettings(float x, float y, float scale)
    {
        // === TOP SECTION: Checkboxes and Color Buttons (2 columns) ===
        
        // LEFT COLUMN: Checkboxes (moved 4px left and up: 20→16, 20→16, 40→36)
        DrawCheckbox(x, y, scale, 16, 16, "Metronome", UISettings.Instance.metronome);
        DrawCheckbox(x, y, scale, 16, 36, "WASD Camera", UISettings.Instance.wasdCamera);

        // RIGHT COLUMN: Color Buttons (moved 4px left and up: 120→116, 20→16, 40→36, 60→56, 80→76)
        DrawColorButton(x, y, scale, 116, 16, "Hover", UISettings.Instance.hoverColor);
        DrawColorButton(x, y, scale, 116, 36, "Destination", UISettings.Instance.destinationColor);
        DrawColorButton(x, y, scale, 116, 56, "Player", UISettings.Instance.playerTileColor);
        DrawColorButton(x, y, scale, 116, 76, "NPC", UISettings.Instance.npcTileColor);

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
    /// Draw a checkbox with label (no hover effect).
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

        // Draw label to the right of checkbox WITH SHADOW
        Rect labelRect = new Rect(
            panelX + (x + 16) * scale,
            panelY + (y - 2) * scale,
            80 * scale,
            20
        );
        UIFonts.DrawShadowedText(labelRect, label, checkboxLabelStyle, scale);
    }

    /// <summary>
    /// Draw a color selection button with preview square.
    /// </summary>
    private void DrawColorButton(float panelX, float panelY, float scale, float x, float y, string label, Color color)
    {
        float boxSize = 12 * scale;
        Rect boxRect = new Rect(panelX + x * scale, panelY + y * scale, boxSize, boxSize);

        // Draw color preview box
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(boxRect, Texture2D.whiteTexture);
        
        // Draw border
        GUI.color = UIFonts.YellowText;
        GUI.Box(boxRect, "");
        GUI.color = oldColor;

        // Draw label to the right
        Rect labelRect = new Rect(
            panelX + (x + 16) * scale,
            panelY + (y - 2) * scale,
            70 * scale,
            20
        );
        UIFonts.DrawShadowedText(labelRect, label, checkboxLabelStyle, scale);
    }

    /// <summary>
    /// Draw Volume slider (0% - 100%).
    /// </summary>
    private void DrawVolumeSlider(float panelX, float panelY, float scale)
    {
        float startY = 106; // Moved up 4px from 110

        // Label above slider
        string label = "Volume";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + startY * scale, 100 * scale, 20), // Moved left 4px from 20
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        float displayValue = isDraggingVolume ? tempVolume : UISettings.Instance.volume;

        // Current value on left (convert 0.0-1.0 to 0%-100%)
        int volumePercent = Mathf.RoundToInt(displayValue * 100);
        string valueText = $"{volumePercent}%";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + (startY + 20) * scale, 50 * scale, 20), // Moved left 4px from 20
            valueText, valueStyle, scale);

        // Slider bar on right
        float sliderX = 71; // Moved left 4px from 75
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
    /// </summary>
    private void DrawUIScaleSlider(float panelX, float panelY, float scale)
    {
        float startY = 151; // Moved up 4px from 155

        // Label above slider
        string label = "UI Scale";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + startY * scale, 100 * scale, 20), // Moved left 4px from 20
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        float displayValue = isDraggingUIScale ? tempUIScale : UISettings.Instance.maxUiScale;

        // Current value on left (convert 0.5-3.0 to 50%-300%)
        int scalePercent = Mathf.RoundToInt(displayValue * 100);
        string valueText = $"{scalePercent}%";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + (startY + 20) * scale, 50 * scale, 20), // Moved left 4px from 20
            valueText, valueStyle, scale);

        // Slider bar on right
        float sliderX = 71; // Moved left 4px from 75
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
    /// </summary>
    private void DrawPingSlider(float panelX, float panelY, float scale)
    {
        float startY = 196; // Moved up 4px from 200

        // Label above slider
        string label = "Ping";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + startY * scale, 100 * scale, 20), // Moved left 4px from 20
            label, labelStyle, scale);

        // Use temp value if dragging, otherwise use actual setting
        int displayValue = isDraggingPing ? tempPing : UISettings.Instance.inputDelay;

        // Current value on left
        string valueText = $"{displayValue}ms";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 16 * scale, panelY + (startY + 20) * scale, 50 * scale, 20), // Moved left 4px from 20
            valueText, valueStyle, scale);

        // Slider bar on right
        float sliderX = 71; // Moved left 4px from 75
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
    /// </summary>
    private void DrawKeyBindingSection(float panelX, float panelY, float scale)
    {
        float startY = 236; // Moved up 4px from 240
        float buttonSize = 30;
        float gap = 8;
        float startX = 6; // Moved left 4px from 10

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
        
        // Show "..." if binding this key, otherwise show current key
        string displayKey = (bindingKey == keyName) ? "..." : currentKey;
        UIFonts.DrawShadowedText(new Rect(x, y + size / 2, size, size / 2), displayKey, keyStyle, scale);
    }

    /// <summary>
    /// Draw color picker panel with RGBA sliders and hex input.
    /// </summary>
    private void DrawColorPicker(float panelX, float panelY, float scale)
    {
        // Header
        string headerText = "Editing: " + GetCurrentColorName();
        UIFonts.DrawShadowedText(
            new Rect(panelX, panelY + 10 * scale, 204 * scale, 20),
            headerText,
            headerStyle,
            scale
        );

        // Color preview box (large)
        float previewSize = 60 * scale;
        float previewX = panelX + (204 * scale - previewSize) / 2f;
        float previewY = panelY + 35 * scale;
        Rect previewRect = new Rect(previewX, previewY, previewSize, previewSize);

        Color oldColor = GUI.color;
        GUI.color = tempColor;
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = UIFonts.YellowText;
        GUI.Box(previewRect, "");
        GUI.color = oldColor;

        // RGBA sliders
        float sliderStartY = 105;
        float sliderSpacing = 25;

        DrawColorSlider(panelX, panelY, scale, sliderStartY + sliderSpacing * 0, "R", tempColor.r, 0f, 1f, isDraggingR);
        DrawColorSlider(panelX, panelY, scale, sliderStartY + sliderSpacing * 1, "G", tempColor.g, 0f, 1f, isDraggingG);
        DrawColorSlider(panelX, panelY, scale, sliderStartY + sliderSpacing * 2, "B", tempColor.b, 0f, 1f, isDraggingB);
        DrawColorSlider(panelX, panelY, scale, sliderStartY + sliderSpacing * 3, "A", tempColor.a, 0f, 1f, isDraggingA);

        // Hex input box
        DrawHexInput(panelX, panelY, scale);

        // Buttons at bottom
        DrawPickerButtons(panelX, panelY, scale);
    }

    /// <summary>
    /// Draw a single RGBA slider.
    /// </summary>
    private void DrawColorSlider(float panelX, float panelY, float scale, float y, string label, float value, float min, float max, bool isDragging)
    {
        // Label
        GUIStyle sliderLabelStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), UIFonts.YellowText, TextAnchor.MiddleLeft);
        sliderLabelStyle.font = UIFonts.VT323;
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + y * scale, 20 * scale, 20),
            label + ":",
            sliderLabelStyle,
            scale
        );

        // Value (0-255)
        int intValue = Mathf.RoundToInt(value * 255);
        GUIStyle valueStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), UIFonts.YellowText, TextAnchor.MiddleRight);
        valueStyle.font = UIFonts.VT323;
        UIFonts.DrawShadowedText(
            new Rect(panelX + 160 * scale, panelY + y * scale, 30 * scale, 20),
            intValue.ToString(),
            valueStyle,
            scale
        );

        // Slider
        float sliderX = 40;
        float sliderWidth = 115;
        float sliderHeight = 10;

        DrawSlider(panelX, panelY, scale, sliderX, y, sliderWidth, sliderHeight, value, min, max, isDragging);
    }

    /// <summary>
    /// Draw hex color input field with text input capability.
    /// </summary>
    private void DrawHexInput(float panelX, float panelY, float scale)
    {
        float y = 215;

        // Label
        GUIStyle hexLabelStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(14 * scale), UIFonts.YellowText, TextAnchor.MiddleLeft);
        hexLabelStyle.font = UIFonts.VT323;
        UIFonts.DrawShadowedText(
            new Rect(panelX + 20 * scale, panelY + y * scale, 40 * scale, 20),
            "Hex:",
            hexLabelStyle,
            scale
        );

        // Input box background
        Rect inputRect = new Rect(panelX + 60 * scale, panelY + y * scale, 80 * scale, 18 * scale);
        Color oldColor = GUI.color;
        
        // Highlight if editing
        if (isEditingHex)
        {
            GUI.color = new Color(0.3f, 0.3f, 0.3f);
        }
        else
        {
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
        }
        GUI.DrawTexture(inputRect, Texture2D.whiteTexture);
        GUI.color = UIFonts.YellowText;
        GUI.Box(inputRect, "");
        GUI.color = oldColor;

        // Display text (buffer if editing, current hex otherwise)
        string displayText = isEditingHex ? hexInputBuffer : ColorToHex(tempColor).TrimStart('#');
        
        // Add cursor if editing
        if (isEditingHex && Time.frameCount % 60 < 30)
        {
            displayText += "_";
        }
        
        hexInputStyle.font = UIFonts.VT323;
        hexInputStyle.alignment = TextAnchor.MiddleCenter;
        hexInputStyle.normal.textColor = isEditingHex ? Color.white : UIFonts.YellowText;
        UIFonts.DrawShadowedText(inputRect, displayText, hexInputStyle, scale);

        // Info text
        GUIStyle infoStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(10 * scale), new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
        infoStyle.font = UIFonts.VT323;
        string infoText = isEditingHex ? "Type 6-digit hex (RRGGBB)" : "Click to edit hex code";
        UIFonts.DrawShadowedText(
            new Rect(panelX + 10 * scale, panelY + (y + 20) * scale, 184 * scale, 15),
            infoText,
            infoStyle,
            scale
        );
        
        // Handle keyboard input if editing
        if (isEditingHex && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Escape)
            {
                // Cancel editing
                isEditingHex = false;
                hexInputBuffer = "";
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
            {
                // Apply if we have 6 characters
                if (hexInputBuffer.Length == 6)
                {
                    TryApplyHexInput(hexInputBuffer);
                    isEditingHex = false;
                    hexInputBuffer = "";
                }
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Backspace)
            {
                // Remove last character
                if (hexInputBuffer.Length > 0)
                {
                    hexInputBuffer = hexInputBuffer.Substring(0, hexInputBuffer.Length - 1);
                }
                Event.current.Use();
            }
            else if (Event.current.character != 0)
            {
                // Add character if valid hex and not at max length
                char c = Event.current.character;
                if (hexInputBuffer.Length < 6 && 
                    ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    hexInputBuffer += char.ToUpper(c);
                    
                    // Auto-apply when we reach 6 characters
                    if (hexInputBuffer.Length == 6)
                    {
                        TryApplyHexInput(hexInputBuffer);
                    }
                }
                Event.current.Use();
            }
        }
    }
    
    /// <summary>
    /// Try to apply hex input to tempColor.
    /// </summary>
    private void TryApplyHexInput(string hex)
    {
        Color? newColor = HexToColor(hex);
        if (newColor.HasValue)
        {
            tempColor = newColor.Value;
            Debug.Log($"[SettingsPanel] Applied hex color: #{hex}");
        }
        else
        {
            Debug.LogWarning($"[SettingsPanel] Invalid hex color: {hex}");
        }
    }

    /// <summary>
    /// Draw Cancel, Reset, Done buttons.
    /// </summary>
    private void DrawPickerButtons(float panelX, float panelY, float scale)
    {
        float buttonY = 246;
        float buttonWidth = 55;
        float buttonHeight = 20;
        float gap = 8;

        // Calculate starting X to center 3 buttons
        float totalWidth = buttonWidth * 3 + gap * 2;
        float startX = (204 - totalWidth) / 2f;

        // Cancel button
        DrawPickerButton(panelX, panelY, scale, startX, buttonY, buttonWidth, buttonHeight, "Cancel", new Color(0.7f, 0.3f, 0.3f));

        // Reset button
        DrawPickerButton(panelX, panelY, scale, startX + buttonWidth + gap, buttonY, buttonWidth, buttonHeight, "Reset", new Color(0.5f, 0.5f, 0.5f));

        // Done button
        DrawPickerButton(panelX, panelY, scale, startX + (buttonWidth + gap) * 2, buttonY, buttonWidth, buttonHeight, "Done", new Color(0.3f, 0.7f, 0.3f));
    }

    /// <summary>
    /// Draw a single picker button.
    /// </summary>
    private void DrawPickerButton(float panelX, float panelY, float scale, float x, float y, float width, float height, string text, Color bgColor)
    {
        Rect buttonRect = new Rect(panelX + x * scale, panelY + y * scale, width * scale, height * scale);

        // Background
        Color oldColor = GUI.color;
        GUI.color = bgColor;
        GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);

        // Hover highlight
        if (UILayout.IsMouseOverRect(buttonRect))
        {
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
        }

        // Border
        GUI.color = UIFonts.YellowText;
        GUI.Box(buttonRect, "");
        GUI.color = oldColor;

        // Text
        GUIStyle buttonTextStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(12 * scale), UIFonts.YellowText, TextAnchor.MiddleCenter);
        buttonTextStyle.font = UIFonts.VT323;
        UIFonts.DrawShadowedText(buttonRect, text, buttonTextStyle, scale);
    }

    /// <summary>
    /// Get name of currently editing color.
    /// </summary>
    private string GetCurrentColorName()
    {
        switch (currentMode)
        {
            case SettingsMode.EditingHoverColor: return "Hover Color";
            case SettingsMode.EditingPlayerColor: return "Player Color";
            case SettingsMode.EditingDestinationColor: return "Destination Color";
            case SettingsMode.EditingNPCColor: return "NPC Color";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Convert Color to hex string (RRGGBB format).
    /// </summary>
    private string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Convert hex string to Color.
    /// Returns null if invalid.
    /// </summary>
    private Color? HexToColor(string hex)
    {
        // Remove # if present
        hex = hex.TrimStart('#');

        // Must be 6 characters (RRGGBB)
        if (hex.Length != 6)
            return null;

        try
        {
            int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);

            return new Color(r / 255f, g / 255f, b / 255f, tempColor.a); // Keep alpha
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handle settings clicks.
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        if (UISettings.Instance == null) return;

        // Handle based on current mode
        if (currentMode == SettingsMode.Normal)
        {
            HandleNormalModeClick(relativeX, relativeY);
        }
        else
        {
            HandleColorPickerClick(relativeX, relativeY);
        }
    }

    /// <summary>
    /// Handle clicks in normal settings mode.
    /// </summary>
    private void HandleNormalModeClick(float relativeX, float relativeY)
    {
        // Metronome checkbox (16, 16, checkbox + label only) - moved from 20, 20
        if (relativeX > 16 && relativeX < 96 && relativeY > 16 && relativeY < 32)
        {
            UISettings.Instance.metronome = !UISettings.Instance.metronome;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] Metronome: {UISettings.Instance.metronome}");
            return;
        }

        // WASD Camera checkbox (16, 36, checkbox + label only) - moved from 20, 40
        if (relativeX > 16 && relativeX < 106 && relativeY > 36 && relativeY < 52)
        {
            UISettings.Instance.wasdCamera = !UISettings.Instance.wasdCamera;
            UISettings.Instance.SaveSettings();
            Debug.Log($"[SettingsPanel] WASD Camera: {UISettings.Instance.wasdCamera}");
            return;
        }

        // Color button clicks (right column) - all moved 4px left and up
        // Hover (116, 16) - moved from 120, 20
        if (relativeX > 116 && relativeX < 186 && relativeY > 16 && relativeY < 32)
        {
            StartColorEditing(SettingsMode.EditingHoverColor, UISettings.Instance.hoverColor);
            return;
        }

        // Destination (116, 36) - moved from 120, 40
        if (relativeX > 116 && relativeX < 186 && relativeY > 36 && relativeY < 52)
        {
            StartColorEditing(SettingsMode.EditingDestinationColor, UISettings.Instance.destinationColor);
            return;
        }

        // Player (116, 56) - moved from 120, 60
        if (relativeX > 116 && relativeX < 186 && relativeY > 56 && relativeY < 72)
        {
            StartColorEditing(SettingsMode.EditingPlayerColor, UISettings.Instance.playerTileColor);
            return;
        }

        // NPC (116, 76) - moved from 120, 80
        if (relativeX > 116 && relativeX < 186 && relativeY > 76 && relativeY < 92)
        {
            StartColorEditing(SettingsMode.EditingNPCColor, UISettings.Instance.npcTileColor);
            return;
        }

        // Check Volume slider click (Y=126-141) - moved from 130-145 (startY 106+20=126)
        if (relativeX > 71 && relativeX < 181 && relativeY > 126 && relativeY < 141)
        {
            isDraggingVolume = true;
            tempVolume = UISettings.Instance.volume;
            HandleSliderDrag(relativeX, 71, 110, 0.0f, 1.0f, true, out float newValue);
            tempVolume = newValue;
            return;
        }

        // Check UI Scale slider click (Y=171-186) - moved from 175-190 (startY 151+20=171)
        if (relativeX > 71 && relativeX < 181 && relativeY > 171 && relativeY < 186)
        {
            isDraggingUIScale = true;
            tempUIScale = UISettings.Instance.maxUiScale;
            HandleSliderDrag(relativeX, 71, 110, 0.5f, 3.0f, true, out float newValue);
            tempUIScale = newValue;
            return;
        }

        // Check Ping slider click (Y=216-231) - moved from 220-235 (startY 196+20=216)
        if (relativeX > 71 && relativeX < 181 && relativeY > 216 && relativeY < 231)
        {
            isDraggingPing = true;
            tempPing = UISettings.Instance.inputDelay;
            HandleSliderDrag(relativeX, 71, 110, 0, 200, false, out float newValue);
            tempPing = Mathf.RoundToInt(newValue);
            return;
        }

        // Check for key binding button clicks - moved from startY 240 to 236
        float buttonSize = 30;
        float gap = 8;
        float startX = 6; // Moved from 10
        float startY = 236; // Moved from 240

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

    /// <summary>
    /// Handle clicks in color picker mode.
    /// </summary>
    private void HandleColorPickerClick(float relativeX, float relativeY)
    {
        // Check RGBA slider clicks
        float sliderStartY = 105;
        float sliderSpacing = 25;

        // R slider
        if (relativeX > 40 && relativeX < 155 && relativeY > sliderStartY && relativeY < sliderStartY + 15)
        {
            isDraggingR = true;
            HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
            tempColor.r = newValue;
            return;
        }

        // G slider
        if (relativeX > 40 && relativeX < 155 && relativeY > sliderStartY + sliderSpacing && relativeY < sliderStartY + sliderSpacing + 15)
        {
            isDraggingG = true;
            HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
            tempColor.g = newValue;
            return;
        }

        // B slider
        if (relativeX > 40 && relativeX < 155 && relativeY > sliderStartY + sliderSpacing * 2 && relativeY < sliderStartY + sliderSpacing * 2 + 15)
        {
            isDraggingB = true;
            HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
            tempColor.b = newValue;
            return;
        }

        // A slider
        if (relativeX > 40 && relativeX < 155 && relativeY > sliderStartY + sliderSpacing * 3 && relativeY < sliderStartY + sliderSpacing * 3 + 15)
        {
            isDraggingA = true;
            HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
            tempColor.a = newValue;
            return;
        }

        // Check hex input click to start editing
        if (relativeX > 60 && relativeX < 140 && relativeY > 215 && relativeY < 233)
        {
            if (!isEditingHex)
            {
                isEditingHex = true;
                hexInputBuffer = ColorToHex(tempColor).TrimStart('#');
                Debug.Log("[SettingsPanel] Started hex editing");
            }
            return;
        }

        // Check button clicks
        float buttonY = 246;
        float buttonWidth = 55;
        float buttonHeight = 20;
        float gap = 8;
        float totalWidth = buttonWidth * 3 + gap * 2;
        float startX = (204 - totalWidth) / 2f;

        // Cancel button
        if (relativeX > startX && relativeX < startX + buttonWidth && relativeY > buttonY && relativeY < buttonY + buttonHeight)
        {
            CancelColorEditing();
            return;
        }

        // Reset button
        if (relativeX > startX + buttonWidth + gap && relativeX < startX + (buttonWidth + gap) * 2 && relativeY > buttonY && relativeY < buttonY + buttonHeight)
        {
            ResetCurrentColor();
            return;
        }

        // Done button
        if (relativeX > startX + (buttonWidth + gap) * 2 && relativeX < startX + buttonWidth * 3 + gap * 2 && relativeY > buttonY && relativeY < buttonY + buttonHeight)
        {
            ApplyColorAndExit();
            return;
        }
    }

    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        if (currentMode == SettingsMode.Normal)
        {
            // Handle normal mode slider dragging
            if (isDraggingVolume)
            {
                HandleSliderDrag(relativeX, 71, 110, 0.0f, 1.0f, true, out float newValue);
                tempVolume = newValue;
            }

            if (isDraggingUIScale)
            {
                HandleSliderDrag(relativeX, 71, 110, 0.5f, 3.0f, true, out float newValue);
                tempUIScale = newValue;
            }

            if (isDraggingPing)
            {
                HandleSliderDrag(relativeX, 71, 110, 0, 200, false, out float newValue);
                tempPing = Mathf.RoundToInt(newValue);
            }

            // Handle mouse wheel for sliders
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                // Volume slider area (Y=106-141, moved from 110-145)
                if (relativeX > 16 && relativeX < 181 && relativeY > 106 && relativeY < 141)
                {
                    float change = scroll > 0 ? 0.05f : -0.05f;
                    UISettings.Instance.volume = Mathf.Clamp(UISettings.Instance.volume + change, 0.0f, 1.0f);
                    UISettings.Instance.SaveSettings();
                }
                // UI Scale slider area (Y=151-186, moved from 155-190)
                else if (relativeX > 16 && relativeX < 181 && relativeY > 151 && relativeY < 186)
                {
                    float change = scroll > 0 ? 0.1f : -0.1f;
                    UISettings.Instance.maxUiScale = Mathf.Clamp(UISettings.Instance.maxUiScale + change, 0.5f, 3.0f);
                    UISettings.Instance.SaveSettings();
                }
                // Ping slider area (Y=196-231, moved from 200-235)
                else if (relativeX > 16 && relativeX < 181 && relativeY > 196 && relativeY < 231)
                {
                    int change = scroll > 0 ? 10 : -10;
                    UISettings.Instance.inputDelay = Mathf.Clamp(UISettings.Instance.inputDelay + change, 0, 200);
                    UISettings.Instance.SaveSettings();
                }
            }
        }
        else
        {
            // Handle color picker slider dragging
            if (isDraggingR)
            {
                HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
                tempColor.r = newValue;
            }
            if (isDraggingG)
            {
                HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
                tempColor.g = newValue;
            }
            if (isDraggingB)
            {
                HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
                tempColor.b = newValue;
            }
            if (isDraggingA)
            {
                HandleColorSliderDrag(relativeX, 40, 115, 0f, 1f, out float newValue);
                tempColor.a = newValue;
            }
        }
    }

    public override void OnMouseUp()
    {
        if (currentMode == SettingsMode.Normal)
        {
            // Apply normal mode slider values
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
        else
        {
            // Stop color slider dragging
            isDraggingR = false;
            isDraggingG = false;
            isDraggingB = false;
            isDraggingA = false;
        }
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

    /// <summary>
    /// Handle color slider drag input.
    /// </summary>
    private void HandleColorSliderDrag(float mouseX, float sliderX, float sliderWidth,
                                       float minValue, float maxValue, out float newValue)
    {
        float percent = Mathf.Clamp01((mouseX - sliderX) / sliderWidth);
        newValue = Mathf.Lerp(minValue, maxValue, percent);
    }

    /// <summary>
    /// Start editing a specific color.
    /// </summary>
    private void StartColorEditing(SettingsMode mode, Color currentColor)
    {
        currentMode = mode;
        tempColor = currentColor;
        originalColor = currentColor;
        isEditingHex = false;
        hexInputBuffer = "";
        Debug.Log($"[SettingsPanel] Started editing {GetCurrentColorName()}");
    }

    /// <summary>
    /// Cancel color editing and revert to original.
    /// </summary>
    private void CancelColorEditing()
    {
        tempColor = originalColor;
        currentMode = SettingsMode.Normal;
        isEditingHex = false;
        hexInputBuffer = "";
        Debug.Log("[SettingsPanel] Cancelled color editing");
    }

    /// <summary>
    /// Reset current color to default.
    /// </summary>
    private void ResetCurrentColor()
    {
        switch (currentMode)
        {
            case SettingsMode.EditingHoverColor:
                tempColor = Color.white;
                break;
            case SettingsMode.EditingPlayerColor:
                tempColor = new Color(1f, 0.84f, 0f); // Gold
                break;
            case SettingsMode.EditingDestinationColor:
                tempColor = Color.white;
                break;
            case SettingsMode.EditingNPCColor:
                tempColor = Color.red;
                break;
        }
        isEditingHex = false;
        hexInputBuffer = "";
        Debug.Log($"[SettingsPanel] Reset {GetCurrentColorName()} to default");
    }

    /// <summary>
    /// Apply color changes and exit picker.
    /// CRITICAL: This triggers the rebuild of all tile highlights!
    /// </summary>
    private void ApplyColorAndExit()
    {
        // Apply to UISettings
        switch (currentMode)
        {
            case SettingsMode.EditingHoverColor:
                UISettings.Instance.hoverColor = tempColor;
                break;
            case SettingsMode.EditingPlayerColor:
                UISettings.Instance.playerTileColor = tempColor;
                break;
            case SettingsMode.EditingDestinationColor:
                UISettings.Instance.destinationColor = tempColor;
                break;
            case SettingsMode.EditingNPCColor:
                UISettings.Instance.npcTileColor = tempColor;
                break;
        }

        // Save settings
        UISettings.Instance.SaveSettings();

        // CRITICAL: Rebuild all tile highlights with new colors!
        TileHighlight tileHighlight = Object.FindAnyObjectByType<TileHighlight>();
        if (tileHighlight != null)
        {
            tileHighlight.RebuildAllHighlights();
            Debug.Log($"[SettingsPanel] Applied {GetCurrentColorName()} and rebuilt highlights");
        }
        else
        {
            Debug.LogWarning("[SettingsPanel] Could not find TileHighlight to rebuild!");
        }

        // Clear hex editing state
        isEditingHex = false;
        hexInputBuffer = "";

        // Return to normal mode
        currentMode = SettingsMode.Normal;
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
