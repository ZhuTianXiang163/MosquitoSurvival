using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryService : MonoBehaviour
{
    public static InventoryService Instance { get; private set; }

    [SerializeField] private ItemDatabase itemDatabase;

    public event Action OnInventoryChanged;
    public event Action OnQuickSlotsChanged;

    public IReadOnlyList<InventorySlotData> Slots => PlayerDataService.Instance.data.slots;
    public IReadOnlyList<QuickSlotData> QuickSlots => PlayerDataService.Instance.data.quickSlots;
    public int Capacity => PlayerDataService.Instance.data.capacity;
    public int UsedSlotCount => Slots.Count(slot => !slot.IsEmpty);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool CanAddItem(ItemId itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        if (def == null)
        {
            return false;
        }

        // Each item goes to its own slot — never auto-stack
        int emptySlots = PlayerDataService.Instance.data.slots.Count(slot => slot.IsEmpty);
        return emptySlots >= amount;
    }

    public bool TryAddItem(ItemId itemId, int amount, out string failReason)
    {
        failReason = string.Empty;

        if (amount <= 0)
        {
            failReason = "Invalid amount";
            return false;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        if (def == null)
        {
            failReason = "Invalid item";
            return false;
        }

        if (!CanAddItem(itemId, amount))
        {
            failReason = "Inventory is full";
            return false;
        }

        PlayerDataService svc = PlayerDataService.Instance;

        // Always use empty slots — never stack into existing slots
        // Each individual item gets its own slot (amount=1 per slot)
        int added = 0;
        foreach (InventorySlotData slot in svc.data.slots)
        {
            if (!slot.IsEmpty)
            {
                continue;
            }

            slot.itemId = itemId.ToString();
            slot.amount = 1;
            added++;
            if (added >= amount)
            {
                break;
            }
        }

        svc.Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemoveItem(ItemId itemId, int amount)
    {
        if (amount <= 0 || GetItemCount(itemId) < amount)
        {
            return false;
        }

        int remaining = amount;
        List<InventorySlotData> slots = PlayerDataService.Instance.data.slots;

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (remaining <= 0)
            {
                break;
            }

            InventorySlotData slot = slots[i];
            if (slot.itemId != itemId.ToString())
            {
                continue;
            }

            int remove = Mathf.Min(slot.amount, remaining);
            slot.amount -= remove;
            remaining -= remove;

            if (slot.amount <= 0)
            {
                slot.itemId = null;
                slot.amount = 0;
            }
        }

        PlayerDataService.Instance.Save();
        OnInventoryChanged?.Invoke();
        ClearEmptyQuickSlots();
        return true;
    }

    public int GetItemCount(ItemId itemId)
    {
        return PlayerDataService.Instance.data.slots
            .Where(slot => slot.itemId == itemId.ToString())
            .Sum(slot => slot.amount);
    }

    public bool HasItem(ItemId itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public IReadOnlyList<InventorySlotData> GetSlotsByCategory(ItemCategory category)
    {
        List<InventorySlotData> result = new List<InventorySlotData>();

        foreach (InventorySlotData slot in PlayerDataService.Instance.data.slots)
        {
            if (slot.IsEmpty || !Enum.TryParse(slot.itemId, out ItemId id))
            {
                continue;
            }

            ItemDefinition def = itemDatabase.Get(id);
            if (def != null && def.category == category)
            {
                result.Add(slot);
            }
        }

        return result;
    }

    public void SortInventory()
    {
        PlayerDataService svc = PlayerDataService.Instance;

        ItemId[] order =
        {
            ItemId.Flower,
            ItemId.Grass,
            ItemId.HerbMedicine,
            ItemId.FloralWater,
            ItemId.MosquitoSpray,
            ItemId.LifePotion
        };

        // Collect all item IDs (one per slot, each slot has amount=1)
        List<ItemId> sortedIds = new List<ItemId>();
        foreach (ItemId id in order)
        {
            int count = GetItemCount(id);
            for (int i = 0; i < count; i++)
            {
                sortedIds.Add(id);
            }
        }

        // Rebuild slots — each item stays in its own slot, never merged
        svc.data.slots.Clear();
        foreach (ItemId id in sortedIds)
        {
            svc.data.slots.Add(new InventorySlotData { itemId = id.ToString(), amount = 1 });
        }

        svc.EnsureSlotsCapacity();
        svc.Save();
        OnInventoryChanged?.Invoke();
    }

    public bool TryUseItem(ItemId itemId, out string message)
    {
        message = string.Empty;

        Debug.Log($"TryUseItem called: {itemId}");

        if (!HasItem(itemId, 1))
        {
            message = "Not enough item";
            Debug.LogWarning($"TryUseItem failed: {message}");
            return false;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        if (def == null || !def.canUse)
        {
            message = "This item cannot be used";
            Debug.LogWarning($"TryUseItem failed: {message}");
            return false;
        }

        if (ItemUseService.Instance == null)
        {
            message = "Item use service is not ready";
            Debug.LogWarning($"TryUseItem failed: {message}");
            return false;
        }

        if (!ItemUseService.Instance.UseItem(itemId, out message))
        {
            Debug.LogWarning($"TryUseItem failed: UseItem returned false, msg={message}");
            return false;
        }

        TryRemoveItem(itemId, 1);

        PlayerDataService.Instance.Save();
        OnInventoryChanged?.Invoke();
        Debug.Log($"TryUseItem success: {itemId}");
        return true;
    }

    public bool TryBindQuickSlot(int quickIndex, ItemId itemId, out string message)
    {
        message = string.Empty;
        List<QuickSlotData> quickSlots = PlayerDataService.Instance.data.quickSlots;

        if (quickIndex < 0 || quickIndex >= quickSlots.Count)
        {
            message = "Invalid quick slot";
            return false;
        }

        if (!HasItem(itemId, 1))
        {
            message = "Item is not in inventory";
            return false;
        }

        quickSlots[quickIndex].itemId = itemId.ToString();
        PlayerDataService.Instance.Save();
        OnQuickSlotsChanged?.Invoke();
        message = "Quick slot bound";
        return true;
    }

    public bool TryUseQuickSlot(int quickIndex, out string message)
    {
        message = string.Empty;
        List<QuickSlotData> quickSlots = PlayerDataService.Instance.data.quickSlots;

        if (quickIndex < 0 || quickIndex >= quickSlots.Count)
        {
            message = "Invalid quick slot";
            return false;
        }

        QuickSlotData quickSlot = quickSlots[quickIndex];
        if (quickSlot.IsEmpty)
        {
            message = "Quick slot is empty";
            return false;
        }

        if (!Enum.TryParse(quickSlot.itemId, out ItemId id))
        {
            quickSlot.itemId = null;
            message = "Invalid quick slot item";
            PlayerDataService.Instance.Save();
            OnQuickSlotsChanged?.Invoke();
            return false;
        }

        if (!HasItem(id, 1))
        {
            quickSlot.itemId = null;
            message = "Not enough item";
            PlayerDataService.Instance.Save();
            OnQuickSlotsChanged?.Invoke();
            return false;
        }

        return TryUseItem(id, out message);
    }

    private void ClearEmptyQuickSlots()
    {
        bool changed = false;
        foreach (QuickSlotData quickSlot in PlayerDataService.Instance.data.quickSlots)
        {
            if (quickSlot.IsEmpty || !Enum.TryParse(quickSlot.itemId, out ItemId id))
            {
                continue;
            }

            if (!HasItem(id, 1))
            {
                quickSlot.itemId = null;
                changed = true;
            }
        }

        if (changed)
        {
            PlayerDataService.Instance.Save();
            OnQuickSlotsChanged?.Invoke();
        }
    }
}
