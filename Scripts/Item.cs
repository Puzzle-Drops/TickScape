using UnityEngine;

/// <summary>
/// Base class for all items (equipment, food, potions, etc).
/// SDK Reference: Item.ts
/// </summary>
public class Item
{
    [Header("Item Properties")]
    public ItemName itemName = ItemName.NONE;
    public string defaultAction = "Use";

    [SerializeField]
    private float _weight = 0f;

    /// <summary>
    /// Weight of this item in kg.
    /// SDK Reference: Item.weight in Item.ts
    /// Can be overridden in subclasses (e.g., Potion weight varies by doses).
    /// </summary>
    public virtual float Weight
    {
        get { return _weight; }
        set { _weight = value; }
    }

    // Runtime state
    public Vector2Int groundLocation;
    public bool selected = false;
    private string _serialNumber;

    /// <summary>
    /// Unique identifier for this item instance.
    /// SDK Reference: Item.serialNumber getter in Item.ts
    /// </summary>
    public string SerialNumber
    {
        get
        {
            if (string.IsNullOrEmpty(_serialNumber))
            {
                _serialNumber = Random.Range(0f, 1f).ToString();
            }
            return _serialNumber;
        }
    }

    /// <summary>
    /// Can this item be left-clicked in inventory?
    /// SDK Reference: Item.hasInventoryLeftClick getter
    /// </summary>
    public virtual bool HasInventoryLeftClick
    {
        get { return false; }
    }

    /// <summary>
    /// Handle left-click in inventory.
    /// SDK Reference: Item.inventoryLeftClick()
    /// </summary>
    public virtual void InventoryLeftClick(Player player)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Find this item's position in player's inventory.
    /// Returns -1 if not in inventory.
    /// SDK Reference: Item.inventoryPosition()
    /// </summary>
    public int InventoryPosition(Player player)
    {
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i] != null && player.inventory[i].SerialNumber == this.SerialNumber)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Remove this item from player's inventory.
    /// SDK Reference: Item.consumeItem()
    /// </summary>
    public void ConsumeItem(Player player)
    {
        int pos = InventoryPosition(player);
        if (pos >= 0)
        {
            player.inventory[pos] = null;
        }
    }
}
