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

    // Text style for weight
    private GUIStyle weightStyle;
    private GUIStyle weightStyleShadow;

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
        weightStyle = new GUIStyle();
        weightStyle.fontSize = 16;
        weightStyle.normal.textColor = Color.white;
        weightStyle.alignment = TextAnchor.MiddleLeft;

        weightStyleShadow = new GUIStyle(weightStyle);
        weightStyleShadow.normal.textColor = Color.black;
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
        DrawEquipmentSlot(player, EquipmentSlot.HELMET, x, y, 84, 11, scale);
        DrawEquipmentSlot(player, EquipmentSlot.CAPE, x, y, 43, 50, scale);
        DrawEquipmentSlot(player, EquipmentSlot.NECKLACE, x, y, 84, 50, scale);
        DrawEquipmentSlot(player, EquipmentSlot.AMMO, x, y, 124, 50, scale);
        DrawEquipmentSlot(player, EquipmentSlot.WEAPON, x, y, 28, 89, scale);
        DrawEquipmentSlot(player, EquipmentSlot.CHEST, x, y, 84, 89, scale);
        DrawEquipmentSlot(player, EquipmentSlot.OFFHAND, x, y, 140, 89, scale);
        DrawEquipmentSlot(player, EquipmentSlot.LEGS, x, y, 84, 129, scale);
        DrawEquipmentSlot(player, EquipmentSlot.GLOVES, x, y, 28, 169, scale);
        DrawEquipmentSlot(player, EquipmentSlot.FEET, x, y, 84, 169, scale);
        DrawEquipmentSlot(player, EquipmentSlot.RING, x, y, 140, 169, scale);

        // Draw weight
        string weightText = $"Weight: {player.GetWeight():F3}kg";
        GUI.Label(new Rect(x + 56 * scale, y + 268 * scale, 150, 20), weightText, weightStyleShadow);
        GUI.Label(new Rect(x + 55 * scale, y + 267 * scale, 150, 20), weightText, weightStyle);
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

            // Dim if clicked
            if (clickedSlot.HasValue && clickedSlot.Value == slot)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUI.DrawTexture(slotRect, slotUsedTexture);
                GUI.color = oldColor;
            }

            // TODO: Draw item sprite when sprites are available
            // For now, draw item name as text
            GUIStyle itemStyle = new GUIStyle();
            itemStyle.fontSize = Mathf.RoundToInt(8 * scale);
            itemStyle.normal.textColor = Color.yellow;
            itemStyle.alignment = TextAnchor.MiddleCenter;
            itemStyle.wordWrap = true;

            string itemText = equipment.itemName.ToString().Replace("_", "\n");
            GUI.Label(slotRect, itemText, itemStyle);
        }
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
    }

    /// <summary>
    /// Determine which slot was clicked.
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

    private Player FindPlayer()
    {
        return Object.FindObjectOfType<Player>();
    }

}