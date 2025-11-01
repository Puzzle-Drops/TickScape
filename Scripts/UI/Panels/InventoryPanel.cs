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

            float itemX = x + (20 + col * 43) * scale;
            float itemY = y + (17 + (row + 1) * 35) * scale;

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

        // Draw dragged item following cursor
        if (clickedDownItem != null && draggedItem)
        {
            DrawDraggedItem(x, y, scale);
        }
    }

    /// <summary>
    /// Draw item sprite in inventory slot.
    /// SDK Reference: InventoryControls.draw() in InventoryControls.ts lines 189-213
    /// </summary>
    private void DrawItemSprite(Item item, float x, float y, float scale, bool isDimmed)
    {
        // Get or load item sprite
        Texture2D sprite = GetItemSprite(item.itemName);

        bool hasRealSprite = sprite != null && !IsPlaceholderTexture(sprite);

        if (hasRealSprite)
        {
            // Draw real sprite
            float spriteWidth = sprite.width * scale;
            float spriteHeight = sprite.height * scale;
            float xOffset = (32 * scale - spriteWidth) / 2f;
            float yOffset = (32 * scale - spriteHeight) / 2f;

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
            // Fallback: Draw item name as text (sprite missing or transparent)
            GUIStyle style = new GUIStyle();
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
                GUI.Label(new Rect(x, y, 32 * scale, 32 * scale), itemText, style);
                GUI.color = oldColor;
            }
            else if (!isDimmed || !draggedItem)
            {
                GUI.Label(new Rect(x, y, 32 * scale, 32 * scale), itemText, style);
            }
        }
    }

    /// <summary>
    /// Check if texture is a placeholder (all pixels same color).
    /// </summary>
    private bool IsPlaceholderTexture(Texture2D texture)
    {
        if (texture == null) return true;

        // Check if texture is solid color (placeholder)
        // We check first pixel vs a few others
        Color firstPixel = texture.GetPixel(0, 0);

        // If transparent, it's a placeholder
        if (firstPixel.a < 0.1f) return true;

        // Check a few pixels to see if all same color
        int width = texture.width;
        int height = texture.height;

        if (width > 2 && height > 2)
        {
            Color centerPixel = texture.GetPixel(width / 2, height / 2);
            Color cornerPixel = texture.GetPixel(width - 1, height - 1);

            // If all pixels are same color, it's probably a placeholder
            if (ColorsEqual(firstPixel, centerPixel) && ColorsEqual(firstPixel, cornerPixel))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compare two colors (with tolerance).
    /// </summary>
    private bool ColorsEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f &&
               Mathf.Abs(a.g - b.g) < 0.01f &&
               Mathf.Abs(a.b - b.b) < 0.01f &&
               Mathf.Abs(a.a - b.a) < 0.01f;
    }

    /// <summary>
    /// Draw item being dragged at cursor position.
    /// SDK Reference: InventoryControls.draw() dragged item section
    /// </summary>
    private void DrawDraggedItem(float panelX, float panelY, float scale)
    {
        if (clickedDownItem == null) return;

        Texture2D sprite = GetItemSprite(clickedDownItem.itemName);

        if (sprite != null)
        {
            float spriteWidth = sprite.width * scale;
            float spriteHeight = sprite.height * scale;

            // Draw at cursor with transparency
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.4f);

            Rect spriteRect = new Rect(
                panelX + cursorLocation.x - spriteWidth / 2f,
                panelY + cursorLocation.y - spriteHeight / 2f,
                spriteWidth,
                spriteHeight
            );

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

        float circleSize = 32 * scale;
        Rect circleRect = new Rect(x, y, circleSize, circleSize);
        GUI.DrawTexture(circleRect, Texture2D.whiteTexture);

        GUI.color = oldColor;
    }

    /// <summary>
    /// Get item sprite from cache or load it.
    /// </summary>
    private Texture2D GetItemSprite(ItemName itemName)
    {
        if (itemName == ItemName.NONE) return null;

        // Check cache
        if (itemSpriteCache.ContainsKey(itemName))
        {
            return itemSpriteCache[itemName];
        }

        // Try loading sprite
        string itemNameStr = itemName.ToString().ToLower();
        Texture2D sprite = TextureLoader.LoadTexture(
            $"UI/Items/{itemNameStr}",
            new Color(0.3f, 0.3f, 0.3f), // CHANGED: Gray placeholder instead of transparent
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
    /// </summary>
    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null || clickedDownItem == null)
        {
            clickedDownItem = null;
            return;
        }

        bool isPlaceholder = false; // Items are never placeholders in our system

        // Handle click (not drag)
        if (!draggedItem && !isPlaceholder)
        {
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
            cursorLocation = Vector2.zero;
            return;
        }

        // Prevent swapping if anti-drag not elapsed
        if (!CanDrag())
        {
            clickedDownItem = null;
            cursorLocation = Vector2.zero;
            return;
        }

        // Handle drag - find drop target
        Item clickedUpItem = GetItemAtPosition(relativeX, relativeY);
        Item itemBeingDragged = clickedDownItem;

        if (clickedUpItem != null && itemBeingDragged != null)
        {
            int draggedPos = itemBeingDragged.InventoryPosition(player);
            int targetPos = clickedUpItem.InventoryPosition(player);

            if (draggedPos >= 0 && targetPos >= 0 && draggedPos != targetPos)
            {
                // Update visual cache immediately
                inventoryCache[targetPos] = itemBeingDragged;
                inventoryCache[draggedPos] = clickedUpItem;

                // Queue actual swap for next tick
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.QueueAction(() =>
                    {
                        player.SwapItemPositions(targetPos, draggedPos);
                    });
                }
                else
                {
                    player.SwapItemPositions(targetPos, draggedPos);
                }

                Debug.Log($"[InventoryPanel] Swapped items at positions {draggedPos} <-> {targetPos}");
            }
        }

        clickedDownItem = null;
        cursorLocation = Vector2.zero;
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
                Debug.Log("[InventoryPanel] Started dragging item");
            }
        }
    }

    /// <summary>
    /// Handle mouse up anywhere (even outside panel).
    /// SDK Reference: InventoryControls.onMouseUp() in InventoryControls.ts line 76
    /// </summary>
    public override void OnMouseUp()
    {
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

            float itemX = 20 + col * 43;
            float itemY = 17 + (row + 1) * 35;

            // Check if click is within 32x32 item area
            if (x >= itemX && x < itemX + 32 &&
                y >= itemY && y < itemY + 32)
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
        return Object.FindObjectOfType<Player>();
    }
}