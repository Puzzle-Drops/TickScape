using UnityEngine;

/// <summary>
/// Base class for all UI panels.
/// SDK Reference: BaseControls.ts
/// </summary>
public abstract class BasePanel
{
    [Header("Visual Assets")]
    protected Texture2D panelTexture;
    protected Texture2D tabTexture;

    [Header("State")]
    public bool isSelected = false;

    // Resource paths for sprites
    protected abstract string PanelTexturePath { get; }
    protected abstract string TabTexturePath { get; }

    // Fallback colors if sprites missing
    protected virtual Color PanelFallbackColor => new Color(0.3f, 0.25f, 0.2f); // Brown
    protected virtual Color TabFallbackColor => new Color(0.4f, 0.35f, 0.3f);   // Lighter brown

    /// <summary>
    /// Key binding for this panel.
    /// SDK Reference: BaseControls.keyBinding getter
    /// </summary>
    public virtual KeyCode KeyBinding => KeyCode.None;

    /// <summary>
    /// Is this panel available to use?
    /// SDK Reference: BaseControls.isAvailable getter
    /// </summary>
    public virtual bool IsAvailable => true;

    /// <summary>
    /// Does this panel appear on left side in mobile mode?
    /// SDK Reference: BaseControls.appearsOnLeftInMobile getter
    /// </summary>
    public virtual bool AppearsOnLeftInMobile => true;

    /// <summary>
    /// Initialize panel (load textures).
    /// </summary>
    public virtual void Initialize()
    {
        panelTexture = TextureLoader.LoadTexture(
            PanelTexturePath, 
            PanelFallbackColor, 
            204, 275
        );

        tabTexture = TextureLoader.LoadTexture(
            TabTexturePath, 
            TabFallbackColor, 
            33, 36
        );

        Debug.Log($"[{GetType().Name}] Initialized");
    }

    /// <summary>
    /// Draw the panel.
    /// SDK Reference: BaseControls.draw()
    /// </summary>
    public virtual void DrawPanel(float x, float y, float scale)
    {
        if (panelTexture == null) return;

        Rect panelRect = new Rect(x, y, 204 * scale, 275 * scale);
        GUI.DrawTexture(panelRect, panelTexture);
    }

    /// <summary>
    /// Draw the tab icon.
    /// </summary>
    public virtual void DrawTab(float x, float y, float scale)
    {
        if (tabTexture == null) return;

        Rect tabRect = new Rect(x, y, 33 * scale, 36 * scale);
        GUI.DrawTexture(tabRect, tabTexture);

        // Draw unavailable overlay
        if (!IsAvailable)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(tabRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        // Draw selection indicator
        if (isSelected)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 1, 0, 0.45f); // Green tint
            GUI.Box(tabRect, "");
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Called when panel is clicked (mouse down).
    /// SDK Reference: BaseControls.panelClickDown()
    /// </summary>
    public virtual void OnPanelClickDown(float relativeX, float relativeY)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called when panel is released (mouse up).
    /// SDK Reference: BaseControls.panelClickUp()
    /// </summary>
    public virtual void OnPanelClickUp(float relativeX, float relativeY)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called when panel is right-clicked.
    /// SDK Reference: BaseControls.panelRightClick()
    /// </summary>
    public virtual void OnPanelRightClick(float relativeX, float relativeY)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called when cursor moves over panel.
    /// SDK Reference: BaseControls.cursorMovedto()
    /// </summary>
    public virtual void OnCursorMoved(float relativeX, float relativeY)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called when mouse button is released anywhere (even outside panel).
    /// SDK Reference: BaseControls.onMouseUp()
    /// </summary>
    public virtual void OnMouseUp()
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called every world tick.
    /// SDK Reference: BaseControls.onWorldTick()
    /// </summary>
    public virtual void OnWorldTick()
    {
        // Override in subclasses
    }

    /// <summary>
    /// Get panel name for debugging.
    /// </summary>
    public virtual string GetPanelName()
    {
        return GetType().Name.Replace("Panel", "");
    }
}