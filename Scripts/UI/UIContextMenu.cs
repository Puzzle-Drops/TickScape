using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple right-click context menu system for UI elements.
/// Renamed from ContextMenu to avoid conflict with Unity's built-in attribute.
/// SDK Reference: ContextMenu.ts
/// </summary>
public class UIContextMenu
{
    // Singleton instance
    private static UIContextMenu _instance;
    public static UIContextMenu Instance
    {
        get
        {
            if (_instance == null)
                _instance = new UIContextMenu();
            return _instance;
        }
    }

    // Menu state
    private bool isActive = false;
    private Vector2 position;
    private List<MenuOption> options = new List<MenuOption>();
    private float width = 150f;
    private float itemHeight = 22f;
    private int hoveredIndex = -1;

    // Menu option class
    public class MenuOption
    {
        public string text;
        public System.Action action;
        public Color textColor;

        public MenuOption(string text, System.Action action, Color? textColor = null)
        {
            this.text = text;
            this.action = action;
            this.textColor = textColor ?? Color.white;
        }
    }

    /// <summary>
    /// Show context menu with options.
    /// </summary>
    public void Show(Vector2 screenPos, List<MenuOption> menuOptions)
    {
        isActive = true;
        position = screenPos;
        options = menuOptions;

        // Calculate width based on longest text
        GUIStyle testStyle = UIFonts.CreateTextStyle(14, Color.white, TextAnchor.MiddleLeft);
        width = 100f;
        foreach (var option in options)
        {
            Vector2 size = testStyle.CalcSize(new GUIContent(option.text));
            width = Mathf.Max(width, size.x + 20f);
        }
    }

    /// <summary>
    /// Hide the context menu.
    /// </summary>
    public void Hide()
    {
        isActive = false;
        options.Clear();
        hoveredIndex = -1;
    }

    /// <summary>
    /// Draw the context menu.
    /// </summary>
    public void Draw()
    {
        if (!isActive || options.Count == 0)
            return;

        float scale = UIScale.GetPanelScale();
        float height = (options.Count * itemHeight + 10) * scale;

        // Convert screen position to GUI coordinates
        Vector2 guiPos = new Vector2(position.x, Screen.height - position.y);

        // Clamp to screen
        if (guiPos.x + width * scale > Screen.width)
            guiPos.x = Screen.width - width * scale;
        if (guiPos.y + height > Screen.height)
            guiPos.y = Screen.height - height;

        // Draw background
        Color oldColor = GUI.color;
        GUI.color = new Color(0.373f, 0.329f, 0.271f, 0.95f); // #5f5445 with transparency
        GUI.DrawTexture(new Rect(guiPos.x, guiPos.y, width * scale, height), Texture2D.whiteTexture);

        // Draw border
        GUI.color = Color.black;
        DrawBorder(new Rect(guiPos.x, guiPos.y, width * scale, height), 2);
        GUI.color = oldColor;

        // Check hover
        Vector2 mousePos = Event.current.mousePosition;
        hoveredIndex = -1;

        // Draw options
        for (int i = 0; i < options.Count; i++)
        {
            float y = guiPos.y + (5 + i * itemHeight) * scale;
            Rect optionRect = new Rect(guiPos.x, y, width * scale, itemHeight * scale);

            // Check if hovered
            if (optionRect.Contains(mousePos))
            {
                hoveredIndex = i;

                // Draw hover highlight
                oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.2f);
                GUI.DrawTexture(optionRect, Texture2D.whiteTexture);
                GUI.color = oldColor;
            }

            // Draw text
            Color textColor = hoveredIndex == i ? UIFonts.YellowText : options[i].textColor;
            GUIStyle textStyle = UIFonts.CreateTextStyle(
                Mathf.RoundToInt(14 * scale),
                textColor,
                TextAnchor.MiddleLeft
            );

            Rect textRect = new Rect(optionRect.x + 10 * scale, optionRect.y, optionRect.width, optionRect.height);
            UIFonts.DrawShadowedText(textRect, options[i].text, textStyle, scale);
        }
    }

    /// <summary>
    /// Handle click on menu.
    /// </summary>
    public bool HandleClick(Vector2 screenPos)
    {
        if (!isActive)
            return false;

        // Convert to GUI coordinates
        Vector2 guiClick = new Vector2(screenPos.x, Screen.height - screenPos.y);
        Vector2 guiPos = new Vector2(position.x, Screen.height - position.y);
        float scale = UIScale.GetPanelScale();

        // Check if click is on menu
        Rect menuRect = new Rect(guiPos.x, guiPos.y, width * scale, (options.Count * itemHeight + 10) * scale);

        if (menuRect.Contains(guiClick))
        {
            // Execute hovered option
            if (hoveredIndex >= 0 && hoveredIndex < options.Count)
            {
                options[hoveredIndex].action?.Invoke();
            }
            Hide();
            return true;
        }

        // Click outside menu - close it
        Hide();
        return false;
    }

    private void DrawBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}