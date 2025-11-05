using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Master UI controller managing all panels and tabs.
/// SDK Reference: ControlPanelController.ts
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    private List<BasePanel> panels = new List<BasePanel>();
    private BasePanel selectedPanel = null;

    [Header("Debug")]
    public bool showDebugInfo = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        InitializePanels();
    }

    /// <summary>
    /// Initialize all panels.
    /// SDK Reference: ControlPanelController constructor in ControlPanelController.ts lines 73-107
    /// </summary>
    private void InitializePanels()
    {
        // Single row of functional panels only
        panels.Add(new CombatPanel());          // Combat - Tab 0
        panels.Add(new StatsPanel());           // Stats - Tab 1
        panels.Add(new SettingsPanel());        // Settings - Tab 2
        panels.Add(new InventoryPanel());       // Inventory - Tab 3
        panels.Add(new EquipmentPanel());       // Equipment - Tab 4
        panels.Add(new PrayerPanel());          // Prayer - Tab 5
        panels.Add(new SpellbookPanel());       // Spellbook - Tab 6

        // Initialize all panels
        foreach (var panel in panels)
        {
            panel.Initialize();
        }

        // Select Combat panel by default
        if (panels.Count > 0)
        {
            selectedPanel = panels[0];
            selectedPanel.isSelected = true;
        }

        Debug.Log($"[UIManager] Initialized {panels.Count} panels (8 functional, 6 placeholders)");
    }

    /// <summary>
    /// Main UI rendering (called every frame).
    /// SDK Reference: ControlPanelController.draw() in ControlPanelController.ts lines 249-283
    /// </summary>
    void OnGUI()
    {
        Vector2 mousePos = Event.current.mousePosition;
        bool mouseOverUI = IsMouseOverUI(mousePos);

        if (mouseOverUI && Event.current.type == EventType.MouseDown)
        {
            Event.current.Use(); // Consume the event - prevents click-through!
        }

        if (panels.Count == 0) return;

        float scale = UIScale.CalculateTabScale();
        float panelScale = UIScale.GetPanelScale();

        // Draw selected panel first (behind tabs)
        if (selectedPanel != null)
        {
            Vector2 panelPos = UILayout.GetPanelPosition();
            selectedPanel.DrawPanel(panelPos.x, panelPos.y, panelScale);
        }

        // Draw all tabs
        for (int i = 0; i < panels.Count; i++)
        {
            Vector2 tabPos = UILayout.GetTabPosition(i);
            panels[i].DrawTab(tabPos.x, tabPos.y, scale);
        }

        // Draw selection highlight on selected tab
        if (selectedPanel != null)
        {
            int selectedIndex = panels.IndexOf(selectedPanel);
            if (selectedIndex >= 0)
            {
                Vector2 tabPos = UILayout.GetTabPosition(selectedIndex);
                Rect tabRect = new Rect(tabPos.x, tabPos.y, 33 * scale, 36 * scale);
                
                // Green outline
                Color oldColor = GUI.color;
                GUI.color = new Color(0, 1, 0, 0.45f);
                DrawBorder(tabRect, 3);
                GUI.color = oldColor;
            }
        }

        // Draw boost panel overlay
        Player player = FindPlayer();
        if (player != null)
        {
            // Check if panels are minimized (no selected panel)
            bool panelsMinimized = (selectedPanel == null);
            BoostPanel.Draw(scale, player, panelsMinimized);
            HealthPanel.Draw(scale, player, panelsMinimized);
        }

// Draw tooltip LAST (on top of everything)
if (HoverPreview.currentHover != null && 
    UISettings.Instance != null && 
    UISettings.Instance.showTooltips)
{
    TooltipRenderer.Draw(HoverPreview.currentHover, scale);
}

        // Debug info
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    /// <summary>
    /// Check if mouse is over any UI element.
    /// </summary>
    public bool IsMouseOverUI(Vector2 mousePos)
    {
        // Convert screen position to GUI coordinates
        Vector2 guiMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);

        float scale = UIScale.CalculateTabScale();
        float panelScale = UIScale.GetPanelScale();

        // Check if over selected panel
        if (selectedPanel != null)
        {
            Vector2 panelPos = UILayout.GetPanelPosition();
            Rect panelRect = new Rect(panelPos.x, panelPos.y, 204 * panelScale, 275 * panelScale);

            if (panelRect.Contains(guiMousePos))
            {
                //Debug.Log($"[UI] Mouse over panel at {guiMousePos}, panel rect: {panelRect}");
                return true;
            }
        }

        // Check if over any tab
        for (int i = 0; i < panels.Count; i++)
        {
            Vector2 tabPos = UILayout.GetTabPosition(i);
            Rect tabRect = new Rect(tabPos.x, tabPos.y, 33 * scale, 36 * scale);

            if (tabRect.Contains(guiMousePos))
            {
                //Debug.Log($"[UI] Mouse over tab {i} at {guiMousePos}, tab rect: {tabRect}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Draw border around rectangle.
    /// </summary>
    private void DrawBorder(Rect rect, float thickness)
    {
        // Top
        GUI.Box(new Rect(rect.x, rect.y, rect.width, thickness), "");
        // Bottom
        GUI.Box(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), "");
        // Left
        GUI.Box(new Rect(rect.x, rect.y, thickness, rect.height), "");
        // Right
        GUI.Box(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), "");
    }

    /// <summary>
    /// Handle input (clicks and keyboard).
    /// </summary>
    void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();

        // Handle key binding for settings panel
        if (selectedPanel is SettingsPanel settingsPanel)
        {
            settingsPanel.HandleKeyBinding();
        }
    }

    /// <summary>
    /// Handle keyboard shortcuts for panels.
    /// SDK Reference: ControlPanelController constructor event listener in ControlPanelController.ts lines 100-106
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Don't process if binding keys
        if (UISettings.Instance != null && UISettings.Instance.isKeybinding)
            return;

        foreach (var panel in panels)
        {
            if (panel.KeyBinding != KeyCode.None && Input.GetKeyDown(panel.KeyBinding))
            {
                // Toggle if this panel is already selected
                if (panel == selectedPanel)
                {
                    selectedPanel = null;  // Minimize
                }
                else
                {
                    SelectPanel(panel);  // Open
                }
            }
        }
    }

    /// <summary>
    /// Handle mouse input (clicks on tabs and panels).
    /// SDK Reference: ControlPanelController.controlPanelClickDown() in ControlPanelController.ts lines 254-279
    /// </summary>
    private void HandleMouseInput()
    {
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y; // Flip for OnGUI coordinates

        float scale = UIScale.CalculateTabScale();
        float panelScale = UIScale.GetPanelScale();

        // Check mouse down
        if (Input.GetMouseButtonDown(0))
        {
            // Check tab clicks
            for (int i = 0; i < panels.Count; i++)
            {
                Vector2 tabPos = UILayout.GetTabPosition(i);
                Rect tabRect = new Rect(tabPos.x, tabPos.y, 33 * scale, 36 * scale);

                if (tabRect.Contains(mousePos))
                {
                    BasePanel clickedPanel = panels[i];
                    
                    // Toggle if clicking selected panel
                    if (clickedPanel == selectedPanel)
                    {
                        selectedPanel = null;
                    }
                    else
                    {
                        SelectPanel(clickedPanel);
                    }
                    return;
                }
            }

            // Check panel clicks
            if (selectedPanel != null)
            {
                Vector2 panelPos = UILayout.GetPanelPosition();
                Rect panelRect = new Rect(panelPos.x, panelPos.y, 204 * panelScale, 275 * panelScale);

                if (panelRect.Contains(mousePos))
                {
                    float relativeX = (mousePos.x - panelPos.x) / panelScale;
                    float relativeY = (mousePos.y - panelPos.y) / panelScale;
                    selectedPanel.OnPanelClickDown(relativeX, relativeY);
                    return;
                }
            }
        }

        // Check mouse up
        if (Input.GetMouseButtonUp(0))
        {
            // Always notify selected panel of mouse up (even if outside)
            if (selectedPanel != null)
            {
                selectedPanel.OnMouseUp();

                Vector2 panelPos = UILayout.GetPanelPosition();
                Rect panelRect = new Rect(panelPos.x, panelPos.y, 204 * panelScale, 275 * panelScale);

                if (panelRect.Contains(mousePos))
                {
                    float relativeX = (mousePos.x - panelPos.x) / panelScale;
                    float relativeY = (mousePos.y - panelPos.y) / panelScale;
                    selectedPanel.OnPanelClickUp(relativeX, relativeY);
                }
            }
        }

        // Check right-click
        if (Input.GetMouseButtonDown(1))
        {
            if (selectedPanel != null)
            {
                Vector2 panelPos = UILayout.GetPanelPosition();
                Rect panelRect = new Rect(panelPos.x, panelPos.y, 204 * panelScale, 275 * panelScale);

                if (panelRect.Contains(mousePos))
                {
                    float relativeX = (mousePos.x - panelPos.x) / panelScale;
                    float relativeY = (mousePos.y - panelPos.y) / panelScale;
                    selectedPanel.OnPanelRightClick(relativeX, relativeY);
                }
            }
        }

        // Track cursor movement
        if (selectedPanel != null)
        {
            Vector2 panelPos = UILayout.GetPanelPosition();
            Rect panelRect = new Rect(panelPos.x, panelPos.y, 204 * panelScale, 275 * panelScale);

            if (panelRect.Contains(mousePos))
            {
                float relativeX = (mousePos.x - panelPos.x) / panelScale;
                float relativeY = (mousePos.y - panelPos.y) / panelScale;
                selectedPanel.OnCursorMoved(relativeX, relativeY);
            }
        }
    }

    /// <summary>
    /// Select a panel.
    /// </summary>
    private void SelectPanel(BasePanel panel)
    {
        if (selectedPanel != null)
        {
            selectedPanel.isSelected = false;
        }

        selectedPanel = panel;
        
        if (selectedPanel != null)
        {
            selectedPanel.isSelected = true;
            Debug.Log($"[UIManager] Selected panel: {panel.GetPanelName()}");
        }
    }

    /// <summary>
    /// Called by WorldManager each game tick.
    /// SDK Reference: ControlPanelController.onWorldTick() in ControlPanelController.ts line 285
    /// </summary>
    public void OnWorldTick()
    {
        foreach (var panel in panels)
        {
            panel.OnWorldTick();
        }
    }

    /// <summary>
    /// Draw debug information.
    /// </summary>
    private void DrawDebugInfo()
    {
        string info = $"UI Scale: {UIScale.CalculateTabScale():F2}\n";
        info += $"Panel Scale: {UIScale.GetPanelScale():F2}\n";
        info += $"Selected: {(selectedPanel != null ? selectedPanel.GetPanelName() : "None")}\n";
        info += $"Panels: {panels.Count}";

        GUI.Label(new Rect(10, 10, 300, 100), info);
    }

    /// <summary>
    /// Find the player in the scene.
    /// </summary>
    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }


}
