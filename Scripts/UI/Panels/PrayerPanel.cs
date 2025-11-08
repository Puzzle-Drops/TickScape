using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Prayer panel with 5x6 grid, draggable prayers, and edit mode.
/// SDK Reference: PrayerControls.ts
/// </summary>
public class PrayerPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/prayer_panel";
    protected override string TabTexturePath => "UI/Tabs/prayer_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.prayerKey : KeyCode.F5;

    public override bool IsAvailable => true;

    // Grid dimensions
    private const int GRID_COLS = 5;
    private const int GRID_ROWS = 6;
    private const float GRID_CELL_SIZE = 36f; // Base size before scaling
    private const float GRID_START_X = 14f;
    private const float GRID_START_Y = 22f;
    private const float PRAYER_ICON_SIZE = 36f; // Fill entire cell, no padding

    // Edit mode
    private bool isEditMode = false;

    // Drag state (similar to InventoryPanel)
    private Prayer draggedPrayer = null;
    private Vector2 dragStartLocation = Vector2.zero;
    private Vector2 currentCursorLocation = Vector2.zero;
    private bool isDragging = false;
    private const float DRAG_START_THRESHOLD = 5f;

    // Sprite cache (normal and overhead versions)
    private Dictionary<PrayerType, Texture2D> prayerSpriteCache = new Dictionary<PrayerType, Texture2D>();
    private Dictionary<PrayerType, Texture2D> prayerSpriteCacheOverhead = new Dictionary<PrayerType, Texture2D>();

    // Track which prayers are marked for quick prayers
    private Dictionary<PrayerType, bool> quickPrayerMarks = new Dictionary<PrayerType, bool>();

    // Edit mode button
    private GUIStyle editModeTextStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Edit mode text style
        editModeTextStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleCenter);

        // Initialize quick prayer marks from settings
        if (UISettings.Instance != null)
        {
            foreach (var prayerType in UISettings.Instance.quickPrayerSelections)
            {
                quickPrayerMarks[prayerType] = true;
            }
        }

        Debug.Log("[PrayerPanel] Initialized with grid-based positioning");
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return;

        // Update text style font size
        editModeTextStyle.fontSize = Mathf.RoundToInt(14 * scale);

        // Draw all prayers at their grid positions
        var prayers = player.prayerController.prayers;
        foreach (var prayer in prayers)
        {
            DrawPrayerAtGridPosition(prayer, x, y, scale, player);
        }

        // Draw highlight on hovered grid cell (when dragging)
        if (isDragging && draggedPrayer != null)
        {
            Vector2Int? hoveredCell = GetGridCellAtPosition(currentCursorLocation.x, currentCursorLocation.y);
            if (hoveredCell.HasValue && IsValidGridCell(hoveredCell.Value))
            {
                DrawGridCellHighlight(x, y, scale, hoveredCell.Value);
            }
        }

        // Draw dragged prayer following cursor (with transparency)
        if (isDragging && draggedPrayer != null)
        {
            DrawDraggedPrayer(x, y, scale);
        }

        // Draw edit mode button at bottom
        DrawEditModeButton(x, y, scale);

        // Draw edit mode text if enabled
        if (isEditMode)
        {
            DrawEditModeText(x, y, scale);
        }
    }

    /// <summary>
    /// Draw a prayer at its grid position.
    /// </summary>
    private void DrawPrayerAtGridPosition(Prayer prayer, float panelX, float panelY, float scale, Player player)
    {
        // Get grid position from UISettings
        Vector2Int gridPos = UISettings.Instance.GetPrayerGridPosition(prayer.type);

        // Calculate screen position
        Rect cellRect = GetGridCellRect(panelX, panelY, scale, gridPos);

        // No padding - icon fills entire cell
        Rect iconRect = cellRect;

        // Get prayer sprite (overhead version if active and not being dragged)
        bool showOverhead = prayer.isActive && !(draggedPrayer == prayer && isDragging);
        Texture2D sprite = GetPrayerSprite(prayer.type, showOverhead);

        // Dim if being dragged
        bool isDimmed = (draggedPrayer == prayer && isDragging);

        if (sprite != null)
        {
            // Calculate rect that maintains aspect ratio
            Rect spriteRect = GetAspectFitRect(iconRect, sprite);

            // Draw sprite (includes colored placeholders if sprite missing)
            Color oldColor = GUI.color;
            if (isDimmed)
            {
                GUI.color = new Color(1, 1, 1, 0.4f); // Dimmed
            }
            GUI.DrawTexture(spriteRect, sprite);
            GUI.color = oldColor;
        }
        else
        {
            // Fallback: Draw prayer name as text (only if sprite loading completely failed)
            GUIStyle textStyle = new GUIStyle();
            textStyle.font = UIFonts.VT323;
            textStyle.fontSize = Mathf.RoundToInt(8 * scale);
            textStyle.normal.textColor = isDimmed ? new Color(1, 1, 0, 0.4f) : Color.yellow;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;

            GUI.Label(iconRect, prayer.GetName(), textStyle);
        }

        // Draw level requirement overlay (dark if can't use)
        if (player.stats.prayer < prayer.GetLevelRequirement())
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        // Draw quick prayer 'X' mark if selected
        if (quickPrayerMarks.ContainsKey(prayer.type) && quickPrayerMarks[prayer.type])
        {
            // Draw yellow 'X' in bottom-right corner of prayer icon
            GUIStyle xStyle = UIFonts.CreateTextStyle(
                Mathf.RoundToInt(12 * scale),
                UIFonts.YellowText,
                TextAnchor.LowerRight
            );

            Rect xRect = new Rect(
                iconRect.x + iconRect.width - (28 * scale),
                iconRect.y + iconRect.height - (20 * scale),
                20 * scale,
                20 * scale
            );

            UIFonts.DrawShadowedText(xRect, "X", xStyle, scale);
        }

    }

    /// <summary>
    /// Draw grid cell highlight during drag.
    /// </summary>
    private void DrawGridCellHighlight(float panelX, float panelY, float scale, Vector2Int gridPos)
    {
        Rect cellRect = GetGridCellRect(panelX, panelY, scale, gridPos);

        Color oldColor = GUI.color;
        GUI.color = new Color(0, 1, 0, 0.3f); // Green with transparency
        GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw prayer being dragged at cursor.
    /// </summary>
    private void DrawDraggedPrayer(float panelX, float panelY, float scale)
    {
        if (draggedPrayer == null) return;

        Texture2D sprite = GetPrayerSprite(draggedPrayer.type, false);
        float iconSize = PRAYER_ICON_SIZE * scale;

        if (sprite != null)
        {
            // CRITICAL FIX: Scale the cursor location before adding to panel position
            Rect spriteRect = new Rect(
                panelX + (currentCursorLocation.x * scale) - iconSize / 2f,
                panelY + (currentCursorLocation.y * scale) - iconSize / 2f,
                iconSize,
                iconSize
            );

            // Calculate rect that maintains aspect ratio
            Rect aspectRect = GetAspectFitRect(spriteRect, sprite);

            // Draw with transparency
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.6f);
            GUI.DrawTexture(aspectRect, sprite);
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Draw edit mode toggle button.
    /// </summary>
    private void DrawEditModeButton(float panelX, float panelY, float scale)
    {
        float buttonWidth = Mathf.Round(80f * scale);
        float buttonHeight = Mathf.Round(20f * scale);
        float buttonX = Mathf.Round(panelX + (204f * scale - buttonWidth) / 2f);
        float buttonY = Mathf.Round(panelY + 246f * scale);

        Rect buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

        // Draw button background rectangle
        Color oldColor = GUI.color;
        GUI.color = isEditMode
            ? new Color(0.2f, 0.8f, 0.2f, 0.5f) // Greenish when active
            : new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray when inactive
        GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Hover highlight overlay
        if (UILayout.IsMouseOverRect(buttonRect))
        {
            oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        // Draw button text
        GUIStyle buttonTextStyle = new GUIStyle
        {
            font = UIFonts.VT323,
            fontSize = Mathf.RoundToInt(12 * scale),
            normal = { textColor = UIFonts.YellowText },
            alignment = TextAnchor.MiddleCenter
        };

        UIFonts.DrawShadowedText(buttonRect, isEditMode ? "Done" : "Edit", buttonTextStyle, scale);
    }

    /// <summary>
    /// Draw edit mode instruction text.
    /// </summary>
    private void DrawEditModeText(float panelX, float panelY, float scale)
    {
        string text = "Drag prayers to reorder";
        float textY = panelY + 230 * scale;
        float textWidth = 204 * scale;

        UIFonts.DrawShadowedText(new Rect(panelX, textY, textWidth, 20), text, editModeTextStyle, scale);
    }

    /// <summary>
    /// Calculate rect that fits sprite within bounds while maintaining aspect ratio.
    /// Centers the sprite within the bounds rect.
    /// </summary>
    private Rect GetAspectFitRect(Rect bounds, Texture2D sprite)
    {
        if (sprite == null)
            return bounds;

        float spriteAspect = (float)sprite.width / sprite.height;
        float boundsAspect = bounds.width / bounds.height;

        float width, height;

        if (spriteAspect > boundsAspect)
        {
            // Sprite is wider - fit to width
            width = bounds.width;
            height = width / spriteAspect;
        }
        else
        {
            // Sprite is taller - fit to height
            height = bounds.height;
            width = height * spriteAspect;
        }

        // Center within bounds
        float x = bounds.x + (bounds.width - width) / 2f;
        float y = bounds.y + (bounds.height - height) / 2f;

        return new Rect(x, y, width, height);
    }

    /// <summary>
    /// Get rectangle for a grid cell.
    /// </summary>
    private Rect GetGridCellRect(float panelX, float panelY, float scale, Vector2Int gridPos)
    {
        float cellX = panelX + (GRID_START_X + gridPos.x * GRID_CELL_SIZE) * scale;
        float cellY = panelY + (GRID_START_Y + gridPos.y * GRID_CELL_SIZE) * scale;
        float cellSize = GRID_CELL_SIZE * scale;

        return new Rect(cellX, cellY, cellSize, cellSize);
    }

    /// <summary>
    /// Get grid cell at panel-relative position.
    /// Returns null if outside grid bounds.
    /// </summary>
    private Vector2Int? GetGridCellAtPosition(float relativeX, float relativeY)
    {
        float gridX = relativeX - GRID_START_X;
        float gridY = relativeY - GRID_START_Y;

        int col = Mathf.FloorToInt(gridX / GRID_CELL_SIZE);
        int row = Mathf.FloorToInt(gridY / GRID_CELL_SIZE);

        if (col >= 0 && col < GRID_COLS && row >= 0 && row < GRID_ROWS)
        {
            return new Vector2Int(col, row);
        }

        return null;
    }

    /// <summary>
    /// Check if grid cell is valid.
    /// </summary>
    private bool IsValidGridCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GRID_COLS && cell.y >= 0 && cell.y < GRID_ROWS;
    }

    /// <summary>
    /// Get prayer at grid position.
    /// </summary>
    private Prayer GetPrayerAtGridPosition(Vector2Int gridPos)
    {
        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return null;

        foreach (var prayer in player.prayerController.prayers)
        {
            Vector2Int prayerPos = UISettings.Instance.GetPrayerGridPosition(prayer.type);
            if (prayerPos == gridPos)
            {
                return prayer;
            }
        }

        return null;
    }

    /// <summary>
    /// Get prayer sprite from cache or load it.
    /// </summary>
    private Texture2D GetPrayerSprite(PrayerType type, bool overhead = false)
    {
        // Choose the appropriate cache
        var cache = overhead ? prayerSpriteCacheOverhead : prayerSpriteCache;

        // Check cache
        if (cache.ContainsKey(type))
        {
            return cache[type];
        }

        // Create temporary prayer to get sprite name
        Prayer tempPrayer = new Prayer { type = type };
        string spriteName = tempPrayer.GetSpriteName();

        // Add suffix for overhead version
        if (overhead)
        {
            spriteName += "_overhead";
        }

        // Load sprite
        Texture2D sprite = TextureLoader.LoadTexture(
            $"UI/Prayers/{spriteName}",
            new Color(0.3f, 0.3f, 0.3f), // Gray placeholder
            36, 36
        );

        // Cache it
        cache[type] = sprite;

        return sprite;
    }

    /// <summary>
    /// Handle panel clicks.
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return;

        // Check edit mode button click
        float buttonWidth = 80;
        float buttonHeight = 20;
        float buttonX = (204 - buttonWidth) / 2f;
        float buttonY = 250;

        if (relativeX >= buttonX && relativeX <= buttonX + buttonWidth &&
            relativeY >= buttonY && relativeY <= buttonY + buttonHeight)
        {
            isEditMode = !isEditMode;
            Debug.Log($"[PrayerPanel] Edit mode: {isEditMode}");
            return;
        }

        // Get clicked grid cell
        Vector2Int? clickedCell = GetGridCellAtPosition(relativeX, relativeY);
        if (!clickedCell.HasValue) return;

        Prayer clickedPrayer = GetPrayerAtGridPosition(clickedCell.Value);

        if (isEditMode)
        {
            // Edit mode: Start dragging
            if (clickedPrayer != null)
            {
                draggedPrayer = clickedPrayer;
                dragStartLocation = new Vector2(relativeX, relativeY);
                currentCursorLocation = dragStartLocation;
                isDragging = false; // Will become true once cursor moves far enough
            }
        }
        else
        {
            // Normal mode: Toggle prayer
            if (clickedPrayer != null)
            {
                // Check if out of prayer points
                if (player.currentStats.prayer <= 0)
                {
                    Debug.Log("[PrayerPanel] Out of prayer points!");
                    return;
                }

                // Check level requirement
                if (player.stats.prayer < clickedPrayer.GetLevelRequirement())
                {
                    Debug.Log($"[PrayerPanel] Need {clickedPrayer.GetLevelRequirement()} Prayer to use {clickedPrayer.GetName()}");
                    return;
                }

                // Toggle prayer
                TogglePrayer(clickedPrayer, player);
            }
        }
    }

    /// <summary>
    /// Handle panel right-clicks for quick prayer selection.
    /// </summary>
    public override void OnPanelRightClick(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null || player.prayerController == null) return;

        // Get clicked grid cell
        Vector2Int? clickedCell = GetGridCellAtPosition(relativeX, relativeY);
        if (!clickedCell.HasValue) return;

        Prayer clickedPrayer = GetPrayerAtGridPosition(clickedCell.Value);

        if (clickedPrayer != null)
        {
            // Toggle quick prayer mark
            bool isMarked = quickPrayerMarks.ContainsKey(clickedPrayer.type) && quickPrayerMarks[clickedPrayer.type];

            if (!isMarked)
            {
                // Mark this prayer and handle conflicts

                // First, remove conflicting prayers (same groups)
                var groups = clickedPrayer.GetGroups();
                foreach (var group in groups)
                {
                    // Find prayers with conflicting groups and unmark them
                    foreach (var prayer in player.prayerController.prayers)
                    {
                        if (prayer != clickedPrayer && prayer.GetGroups().Contains(group))
                        {
                            quickPrayerMarks[prayer.type] = false;
                        }
                    }
                }

                // Now mark this prayer
                quickPrayerMarks[clickedPrayer.type] = true;
                Debug.Log($"[QuickPrayer] Marked {clickedPrayer.GetName()} for quick prayers");
            }
            else
            {
                // Unmark this prayer
                quickPrayerMarks[clickedPrayer.type] = false;
                Debug.Log($"[QuickPrayer] Unmarked {clickedPrayer.GetName()} from quick prayers");
            }

            // Update UISettings with current marks
            UISettings.Instance.quickPrayerSelections.Clear();
            foreach (var kvp in quickPrayerMarks)
            {
                if (kvp.Value)
                {
                    UISettings.Instance.quickPrayerSelections.Add(kvp.Key);
                }
            }
            UISettings.Instance.SaveSettings();
        }
    }

    /// <summary>
    /// Handle mouse up (when released over panel).
    /// Note: Drag completion is handled in OnMouseUp() which is always called.
    /// </summary>
    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        // OnMouseUp handles drag completion
        // This is just here for consistency
    }

    /// <summary>
    /// Handle cursor movement.
    /// </summary>
    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        currentCursorLocation = new Vector2(relativeX, relativeY);

        // Check if we should start dragging
        if (isEditMode && draggedPrayer != null && !isDragging)
        {
            float distance = Vector2.Distance(currentCursorLocation, dragStartLocation);
            if (distance > DRAG_START_THRESHOLD)
            {
                isDragging = true;
                Debug.Log($"[PrayerPanel] Started dragging {draggedPrayer.GetName()}");
            }
        }
    }

    /// <summary>
    /// Handle mouse up anywhere.
    /// CRITICAL: This is called even if mouse is outside panel, so we handle drag completion here.
    /// </summary>
    public override void OnMouseUp()
    {
        if (!isEditMode || draggedPrayer == null)
        {
            draggedPrayer = null;
            isDragging = false;
            return;
        }

        // Complete drag if we were dragging
        if (isDragging)
        {
            // Get drop target grid cell using last cursor position
            Vector2Int? targetCell = GetGridCellAtPosition(currentCursorLocation.x, currentCursorLocation.y);

            if (targetCell.HasValue && IsValidGridCell(targetCell.Value))
            {
                // Get prayer at target position (if any)
                Prayer targetPrayer = GetPrayerAtGridPosition(targetCell.Value);

                if (targetPrayer != null && targetPrayer != draggedPrayer)
                {
                    // Swap positions
                    UISettings.Instance.SwapPrayerPositions(draggedPrayer.type, targetPrayer.type);
                    UISettings.Instance.SaveSettings();
                    Debug.Log($"[PrayerPanel] Swapped {draggedPrayer.GetName()} <-> {targetPrayer.GetName()}");
                }
                else if (targetPrayer == null)
                {
                    // Move to empty cell
                    UISettings.Instance.SetPrayerGridPosition(draggedPrayer.type, targetCell.Value);
                    UISettings.Instance.SaveSettings();
                    Debug.Log($"[PrayerPanel] Moved {draggedPrayer.GetName()} to ({targetCell.Value.x}, {targetCell.Value.y})");
                }
            }
        }

        draggedPrayer = null;
        isDragging = false;
    }

    /// <summary>
    /// Toggle prayer on/off with group logic.
    /// SDK Reference: Prayer.toggle() in BasePrayer.ts
    /// </summary>
    private void TogglePrayer(Prayer prayer, Player player)
    {
        if (prayer.isActive)
        {
            // Deactivate
            prayer.isActive = false;
            Debug.Log($"[PrayerPanel] Deactivated {prayer.GetName()}");
        }
        else
        {
            // Deactivate conflicting prayers in same groups
            var groups = prayer.GetGroups();
            foreach (var group in groups)
            {
                var conflicting = player.prayerController.MatchGroup(group);
                if (conflicting != null && conflicting != prayer)
                {
                    conflicting.isActive = false;
                    Debug.Log($"[PrayerPanel] Deactivated conflicting {conflicting.GetName()}");
                }
            }

            // Activate
            prayer.isActive = true;
            Debug.Log($"[PrayerPanel] Activated {prayer.GetName()}");
        }
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }
}