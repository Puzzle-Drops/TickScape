using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spell types available in Ancient Magicks.
/// </summary>
public enum SpellType
{
    ICE_BARRAGE,
    BLOOD_BARRAGE
}

/// <summary>
/// Individual spell with grid position and state.
/// Similar to Prayer class.
/// </summary>
[System.Serializable]
public class Spell
{
    public SpellType type;
    public bool isSelected = false;

    public string GetName()
    {
        switch (type)
        {
            case SpellType.ICE_BARRAGE: return "Ice Barrage";
            case SpellType.BLOOD_BARRAGE: return "Blood Barrage";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Get sprite resource name for loading from Resources/UI/Spells/
    /// </summary>
    public string GetSpriteName()
    {
        switch (type)
        {
            case SpellType.ICE_BARRAGE: return "ice_barrage";
            case SpellType.BLOOD_BARRAGE: return "blood_barrage";
            default: return "unknown";
        }
    }
}

/// <summary>
/// Ancient spellbook panel with spell selection and drag-and-drop grid positioning.
/// SDK Reference: AncientsSpellbookControls.ts
/// </summary>
public class SpellbookPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/spellbook_ancients";
    protected override string TabTexturePath => "UI/Tabs/spellbook_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.spellbookKey : KeyCode.F6;

    public override bool IsAvailable => true;

    // Grid dimensions (same as prayer panel)
    private const int GRID_COLS = 5;
    private const int GRID_ROWS = 7;
    private const float GRID_CELL_SIZE = 36f;
    private const float GRID_START_X = 11f;
    private const float GRID_START_Y = 2f;
    private const float SPELL_ICON_WIDTH = 23f;
    private const float SPELL_ICON_HEIGHT = 25f;

    // Edit mode
    private bool isEditMode = false;

    // Drag state
    private Spell draggedSpell = null;
    private Vector2 dragStartLocation = Vector2.zero;
    private Vector2 currentCursorLocation = Vector2.zero;
    private bool isDragging = false;
    private const float DRAG_START_THRESHOLD = 5f;

    // Spell list
    private List<Spell> spells = new List<Spell>();

    // Sprite cache
    private Dictionary<SpellType, Texture2D> spellSpriteCache = new Dictionary<SpellType, Texture2D>();

    // Edit mode button style
    private GUIStyle editModeTextStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Initialize spells
        spells.Add(new Spell { type = SpellType.ICE_BARRAGE });
        spells.Add(new Spell { type = SpellType.BLOOD_BARRAGE });

        // Edit mode text style
        editModeTextStyle = new GUIStyle();
        editModeTextStyle.fontSize = 14;
        editModeTextStyle.normal.textColor = Color.yellow;
        editModeTextStyle.alignment = TextAnchor.MiddleCenter;

        Debug.Log("[SpellbookPanel] Initialized with grid-based positioning");
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Update text style font size
        editModeTextStyle.fontSize = Mathf.RoundToInt(14 * scale);

        // Draw all spells at their grid positions
        foreach (var spell in spells)
        {
            DrawSpellAtGridPosition(spell, x, y, scale);
        }

        // Draw highlight on hovered grid cell (when dragging)
        if (isDragging && draggedSpell != null)
        {
            Vector2Int? hoveredCell = GetGridCellAtPosition(currentCursorLocation.x, currentCursorLocation.y);
            if (hoveredCell.HasValue && IsValidGridCell(hoveredCell.Value))
            {
                DrawGridCellHighlight(x, y, scale, hoveredCell.Value);
            }
        }

        // Draw dragged spell following cursor (with transparency)
        if (isDragging && draggedSpell != null)
        {
            DrawDraggedSpell(x, y, scale);
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
    /// Draw a spell at its grid position.
    /// </summary>
    private void DrawSpellAtGridPosition(Spell spell, float panelX, float panelY, float scale)
    {
        // Get grid position from UISettings
        Vector2Int gridPos = UISettings.Instance.GetSpellGridPosition(spell.type);

        // Calculate screen position
        Rect cellRect = GetGridCellRect(panelX, panelY, scale, gridPos);

        // Center the spell icon within the cell
        float paddingX = (GRID_CELL_SIZE - SPELL_ICON_WIDTH) / 2f * scale;
        float paddingY = (GRID_CELL_SIZE - SPELL_ICON_HEIGHT) / 2f * scale;
        Rect iconRect = new Rect(
            cellRect.x + paddingX,
            cellRect.y + paddingY,
            SPELL_ICON_WIDTH * scale,
            SPELL_ICON_HEIGHT * scale
        );

        // Get spell sprite
        Texture2D sprite = GetSpellSprite(spell.type);

        // Dim if being dragged
        bool isDimmed = (draggedSpell == spell && isDragging);

        if (sprite != null)
        {
            // Calculate rect that maintains aspect ratio
            Rect spriteRect = GetAspectFitRect(iconRect, sprite);

            // Draw sprite
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
            // Fallback: Draw spell name as text
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = Mathf.RoundToInt(8 * scale);
            textStyle.normal.textColor = isDimmed ? new Color(1, 1, 0, 0.4f) : Color.yellow;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;

            GUI.Label(iconRect, spell.GetName(), textStyle);
        }

        // Draw selection highlight (if selected)
        if (spell.isSelected && !isDimmed)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.82f, 0.73f, 0.47f, 0.45f); // Golden highlight
            GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
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
    /// Draw spell being dragged at cursor.
    /// </summary>
    private void DrawDraggedSpell(float panelX, float panelY, float scale)
    {
        if (draggedSpell == null) return;

        Texture2D sprite = GetSpellSprite(draggedSpell.type);
        float iconWidth = SPELL_ICON_WIDTH * scale;
        float iconHeight = SPELL_ICON_HEIGHT * scale;

        if (sprite != null)
        {
            // Scale the cursor location before adding to panel position
            Rect spriteRect = new Rect(
                panelX + (currentCursorLocation.x * scale) - iconWidth / 2f,
                panelY + (currentCursorLocation.y * scale) - iconHeight / 2f,
                iconWidth,
                iconHeight
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
        float buttonY = Mathf.Round(panelY + 250f * scale);

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
            fontSize = Mathf.RoundToInt(12 * scale),
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        GUI.Label(buttonRect, isEditMode ? "Done" : "Edit", buttonTextStyle);
    }

    /// <summary>
    /// Draw edit mode instruction text.
    /// </summary>
    private void DrawEditModeText(float panelX, float panelY, float scale)
    {
        string text = "Drag spells to reorder";
        float textY = panelY + 230 * scale;
        float textWidth = 204 * scale;

        GUI.Label(new Rect(panelX, textY, textWidth, 20), text, editModeTextStyle);
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
    /// Get spell at grid position.
    /// </summary>
    private Spell GetSpellAtGridPosition(Vector2Int gridPos)
    {
        foreach (var spell in spells)
        {
            Vector2Int spellPos = UISettings.Instance.GetSpellGridPosition(spell.type);
            if (spellPos == gridPos)
            {
                return spell;
            }
        }

        return null;
    }

    /// <summary>
    /// Get spell sprite from cache or load it.
    /// </summary>
    private Texture2D GetSpellSprite(SpellType type)
    {
        // Check cache
        if (spellSpriteCache.ContainsKey(type))
        {
            return spellSpriteCache[type];
        }

        // Create temporary spell to get sprite name
        Spell tempSpell = new Spell { type = type };
        string spriteName = tempSpell.GetSpriteName();

        // Load sprite
        Texture2D sprite = TextureLoader.LoadTexture(
            $"UI/Spells/{spriteName}",
            new Color(0.3f, 0.3f, 0.3f), // Gray placeholder
            (int)SPELL_ICON_WIDTH, (int)SPELL_ICON_HEIGHT
        );

        // Cache it
        spellSpriteCache[type] = sprite;

        return sprite;
    }

    /// <summary>
    /// Handle panel clicks.
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        // Check edit mode button click
        float buttonWidth = 80;
        float buttonHeight = 20;
        float buttonX = (204 - buttonWidth) / 2f;
        float buttonY = 250;

        if (relativeX >= buttonX && relativeX <= buttonX + buttonWidth &&
            relativeY >= buttonY && relativeY <= buttonY + buttonHeight)
        {
            isEditMode = !isEditMode;
            Debug.Log($"[SpellbookPanel] Edit mode: {isEditMode}");
            return;
        }

        // Get clicked grid cell
        Vector2Int? clickedCell = GetGridCellAtPosition(relativeX, relativeY);
        if (!clickedCell.HasValue) return;

        Spell clickedSpell = GetSpellAtGridPosition(clickedCell.Value);

        if (isEditMode)
        {
            // Edit mode: Start dragging
            if (clickedSpell != null)
            {
                draggedSpell = clickedSpell;
                dragStartLocation = new Vector2(relativeX, relativeY);
                currentCursorLocation = dragStartLocation;
                isDragging = false; // Will become true once cursor moves far enough
            }
        }
        else
        {
            // Normal mode: Select/deselect spell
            if (clickedSpell != null)
            {
                // Check if clicking the same spell (toggle off)
                if (clickedSpell.isSelected)
                {
                    // Deselect
                    clickedSpell.isSelected = false;
                    Debug.Log($"[SpellbookPanel] Deselected {clickedSpell.GetName()}");
                    // TODO: player.manualSpellCastSelection = null;
                }
                else
                {
                    // Clear all other selections
                    foreach (var spell in spells)
                    {
                        spell.isSelected = false;
                    }

                    // Select this spell
                    clickedSpell.isSelected = true;
                    Debug.Log($"[SpellbookPanel] Selected {clickedSpell.GetName()}");
                    // TODO: player.manualSpellCastSelection = create spell instance
                }
            }
        }
    }

    /// <summary>
    /// Handle mouse up (when released over panel).
    /// </summary>
    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        // OnMouseUp handles drag completion
    }

    /// <summary>
    /// Handle cursor movement.
    /// </summary>
    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        currentCursorLocation = new Vector2(relativeX, relativeY);

        // Check if we should start dragging
        if (isEditMode && draggedSpell != null && !isDragging)
        {
            float distance = Vector2.Distance(currentCursorLocation, dragStartLocation);
            if (distance > DRAG_START_THRESHOLD)
            {
                isDragging = true;
                Debug.Log($"[SpellbookPanel] Started dragging {draggedSpell.GetName()}");
            }
        }
    }

    /// <summary>
    /// Handle mouse up anywhere.
    /// CRITICAL: This is called even if mouse is outside panel, so we handle drag completion here.
    /// </summary>
    public override void OnMouseUp()
    {
        if (!isEditMode || draggedSpell == null)
        {
            draggedSpell = null;
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
                // Get spell at target position (if any)
                Spell targetSpell = GetSpellAtGridPosition(targetCell.Value);

                if (targetSpell != null && targetSpell != draggedSpell)
                {
                    // Swap positions
                    UISettings.Instance.SwapSpellPositions(draggedSpell.type, targetSpell.type);
                    UISettings.Instance.SaveSettings();
                    Debug.Log($"[SpellbookPanel] Swapped {draggedSpell.GetName()} <-> {targetSpell.GetName()}");
                }
                else if (targetSpell == null)
                {
                    // Move to empty cell
                    UISettings.Instance.SetSpellGridPosition(draggedSpell.type, targetCell.Value);
                    UISettings.Instance.SaveSettings();
                    Debug.Log($"[SpellbookPanel] Moved {draggedSpell.GetName()} to ({targetCell.Value.x}, {targetCell.Value.y})");
                }
            }
        }

        draggedSpell = null;
        isDragging = false;
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }
}