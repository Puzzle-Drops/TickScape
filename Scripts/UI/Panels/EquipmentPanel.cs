using UnityEngine;

/// <summary>
/// Equipment panel showing equipped items and weight.
/// SDK Reference: EquipmentControls.ts
/// </summary>
public class EquipmentPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/equipment_panel";
    protected override string TabTexturePath => "UI/Tabs/equipment_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.equipmentKey : KeyCode.F4;

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Equipment slot background
    private Texture2D slotUsedTexture;

    // Currently clicked slot (for visual feedback)
    private EquipmentSlot? clickedSlot = null;

    // Currently hovered slot (for hover highlight)
    private EquipmentSlot? hoveredSlot = null;

    // Item sprite cache
    private System.Collections.Generic.Dictionary<ItemName, Texture2D> itemSpriteCache =
        new System.Collections.Generic.Dictionary<ItemName, Texture2D>();

    // Text style for weight
    private GUIStyle weightStyle;
    private GUIStyle weightStyleShadow;

    // Equipment slot positions
    private struct SlotPosition
    {
        public EquipmentSlot slot;
        public float x;
        public float y;

        public SlotPosition(EquipmentSlot slot, float x, float y)
        {
            this.slot = slot;
            this.x = x;
            this.y = y;
        }
    }

    private SlotPosition[] slotPositions = new SlotPosition[]
    {
        new SlotPosition(EquipmentSlot.HELMET, 84, 11),
        new SlotPosition(EquipmentSlot.CAPE, 43, 50),
        new SlotPosition(EquipmentSlot.NECKLACE, 84, 50),
        new SlotPosition(EquipmentSlot.AMMO, 124, 50),
        new SlotPosition(EquipmentSlot.WEAPON, 28, 89),
        new SlotPosition(EquipmentSlot.CHEST, 84, 89),
        new SlotPosition(EquipmentSlot.OFFHAND, 140, 89),
        new SlotPosition(EquipmentSlot.LEGS, 84, 129),
        new SlotPosition(EquipmentSlot.GLOVES, 28, 169),
        new SlotPosition(EquipmentSlot.FEET, 84, 169),
        new SlotPosition(EquipmentSlot.RING, 140, 169)
    };

    public override void Initialize()
    {
        base.Initialize();

        // Load slot background
        slotUsedTexture = TextureLoader.LoadTexture(
            "UI/Elements/equipment_slot_used",
            new Color(0.4f, 0.4f, 0.4f),
            36, 36
        );

        // Weight text style
        weightStyle = UIFonts.CreateTextStyle(16, UIFonts.WhiteText, TextAnchor.MiddleLeft);
        weightStyleShadow = UIFonts.CreateShadowStyle(16, TextAnchor.MiddleLeft);
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Update font size
        weightStyle.fontSize = Mathf.RoundToInt(16 * scale);
        weightStyleShadow.fontSize = weightStyle.fontSize;

        // Draw all equipment slots
        foreach (var slotPos in slotPositions)
        {
            DrawEquipmentSlot(player, slotPos.slot, x, y, slotPos.x, slotPos.y, scale);
        }

        // Draw hover highlight
        if (hoveredSlot.HasValue)
        {
            DrawSlotHighlight(x, y, scale, hoveredSlot.Value);
        }

        // Draw weight
        string weightText = $"Weight: {player.GetWeight():F3}kg";
        UIFonts.DrawShadowedText(new Rect(x + 10 * scale, y + 250 * scale, 150, 20), weightText, weightStyle, scale);
    }

    /// <summary>
    /// Draw hover highlight on equipment slot.
    /// </summary>
    private void DrawSlotHighlight(float panelX, float panelY, float scale, EquipmentSlot slot)
    {
        // Find slot position
        SlotPosition? slotPos = null;
        foreach (var pos in slotPositions)
        {
            if (pos.slot == slot)
            {
                slotPos = pos;
                break;
            }
        }

        if (!slotPos.HasValue) return;

        Rect slotRect = new Rect(
            panelX + slotPos.Value.x * scale,
            panelY + slotPos.Value.y * scale,
            36 * scale,
            36 * scale
        );

        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.1f); // Grey with low transparency
        GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw a single equipment slot.
    /// SDK Reference: EquipmentControls.drawEquipment() in EquipmentControls.ts lines 120-148
    /// </summary>
    private void DrawEquipmentSlot(Player player, EquipmentSlot slot, float panelX, float panelY,
                                    float slotX, float slotY, float scale)
    {
        Equipment equipment = GetEquipmentInSlot(player, slot);

        if (equipment != null)
        {
            // Draw slot background
            Rect slotRect = new Rect(
                panelX + slotX * scale,
                panelY + slotY * scale,
                36 * scale,
                36 * scale
            );
            GUI.DrawTexture(slotRect, slotUsedTexture);

            // Draw item sprite with click feedback (SDK Reference: EquipmentControls.ts lines 142-153)
            Texture2D sprite = GetItemSprite(equipment.itemName);
            if (sprite != null)
            {
                // Use GetAspectFitRect to maintain aspect ratio within the slot
                Rect spriteRect = GetAspectFitRect(slotRect, sprite);

                // Apply click feedback (dim sprite to 50% opacity like SDK)
                Color oldColor = GUI.color;
                if (clickedSlot.HasValue && clickedSlot.Value == slot)
                {
                    GUI.color = new Color(1, 1, 1, 0.5f); // 50% opacity for click feedback
                }

                GUI.DrawTexture(spriteRect, sprite);
                GUI.color = oldColor;
            }
            else
            {
                // Fallback: Draw item name as text (only if sprite completely failed to load)
                GUIStyle itemStyle = new GUIStyle();
                itemStyle.font = UIFonts.VT323;
                itemStyle.fontSize = Mathf.RoundToInt(8 * scale);
                itemStyle.normal.textColor = Color.yellow;
                itemStyle.alignment = TextAnchor.MiddleCenter;
                itemStyle.wordWrap = true;

                string itemText = equipment.itemName.ToString().Replace("_", "\n");
                GUI.Label(slotRect, itemText, itemStyle);
            }
        }
    }

    /// <summary>
    /// Get item sprite from cache or load it.
    /// Same system as InventoryPanel.
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
            new Color(0.3f, 0.3f, 0.3f), // Gray placeholder
            32, 32
        );

        // Cache it
        itemSpriteCache[itemName] = sprite;

        return sprite;
    }

    /// <summary>
    /// Get equipment in specific slot.
    /// </summary>
    private Equipment GetEquipmentInSlot(Player player, EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.WEAPON: return player.equipment.weapon;
            case EquipmentSlot.OFFHAND: return player.equipment.offhand;
            case EquipmentSlot.HELMET: return player.equipment.helmet;
            case EquipmentSlot.NECKLACE: return player.equipment.necklace;
            case EquipmentSlot.CHEST: return player.equipment.chest;
            case EquipmentSlot.LEGS: return player.equipment.legs;
            case EquipmentSlot.FEET: return player.equipment.feet;
            case EquipmentSlot.GLOVES: return player.equipment.gloves;
            case EquipmentSlot.RING: return player.equipment.ring;
            case EquipmentSlot.CAPE: return player.equipment.cape;
            case EquipmentSlot.AMMO: return player.equipment.ammo;
            default: return null;
        }
    }

    /// <summary>
    /// Handle equipment slot clicks to unequip.
    /// SDK Reference: EquipmentControls.panelClickDown() in EquipmentControls.ts lines 44-74
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null) return;

        EquipmentSlot? clicked = GetClickedSlot(relativeX, relativeY);

        if (clicked.HasValue)
        {
            clickedSlot = clicked;

            // Queue unequip action
            if (InputManager.Instance != null)
            {
                EquipmentSlot slotToUnequip = clicked.Value;
                InputManager.Instance.QueueAction(() => UnequipSlot(player, slotToUnequip));
            }
            else
            {
                UnequipSlot(player, clicked.Value);
            }
        }
    }

    public override void OnPanelClickUp(float relativeX, float relativeY)
    {
        clickedSlot = null;
    }

    public override void OnMouseUp()
    {
        clickedSlot = null;
        
        // Clear hover preview when unequipping
        HoverPreview.Clear();
    }

    /// <summary>
    /// Handle cursor movement for hover detection.
    /// </summary>
    public override void OnCursorMoved(float relativeX, float relativeY)
    {
        // Update hovered slot
        hoveredSlot = GetClickedSlot(relativeX, relativeY);

        // Update hover preview for tooltips
        Player player = FindPlayer();
        if (player != null && hoveredSlot.HasValue)
        {
            Equipment equipment = GetEquipmentInSlot(player, hoveredSlot.Value);
            if (equipment != null)
            {
                // Show "Unequip" tooltip with no effects (equipment doesn't preview stat changes)
                HoverPreview.SetHover(equipment, player, Input.mousePosition, "Unequip");
            }
            else
            {
                HoverPreview.Clear();
            }
        }
        else
        {
            HoverPreview.Clear();
        }
    }

    /// <summary>
    /// Determine which slot was clicked/hovered.
    /// SDK Reference: EquipmentControls.panelClickDown() coordinate checks
    /// </summary>
    private EquipmentSlot? GetClickedSlot(float x, float y)
    {
        if (x > 84 && y > 11 && x < 84 + 36 && y < 11 + 36)
            return EquipmentSlot.HELMET;
        else if (x > 43 && y > 50 && x < 43 + 36 && y < 50 + 36)
            return EquipmentSlot.CAPE;
        else if (x > 84 && y > 50 && x < 84 + 36 && y < 50 + 36)
            return EquipmentSlot.NECKLACE;
        else if (x > 124 && y > 50 && x < 124 + 36 && y < 50 + 36)
            return EquipmentSlot.AMMO;
        else if (x > 28 && y > 89 && x < 28 + 36 && y < 89 + 36)
            return EquipmentSlot.WEAPON;
        else if (x > 84 && y > 89 && x < 84 + 36 && y < 89 + 36)
            return EquipmentSlot.CHEST;
        else if (x > 140 && y > 89 && x < 140 + 36 && y < 89 + 36)
            return EquipmentSlot.OFFHAND;
        else if (x > 84 && y > 129 && x < 84 + 36 && y < 129 + 36)
            return EquipmentSlot.LEGS;
        else if (x > 28 && y > 169 && x < 28 + 36 && y < 169 + 36)
            return EquipmentSlot.GLOVES;
        else if (x > 84 && y > 169 && x < 84 + 36 && y < 169 + 36)
            return EquipmentSlot.FEET;
        else if (x > 140 && y > 169 && x < 140 + 36 && y < 169 + 36)
            return EquipmentSlot.RING;

        return null;
    }

    /// <summary>
    /// Unequip item from slot.
    /// SDK Reference: EquipmentControls.unequipItem() in EquipmentControls.ts lines 87-90
    /// </summary>
    private void UnequipSlot(Player player, EquipmentSlot slot)
    {
        Equipment equipment = GetEquipmentInSlot(player, slot);

        if (equipment != null)
        {
            equipment.Unequip(player);
            Debug.Log($"[EquipmentPanel] Unequipped {equipment.itemName}");
        }
    }

    /// <summary>
    /// Calculate rect that fits sprite within bounds while maintaining aspect ratio.
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

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }

}
