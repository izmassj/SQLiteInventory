using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private Dictionary<ItemType, List<InventorySlot>> pockets;

    public event System.Action<Item, int, int> ItemQuantityChanged;

    private int _bulkDepth = 0;
    public bool IsInBulkUpdate => _bulkDepth > 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        InitializePockets();
    }

    void InitializePockets()
    {
        pockets = new Dictionary<ItemType, List<InventorySlot>>();

        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            pockets.Add(type, new List<InventorySlot>());
        }
    }

    public void AddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        List<InventorySlot> pocket = pockets[item.type];

        InventorySlot existing = pocket.FirstOrDefault(s => s.item == item);

        if (existing != null && item.stackable)
        {
            int oldQty = existing.quantity;
            existing.quantity += amount;
            existing.quantity = Mathf.Min(existing.quantity, item.maxStack);

            RaiseQuantityChanged(item, oldQty, existing.quantity);
        }
        else
        {
            int clamped = item.stackable ? Mathf.Clamp(amount, 1, Mathf.Max(1, item.maxStack)) : 1;
            pocket.Add(new InventorySlot(item, clamped));

            RaiseQuantityChanged(item, 0, clamped);
        }
    }

    public void RemoveItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        List<InventorySlot> pocket = pockets[item.type];

        InventorySlot slot = pocket.FirstOrDefault(s => s.item == item);

        if (slot == null)
            return;

        int oldQty = slot.quantity;

        slot.quantity -= amount;

        if (slot.quantity <= 0)
        {
            pocket.Remove(slot);
            RaiseQuantityChanged(item, oldQty, 0);
        }
        else
        {
            RaiseQuantityChanged(item, oldQty, slot.quantity);
        }
    }

    public void SetItemQuantity(Item item, int quantity)
    {
        if (item == null)
            return;

        List<InventorySlot> pocket = pockets[item.type];
        InventorySlot slot = pocket.FirstOrDefault(s => s.item == item);
        int oldQty = slot != null ? slot.quantity : 0;

        int newQty;
        if (quantity <= 0)
        {
            if (slot != null)
                pocket.Remove(slot);
            newQty = 0;
        }
        else
        {
            newQty = item.stackable ? Mathf.Clamp(quantity, 1, Mathf.Max(1, item.maxStack)) : 1;
            if (slot == null)
            {
                pocket.Add(new InventorySlot(item, newQty));
            }
            else
            {
                slot.quantity = newQty;
            }
        }

        if (oldQty != newQty)
            RaiseQuantityChanged(item, oldQty, newQty);
    }

    public int GetItemQuantity(Item item)
    {
        if (item == null) return 0;
        List<InventorySlot> pocket = pockets[item.type];
        InventorySlot slot = pocket.FirstOrDefault(s => s.item == item);
        return slot != null ? slot.quantity : 0;
    }

    public IEnumerable<InventorySlot> GetAllSlots()
    {
        foreach (var kv in pockets)
        {
            foreach (var slot in kv.Value)
                yield return slot;
        }
    }

    public void ClearInventory()
    {
        foreach (var pocket in pockets.Values)
        {
            pocket.Clear();
        }

        var player = FindAnyObjectByType<PlayerMovementController>();
        if (player != null)
            player.SetCurrentPlayerState(PlayerState.Idle);
    }

    public void FillWithAllItems()
    {
        ClearInventory();

        Item[] allItems = Resources.LoadAll<Item>("");

        foreach (Item item in allItems)
        {
            if (item == null)
                continue;

            int amountToAdd = item.stackable
                ? Mathf.Clamp(99, 1, item.maxStack)
                : 1;

            AddItem(item, amountToAdd);
        }

        var player = FindAnyObjectByType<PlayerMovementController>();
        if (player != null)
            player.SetCurrentPlayerState(PlayerState.Idle);
    }

    public List<InventorySlot> GetPocket(ItemType type)
    {
        return pockets[type];
    }

    public void BeginBulkUpdate() => _bulkDepth++;
    public void EndBulkUpdate() => _bulkDepth = Mathf.Max(0, _bulkDepth - 1);

    private void RaiseQuantityChanged(Item item, int oldQty, int newQty)
    {
        if (IsInBulkUpdate)
            return;

        ItemQuantityChanged?.Invoke(item, oldQty, newQty);
    }
}
