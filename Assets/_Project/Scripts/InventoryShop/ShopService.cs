using System;
using UnityEngine;

public enum ExpansionTier
{
    Small,
    Medium,
    Large
}

public class ShopService : MonoBehaviour
{
    public static ShopService Instance { get; private set; }

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private int limitedGoodsRefreshSeconds = 180;

    private static readonly (int slots, int cost, int maxCount)[] ExpansionConfig =
    {
        (4, 150, 3),
        (8, 400, 2),
        (16, 900, 1)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool CanBuy(ItemId itemId, out string reason)
    {
        reason = string.Empty;
        ItemDefinition def = itemDatabase.Get(itemId);

        if (def == null)
        {
            reason = "Invalid item";
            return false;
        }

        if (def.isLimitedGoods)
        {
            return CanBuyLimitedSpray(out reason);
        }

        if (!def.canBuy)
        {
            reason = "This item cannot be bought";
            return false;
        }

        if (GameManager.Instance.Coins < def.buyPrice)
        {
            reason = "Not enough coins";
            return false;
        }

        if (!InventoryService.Instance.CanAddItem(itemId, 1))
        {
            reason = "Inventory is full";
            return false;
        }

        return true;
    }

    public bool TryBuy(ItemId itemId, out string message)
    {
        message = string.Empty;

        if (!CanBuy(itemId, out message))
        {
            return false;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        int coinsBefore = GameManager.Instance.Coins;
        if (!GameManager.Instance.TrySpendCoins(def.buyPrice))
        {
            message = "Not enough coins";
            return false;
        }

        if (!InventoryService.Instance.TryAddItem(itemId, 1, out message))
        {
            GameManager.Instance.AddCoins(def.buyPrice);
            return false;
        }

        int coinsAfter = GameManager.Instance.Coins;
        message = $"Buy {itemId}: coins {coinsBefore}->{coinsAfter}";
        Debug.Log(message);

        SyncCoinsFromGameManager();
        return true;
    }

    public bool CanCraftFloralWater(out string reason)
    {
        reason = string.Empty;

        if (!InventoryService.Instance.HasItem(ItemId.Flower, 2))
        {
            reason = "Not enough flowers";
            return false;
        }

        if (!InventoryService.Instance.CanAddItem(ItemId.FloralWater, 1))
        {
            reason = "Inventory is full";
            return false;
        }

        return true;
    }

    public bool TryCraftFloralWater(out string message)
    {
        message = string.Empty;

        if (!CanCraftFloralWater(out message))
        {
            return false;
        }

        InventoryService.Instance.TryRemoveItem(ItemId.Flower, 2);
        InventoryService.Instance.TryAddItem(ItemId.FloralWater, 1, out _);
        message = "Craft successful";
        return true;
    }

    public bool CanCraftHerbMedicine(out string reason)
    {
        reason = string.Empty;

        if (!InventoryService.Instance.HasItem(ItemId.Grass, 2))
        {
            reason = "Not enough grass";
            return false;
        }

        if (!InventoryService.Instance.CanAddItem(ItemId.HerbMedicine, 1))
        {
            reason = "Inventory is full";
            return false;
        }

        return true;
    }

    public bool TryCraftHerbMedicine(out string message)
    {
        message = string.Empty;

        if (!CanCraftHerbMedicine(out message))
        {
            return false;
        }

        InventoryService.Instance.TryRemoveItem(ItemId.Grass, 2);
        InventoryService.Instance.TryAddItem(ItemId.HerbMedicine, 1, out _);
        message = "Craft successful";
        return true;
    }

    public bool CanCraftLifePotion(out string reason)
    {
        reason = string.Empty;

        if (!InventoryService.Instance.HasItem(ItemId.Flower, 3))
        {
            reason = "Need 3 flowers";
            return false;
        }

        if (!InventoryService.Instance.HasItem(ItemId.Grass, 3))
        {
            reason = "Need 3 grass";
            return false;
        }

        if (GameManager.Instance.Coins < 200)
        {
            reason = "Not enough coins (need 200)";
            return false;
        }

        if (!InventoryService.Instance.CanAddItem(ItemId.LifePotion, 1))
        {
            reason = "Inventory is full";
            return false;
        }

        return true;
    }

    public bool TryCraftLifePotion(out string message)
    {
        message = string.Empty;

        if (!CanCraftLifePotion(out message))
        {
            return false;
        }

        InventoryService.Instance.TryRemoveItem(ItemId.Flower, 3);
        InventoryService.Instance.TryRemoveItem(ItemId.Grass, 3);
        GameManager.Instance.TrySpendCoins(200);
        InventoryService.Instance.TryAddItem(ItemId.LifePotion, 1, out _);
        SyncCoinsFromGameManager();
        message = "Life Potion crafted!";
        return true;
    }

    public bool CanSell(ItemId itemId, int amount, out string reason)
    {
        reason = string.Empty;

        if (amount <= 0)
        {
            reason = "Nothing to sell";
            return false;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        if (def == null)
        {
            reason = "Invalid item";
            return false;
        }

        if (!def.canSell || def.sellPrice < 0)
        {
            reason = "This item cannot be sold";
            return false;
        }

        if (!InventoryService.Instance.HasItem(itemId, amount))
        {
            reason = "Nothing to sell";
            return false;
        }

        return true;
    }

    public bool TrySell(ItemId itemId, int amount, out string message)
    {
        message = string.Empty;

        if (!CanSell(itemId, amount, out message))
        {
            return false;
        }

        ItemDefinition def = itemDatabase.Get(itemId);
        if (!InventoryService.Instance.TryRemoveItem(itemId, amount))
        {
            message = "Remove item failed";
            return false;
        }

        GameManager.Instance.AddCoins(def.sellPrice * amount);
        SyncCoinsFromGameManager();
        message = "Sell successful";
        return true;
    }

    public bool CanExpandBackpack(ExpansionTier tier, out string reason)
    {
        reason = string.Empty;
        (int slots, int cost, int maxCount) = ExpansionConfig[(int)tier];
        InventorySaveData data = PlayerDataService.Instance.data;

        int currentCount = 0;
        switch (tier)
        {
            case ExpansionTier.Small:
                currentCount = data.smallExpansionCount;
                break;
            case ExpansionTier.Medium:
                currentCount = data.mediumExpansionCount;
                break;
            case ExpansionTier.Large:
                currentCount = data.largeExpansionCount;
                break;
        }

        if (currentCount >= maxCount)
        {
            reason = "Expansion tier limit reached";
            return false;
        }

        if (GameManager.Instance.Coins < cost)
        {
            reason = "Not enough coins";
            return false;
        }

        return true;
    }

    public bool TryExpandBackpack(ExpansionTier tier, out string message)
    {
        message = string.Empty;

        if (!CanExpandBackpack(tier, out message))
        {
            return false;
        }

        (int slots, int cost, int maxCount) = ExpansionConfig[(int)tier];
        if (!GameManager.Instance.TrySpendCoins(cost))
        {
            message = "Not enough coins";
            return false;
        }

        InventorySaveData data = PlayerDataService.Instance.data;
        data.capacity += slots;

        switch (tier)
        {
            case ExpansionTier.Small:
                data.smallExpansionCount++;
                break;
            case ExpansionTier.Medium:
                data.mediumExpansionCount++;
                break;
            case ExpansionTier.Large:
                data.largeExpansionCount++;
                break;
        }

        PlayerDataService.Instance.EnsureSlotsCapacity();
        SyncCoinsFromGameManager();
        PlayerDataService.Instance.Save();
        message = "Backpack expanded";
        return true;
    }

    public void RefreshLimitedGoodsIfNeeded()
    {
        InventorySaveData data = PlayerDataService.Instance.data;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (data.limitedSprayRefreshUnixSeconds == 0)
        {
            data.limitedSprayRefreshUnixSeconds = now + limitedGoodsRefreshSeconds;
            data.limitedSprayRemaining = 1;
            PlayerDataService.Instance.Save();
            return;
        }

        if (now >= data.limitedSprayRefreshUnixSeconds)
        {
            data.limitedSprayRemaining = 1;
            data.limitedSprayRefreshUnixSeconds = now + limitedGoodsRefreshSeconds;
            PlayerDataService.Instance.Save();
        }
    }

    public bool CanBuyLimitedSpray(out string reason)
    {
        reason = string.Empty;
        RefreshLimitedGoodsIfNeeded();

        if (PlayerDataService.Instance.data.limitedSprayRemaining <= 0)
        {
            reason = "Limited item sold out";
            return false;
        }

        if (GameManager.Instance.Coins < 180)
        {
            reason = "Not enough coins";
            return false;
        }

        if (!InventoryService.Instance.CanAddItem(ItemId.MosquitoSpray, 1))
        {
            reason = "Inventory is full";
            return false;
        }

        return true;
    }

    public bool TryBuyLimitedSpray(out string message)
    {
        message = string.Empty;

        if (!CanBuyLimitedSpray(out message))
        {
            return false;
        }

        if (!GameManager.Instance.TrySpendCoins(180))
        {
            message = "Not enough coins";
            return false;
        }

        if (!InventoryService.Instance.TryAddItem(ItemId.MosquitoSpray, 1, out message))
        {
            GameManager.Instance.AddCoins(180);
            return false;
        }

        PlayerDataService.Instance.data.limitedSprayRemaining = 0;
        SyncCoinsFromGameManager();
        PlayerDataService.Instance.Save();
        message = "Purchase successful";
        return true;
    }

    public long GetLimitedSprayRemainingSeconds()
    {
        RefreshLimitedGoodsIfNeeded();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, PlayerDataService.Instance.data.limitedSprayRefreshUnixSeconds - now);
    }

    public void SyncCoinsFromGameManager()
    {
        if (PlayerDataService.Instance == null || GameManager.Instance == null)
        {
            return;
        }

        PlayerDataService.Instance.data.coins = GameManager.Instance.Coins;
        PlayerDataService.Instance.Save();
    }
}
