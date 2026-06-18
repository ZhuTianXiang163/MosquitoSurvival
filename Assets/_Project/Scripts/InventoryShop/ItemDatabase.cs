using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "InventoryShop/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();

    private Dictionary<ItemId, ItemDefinition> lookup;

    public static ItemDatabase Instance { get; private set; }

    private void OnEnable()
    {
        Instance = this;
        lookup = null;
    }

    private void BuildLookup()
    {
        if (lookup != null)
        {
            return;
        }

        lookup = new Dictionary<ItemId, ItemDefinition>();
        if (items == null)
        {
            return;
        }

        foreach (ItemDefinition def in items)
        {
            if (def != null && !lookup.ContainsKey(def.id))
            {
                lookup[def.id] = def;
            }
        }
    }

    public ItemDefinition Get(ItemId id)
    {
        BuildLookup();
        lookup.TryGetValue(id, out ItemDefinition def);
        return def;
    }

    public IReadOnlyList<ItemDefinition> GetAll()
    {
        return items;
    }

    public IReadOnlyList<ItemDefinition> GetByCategory(ItemCategory category)
    {
        return items == null
            ? new List<ItemDefinition>()
            : items.Where(item => item.category == category).ToList();
    }

    public static ItemDatabase CreateDefault()
    {
        ItemDatabase db = CreateInstance<ItemDatabase>();
        db.items = new List<ItemDefinition>
        {
            new ItemDefinition
            {
                id = ItemId.Flower,
                displayName = "Flower",
                category = ItemCategory.Material,
                description = "A flower. Sell for coins or craft into Floral Water.",
                obtainHint = "Collect from philodendron plants.",
                maxStack = 99,
                buyPrice = 20,
                sellPrice = 8,
                canBuy = false,
                canSell = true,
                canUse = false,
                isLimitedGoods = false
            },
            new ItemDefinition
            {
                id = ItemId.Grass,
                displayName = "Grass",
                category = ItemCategory.Material,
                description = "Medicinal grass. Sell for coins or craft into Herb Medicine.",
                obtainHint = "Collect from grass plants.",
                maxStack = 99,
                buyPrice = 15,
                sellPrice = 6,
                canBuy = false,
                canSell = true,
                canUse = false,
                isLimitedGoods = false
            },
            new ItemDefinition
            {
                id = ItemId.HerbMedicine,
                displayName = "Herb Medicine",
                category = ItemCategory.Consumable,
                description = "Restores 10 HP instantly.",
                obtainHint = "Craft with 2 grass, or buy in shop.",
                maxStack = 20,
                buyPrice = 60,
                sellPrice = 30,
                canBuy = true,
                canSell = true,
                canUse = true,
                isLimitedGoods = false
            },
            new ItemDefinition
            {
                id = ItemId.FloralWater,
                displayName = "Floral Water",
                category = ItemCategory.Consumable,
                description = "Grants mosquito immunity for 10 seconds.",
                obtainHint = "Craft with 2 flowers, or buy in shop.",
                maxStack = 20,
                buyPrice = 100,
                sellPrice = 50,
                canBuy = true,
                canSell = true,
                canUse = true,
                isLimitedGoods = false
            },
            new ItemDefinition
            {
                id = ItemId.MosquitoSpray,
                displayName = "Mosquito Spray",
                category = ItemCategory.Consumable,
                description = "Clears all mosquitoes in the scene.",
                obtainHint = "Limited shop item.",
                maxStack = 10,
                buyPrice = 180,
                sellPrice = -1,
                canBuy = false,
                canSell = false,
                canUse = true,
                isLimitedGoods = true
            },
            new ItemDefinition
            {
                id = ItemId.LifePotion,
                displayName = "Life Potion",
                category = ItemCategory.Consumable,
                description = "Restores health to full instantly.",
                obtainHint = "Craft with 3 flowers + 3 grass + 200 coins.",
                maxStack = 1,
                buyPrice = -1,
                sellPrice = -1,
                canBuy = false,
                canSell = false,
                canUse = true,
                isLimitedGoods = false
            }
        };

        // Load icon sprites from Resources
        LoadIcons(db);

        db.BuildLookup();
        return db;
    }

    private static void LoadIcons(ItemDatabase db)
    {
        // Map ItemId to sprite filename in Resources/ItemIcons/
        var iconMap = new Dictionary<ItemId, string>
        {
            { ItemId.Flower, "ItemIcons/flower" },
            { ItemId.Grass, "ItemIcons/grass" },
            { ItemId.HerbMedicine, "ItemIcons/herb" },
            { ItemId.FloralWater, "ItemIcons/flowerwater" },
            { ItemId.MosquitoSpray, "ItemIcons/spray" },
            { ItemId.LifePotion, "ItemIcons/life" },
        };

        foreach (ItemDefinition def in db.items)
        {
            if (def == null) continue;
            string path;
            if (iconMap.TryGetValue(def.id, out path))
            {
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    def.icon = sprite;
                }
                else
                {
                    Debug.LogWarning($"ItemDatabase: failed to load icon for {def.id} at {path}");
                }
            }
        }

        Debug.Log("ItemDatabase: icons loaded from Resources/ItemIcons/");
    }
}
