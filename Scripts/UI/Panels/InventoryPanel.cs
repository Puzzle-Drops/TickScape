using UnityEngine;

/// <summary>
/// Inventory panel with drag-drop, anti-drag, and context menus.
/// SDK Reference: InventoryControls.ts
/// </summary>
public class InventoryPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/inventory_panel";
    protected override string TabTexturePath => "UI/Tabs/inventory_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.inventoryKey : KeyCode.Tab;

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Anti-drag system
    // SDK Reference: InventoryControls.ts lines 28-29
    private const float MS_PER_ANTI_DRAG = 20f; // per client tick
    private const float DRAG_RADIUS = 5f;

    private float antiDragTimerAt = 0f;
    private Item clickedDownItem = null;
    private Vector2 clickedDownLocation = Vector2.zero;
    private Vector2 cursorLocation = Vector2.zero;
    private bool draggedItem = false;

    // Visual cache (updates on world tick)
    // SDK Reference: InventoryControls.ts lines 35-37
    private Item[] inventoryCache = new Item[28];

    // Item sprite cache
    private System.Collections.Generic.Dictionary<ItemName, Texture2D> itemSpriteCache =
        new System.Collections.Generic.Dictionary<ItemName, Texture2D>();

    // Inventory grid constants
    private const int GRID_COLS = 4;
    private const int GRID_ROWS = 7;
    private const float GRID_START_X = 20f;
    private const float GRID_START_Y = 17f;
    private const float GRID_CELL_WIDTH = 43f;
    private const float GRID_CELL_HEIGHT = 35f;
    private const float ITEM_SIZE = 32f;

    public override void Initialize()
    {
        base.Initialize();

        // Initialize empty cache
        for (int i = 0; i < 28; i++)
        {
            inventoryCache[i] = null;
        }
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Draw all 28 inventory slots (4 columns x 7 rows)
        for (int i = 0; i < 28; i++)
        {
            int col = i % 4;
            int row = i / 4;

            float itemX = x + (GRID_START_X + col * GRID_CELL_WIDTH) * scale;
            float itemY = y + (GRID_START_Y + row * GRID_CELL_HEIGHT) * scale;

            Item item = inventoryCache[i];

            if (item != null)
            {
                // Draw item sprite
                DrawItemSprite(item, itemX, itemY, scale, item == clickedDownItem);

                // Draw selection circle if selected
                if (item.selected)
                {
                    DrawSelectionCircle(itemX, itemY, scale);
                }
            }
        }

        // Draw grey highlight on hovered slot (always, not just when dragging)
        Vector2Int? hoveredSlot = GetSlotAtPosition(cursorLocation.x, cursorLocation.y);
        if (hoveredSlot.HasValue)
        {
            DrawSlotHighlight(x, y, scale, hoveredSlot.Value.x, hoveredSlot.Value.y);
        }

        // Draw dragged item following cursor
        if (clickedDownItem != null && draggedItem)
        {
            DrawDraggedItem(x, y, scale);
        }
    }

    /// <summary>
    /// Get slot coordinates at panel-relative position.
    /// Returns (col, row) OR null if outside inventory grid.
    /// </summary>
    private Vector2Int? GetSlotAtPosition(float relativeX, float relativeY)
    {
        // Calculate which slot based on position
        float gridX = relativeX - GRID_START_X;
        float gridY = relativeY - GRID_START_Y;

        int col = Mathf.FloorToInt(gridX / GRID_CELL_WIDTH);
        int row = Mathf.FloorToInt(gridY / GRID_CELL_HEIGHT);

        // Check if within valid grid bounds
        if (col >= 0 && col < GRID_COLS && row >= 0 && row < GRID_ROWS)
        {
            return new Vector2Int(col, row);
        }

        return null;
    }

    /// <summary>
    /// Draw slot highlight during drag or hover (grey with low transparency).
    /// </summary>
    private void DrawSlotHighlight(float panelX, float panelY, float scale, int col, int row)
    {
        float itemX = panelX + (GRID_START_X + col * GRID_CELL_WIDTH) * scale;
        float itemY = panelY + (GRID_START_Y + row * GRID_CELL_HEIGHT) * scale;
        float slotSize = ITEM_SIZE * scale;

        Rect slotRect = new Rect(itemX, itemY, slotSize, slotSize);

        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.1f); // Grey with low transparency
        GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw item sprite in inventory slot.
    /// SDK Reference: InventoryControls.draw() in InventoryControls.ts lines 189-213
    /// </summary>
    private void DrawItemSprite(Item item, float x, float y, float scale, bool isDimmed)
    {
        // Get or load item sprite - PASS THE ITEM OBJECT FOR POTIONS
        Texture2D sprite = GetItemSprite(item.itemName, item);
        if (sprite != null)
        {
            // Draw sprite (includes colored placeholders if sprite missing)
            float spriteWidth = sprite.width * scale;
            float spriteHeight = sprite.height * scale;
            float xOffset = (ITEM_SIZE * scale - spriteWidth) / 2f;
            float yOffset = (ITEM_SIZE * scale - spriteHeight) / 2f;
            Rect spriteRect = new Rect(x + xOffset, y + yOffset, spriteWidth, spriteHeight);
            // Dim if being dragged
            if (isDimmed && !draggedItem)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.4f);
                GUI.DrawTexture(spriteRect, sprite);
                GUI.color = oldColor;
            }
            else if (isDimmed && draggedItem)
            {
                // Don't draw here - will draw at cursor
            }
            else
            {
                GUI.DrawTexture(spriteRect, sprite);
            }
        }
        else
        {
            // Fallback: Draw item name as text (only if sprite loading completely failed)
            GUIStyle style = new GUIStyle();
            style.font = UIFonts.VT323;
            style.fontSize = Mathf.RoundToInt(8 * scale);
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;
            string itemText = item.itemName.ToString().Replace("_", "\n");
            // Dim text if being dragged
            if (isDimmed && !draggedItem)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1, 1, 0, 0.4f); // Dimmed yellow
                GUI.Label(new Rect(x, y, ITEM_SIZE * scale, ITEM_SIZE * scale), itemText, style);
                GUI.color = oldColor;
            }
            else if (!isDimmed || !draggedItem)
            {
                GUI.Label(new Rect(x, y, ITEM_SIZE * scale, ITEM_SIZE * scale), itemText, style);
            }
        }
    }

    /// <summary>
    /// Draw item being dragged at cursor position.
    /// SDK Reference: InventoryControls.draw() dragged item section
    /// </summary>
    private void DrawDraggedItem(float panelX, float panelY, float scale)
    {
        if (clickedDownItem == null) return;

        Texture2D sprite = GetItemSprite(clickedDownItem.itemName, clickedDownItem);

        if (sprite != null)
        {
            float spriteWidth = sprite.width * scale;
            float spriteHeight = sprite.height * scale;

            // Scale the cursor location before adding to panel position
            Rect spriteRect = new Rect(
                panelX + (cursorLocation.x * scale) - spriteWidth / 2f,
                panelY + (cursorLocation.y * scale) - spriteHeight / 2f,
                spriteWidth,
                spriteHeight
            );

            // Draw at cursor with transparency
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.4f);
            GUI.DrawTexture(spriteRect, sprite);
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// Draw selection circle around item.
    /// SDK Reference: InventoryControls.draw() selection section lines 219-224
    /// </summary>
    private void DrawSelectionCircle(float x, float y, float scale)
    {
        // Create circular selection indicator
        // For simplicity, draw a tinted box (proper circle would need custom texture)
        Color oldColor = GUI.color;
        GUI.color = new Color(0.82f, 0.73f, 0.47f, 0.45f); // #D1BB7773 from SDK

        float circleSize = ITEM_SIZE * scale;
        Rect circleRect = new Rect(x, y, circleSize, circleSize);
        GUI.DrawTexture(circleRect, Texture2D.whiteTexture);

        GUI.color = oldColor;
    }

    /// <summary>
    /// Get item sprite from cache or load it.
    /// </summary>
    private Texture2D GetItemSprite(ItemName itemName, Item item = null)
    {
        if (itemName == ItemName.NONE) return null;

        // Build sprite name
        string itemNameStr = itemName.ToString().ToLower();

        // SPECIAL CASE: Potions need dose count
        bool isPotion = false;
        if (item is Potion potion)
        {
            // Append dose count: stamina_potion_4, super_combat_potion_3, etc.
            itemNameStr = $"{itemNameStr}_{potion.doses}";
            isPotion = true;
        }

        // DON'T cache potions (sprite changes as doses are consumed)
        if (isPotion)
        {
            return TextureLoader.LoadTexture(
                $"UI/Items/{itemNameStr}",
                new Color(0.3f, 0.3f, 0.3f), // Gray placeholder
                32, 32
            );
        }

        // Check cache for non-potions
        if (itemSpriteCache.ContainsKey(itemName))
        {
            return itemSpriteCache[itemName];
        }

        // Load and cache non-potion sprite
        Texture2D sprite = TextureLoader.LoadTexture(
            $"UI/Items/{itemNameStr}",
            new Color(0.3f, 0.3f, 0.3f), // Gray placeholder
            32, 32
        );

        // Cache it
        itemSpriteCache[itemName] = sprite;
        return sprite;
    }

    /// <summary>
    /// Handle mouse down to start drag or click.
    /// SDK Reference: InventoryControls.panelClickDown() in InventoryControls.ts lines 128-151
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        cursorLocation = new Vector2(relativeX, relativeY);
        clickedDownLocation = cursorLocation;
        draggedItem = false;

        // Find clicked item
        Item clickedItem = GetItemAtPosition(relativeX, relativeY);

        // Deselect all items
        foreach (Item item in player.inventory)
        {
            if (item != null)
            {
                item.selected = false;
            }
        }

        if (clickedItem != null)
        {
            clickedDownItem = clickedItem;

            // Calculate anti-drag timer
            float antiDragMs = UISettings.Instance != null
                ? UISettings.Instance.inputDelay * MS_PER_ANTI_DRAG / 20f
                : MS_PER_ANTI_DRAG;

            antiDragTimerAt = Time.time + (antiDragMs / 1000f);
        }
    }

    /// <summary>
    /// Handle mouse up to complete drag or click.
    /// SDK Reference: InventoryControls.panelClickUp() in InventoryControls.ts lines 90-126
    /// Note: OnMouseUp handles the actual swap logic.
    /// </summary>
    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        // OnMouseUp handles drag completion
        // This is just here for consistency
    }

    /// <summary>
    /// Handle cursor movement for drag detection.
    /// SDK Reference: InventoryControls.cursorMovedto() in InventoryControls.ts lines 64-66
    /// </summary>
    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        cursorLocation = new Vector2(relativeX, relativeY);

        // Check if cursor moved far enough to start drag
        if (!draggedItem && clickedDownItem != null)
        {
            float distance = Vector2.Distance(cursorLocation, clickedDownLocation);
            if (distance > DRAG_RADIUS && CanDrag())
            {
                draggedItem = true;
                Debug.Log($"[InventoryPanel] Started dragging item: {clickedDownItem.itemName}");
            }
        }
    }

    /// <summary>
    /// Handle mouse up anywhere (even outside panel).
    /// SDK Reference: InventoryControls.onMouseUp() in InventoryControls.ts line 76
    /// CRITICAL: This handles the actual item swap since it's called even when mouse is outside panel.
    /// </summary>
    public override void OnMouseUp()
    {
        Player player = FindPlayer();

        // Handle non-drag clicks
        if (!draggedItem && clickedDownItem != null)
        {
            // This is a click, not a drag - handle item action
            if (clickedDownItem.HasInventoryLeftClick)
            {
                // Queue left-click action (equip, eat, etc.)
                if (InputManager.Instance != null)
                {
                    Item itemToClick = clickedDownItem;
                    InputManager.Instance.QueueAction(() =>
                    {
                        itemToClick.InventoryLeftClick(player);
                    });
                }
                else
                {
                    clickedDownItem.InventoryLeftClick(player);
                }
            }
            else
            {
                // Select item
                clickedDownItem.selected = true;
            }

            clickedDownItem = null;
            draggedItem = false;
            return;
        }

        // Handle drag and swap
        if (player == null || clickedDownItem == null || !draggedItem)
        {
            clickedDownItem = null;
            draggedItem = false;
            return;
        }

        // Prevent swapping if anti-drag not elapsed
        if (!CanDrag())
        {
            Debug.Log("[InventoryPanel] Anti-drag timer not elapsed");
            clickedDownItem = null;
            draggedItem = false;
            return;
        }

        // Find drop target slot using last cursor position
        Vector2Int? targetSlot = GetSlotAtPosition(cursorLocation.x, cursorLocation.y);

        if (targetSlot.HasValue)
        {
            // Convert slot coordinates to inventory index
            int targetIndex = targetSlot.Value.y * GRID_COLS + targetSlot.Value.x;
            int draggedIndex = clickedDownItem.InventoryPosition(player);

            if (draggedIndex >= 0 && targetIndex >= 0 && targetIndex < 28 && draggedIndex != targetIndex)
            {
                Item targetItem = player.inventory[targetIndex];

                Debug.Log($"[InventoryPanel] Moving {clickedDownItem.itemName} from slot {draggedIndex} to slot {targetIndex}");

                // Update visual cache immediately for instant feedback
                inventoryCache[targetIndex] = clickedDownItem;
                inventoryCache[draggedIndex] = targetItem;

                // Queue actual swap for next tick
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.QueueAction(() =>
                    {
                        player.SwapItemPositions(draggedIndex, targetIndex);
                    });
                }
                else
                {
                    player.SwapItemPositions(draggedIndex, targetIndex);
                }
            }
        }

        clickedDownItem = null;
        draggedItem = false;
    }

    /// <summary>
    /// Handle right-click for context menu.
    /// SDK Reference: InventoryControls.panelRightClick() in InventoryControls.ts lines 68-88
    /// </summary>
    public override void OnPanelRightClick(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        Item clickedItem = GetItemAtPosition(relativeX, relativeY);

        if (clickedItem != null)
        {
            Debug.Log($"[InventoryPanel] Right-clicked on {clickedItem.itemName}");
            // TODO: Show context menu with item actions
            // For now, just show available actions in console
            ShowItemActions(clickedItem, player);
        }
    }

    /// <summary>
    /// Update inventory cache on world tick.
    /// SDK Reference: InventoryControls.onWorldTick() in InventoryControls.ts lines 78-80
    /// </summary>
    public override void OnWorldTick()
    {
        Player player = FindPlayer();
        if (player == null) return;

        // Copy current inventory to cache
        for (int i = 0; i < 28; i++)
        {
            inventoryCache[i] = player.inventory[i];
        }
    }

    /// <summary>
    /// Check if enough time has passed for drag to be allowed.
    /// SDK Reference: InventoryControls.canDrag() in InventoryControls.ts lines 153-155
    /// </summary>
    private bool CanDrag()
    {
        return Time.time >= antiDragTimerAt;
    }

    /// <summary>
    /// Get item at given panel-relative position.
    /// </summary>
    private Item GetItemAtPosition(float x, float y)
    {
        Player player = FindPlayer();
        if (player == null) return null;

        for (int i = 0; i < 28; i++)
        {
            if (inventoryCache[i] == null) continue;

            int col = i % 4;
            int row = i / 4;

            float itemX = GRID_START_X + col * GRID_CELL_WIDTH;
            float itemY = GRID_START_Y + row * GRID_CELL_HEIGHT;

            // Check if click is within item area
            if (x >= itemX && x < itemX + ITEM_SIZE &&
                y >= itemY && y < itemY + ITEM_SIZE)
            {
                return inventoryCache[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Show available actions for item (temporary debug version).
    /// TODO: Replace with proper context menu UI.
    /// </summary>
    private void ShowItemActions(Item item, Player player)
    {
        Debug.Log($"--- {item.itemName} Actions ---");

        if (item is Equipment)
        {
            Debug.Log("  - Equip");
        }

        if (item is Food)
        {
            Debug.Log("  - Eat");
        }

        Debug.Log("  - Drop");
        Debug.Log("  - Examine");
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }
}