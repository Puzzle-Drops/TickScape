using UnityEngine;

/// <summary>
/// Centralized font management for the entire game.
/// All UI elements should use fonts from here.
/// </summary>
public static class UIFonts
{
    private static Font _vt323;
    private static bool _initialized = false;

    /// <summary>
    /// Primary game font (VT323).
    /// </summary>
    public static Font VT323
    {
        get
        {
            if (!_initialized)
            {
                Initialize();
            }
            return _vt323;
        }
    }

    /// <summary>
    /// Load all fonts on first access.
    /// </summary>
    private static void Initialize()
    {
        _vt323 = Resources.Load<Font>("UI/Fonts/VT323-Regular");

        if (_vt323 == null)
        {
            Debug.LogError("[UIFonts] Failed to load VT323-Regular font from Resources/UI/Fonts/!");
            Debug.LogError("[UIFonts] Falling back to Unity default font.");
            _vt323 = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        else
        {
            Debug.Log("[UIFonts] VT323 font loaded successfully!");
        }

        _initialized = true;
    }

    /// <summary>
    /// Create a standard text style with VT323 font.
    /// </summary>
    public static GUIStyle CreateTextStyle(int fontSize, Color textColor, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GUIStyle style = new GUIStyle();
        style.font = VT323;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.alignment = alignment;
        return style;
    }

    /// <summary>
    /// Create a shadow text style (black, offset by 1 pixel).
    /// </summary>
    public static GUIStyle CreateShadowStyle(int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        return CreateTextStyle(fontSize, Color.black, alignment);
    }

    /// <summary>
    /// Standard yellow text (OSRS-style).
    /// </summary>
    public static Color YellowText => new Color(1f, 0.6f, 0f);

    /// <summary>
    /// Standard white text.
    /// </summary>
    public static Color WhiteText => Color.white;
}