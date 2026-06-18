using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// Bridge: old InventoryManager API → new InventoryService/ShopService.
/// Keyboard shortcuts 1-4 still work but route through the new backpack system.
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Materials (deprecated — read from InventoryService)")]
    [SerializeField] private int flowers = 0;
    [SerializeField] private int grass = 0;

    [Header("Items (deprecated — read from InventoryService)")]
    [SerializeField] private int mosquitoRepellent = 0;
    [SerializeField] private int herbMedicine = 0;

    [Header("Crafting Cost")]
    [SerializeField] private int flowersNeededForRepellent = 2;
    [SerializeField] private int grassNeededForMedicine = 2;

    [Header("Item Effect")]
    [SerializeField] private float repellentDuration = 30f;
    [SerializeField] private int medicineHealAmount = 30;

    public int Flowers => InventoryService.Instance != null
        ? InventoryService.Instance.GetItemCount(ItemId.Flower)
                : flowers;
    public int Grass => Flowers; // both flowers and grass → Herb
    public int MosquitoRepellent => InventoryService.Instance != null
        ? InventoryService.Instance.GetItemCount(ItemId.FloralWater)
        : mosquitoRepellent;
    public int HerbMedicine => InventoryService.Instance != null
        ? InventoryService.Instance.GetItemCount(ItemId.HerbMedicine)
        : herbMedicine;
    public bool IsRepellentActive { get; private set; }

    public event Action<int> OnFlowersChanged;
    public event Action<int> OnGrassChanged;
    public event Action<int> OnMosquitoRepellentChanged;
    public event Action<int> OnHerbMedicineChanged;
    public event Action<bool> OnRepellentStateChanged;

    private float repellentTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (InventoryService.Instance != null)
            InventoryService.Instance.OnInventoryChanged += OnInventoryChangedHandler;
    }

    private void OnDestroy()
    {
        if (InventoryService.Instance != null)
            InventoryService.Instance.OnInventoryChanged -= OnInventoryChangedHandler;
    }

    private void OnInventoryChangedHandler()
    {
        OnFlowersChanged?.Invoke(Flowers);
        OnGrassChanged?.Invoke(Grass);
        OnMosquitoRepellentChanged?.Invoke(MosquitoRepellent);
        OnHerbMedicineChanged?.Invoke(HerbMedicine);
    }

    private void Update()
    {
        UpdateRepellentTimer();
    }

    private void UpdateRepellentTimer()
    {
        if (!IsRepellentActive) return;

        repellentTimer -= Time.deltaTime;
        if (repellentTimer <= 0f)
        {
            IsRepellentActive = false;
            repellentTimer = 0f;
            OnRepellentStateChanged?.Invoke(false);
            Debug.Log("花露水效果结束。");
        }
    }

    // === Bridge methods: route to InventoryService ===

    public void AddFlower(int amount)
    {
        if (InventoryService.Instance != null)
            InventoryService.Instance.TryAddItem(ItemId.Flower, amount, out _);
                    else
                        flowers += amount;
    }

    public void AddGrass(int amount)
    {
        AddFlower(amount); // grass → Herb in new system
    }

    public bool TrySpendFlowers(int amount)
    {
        if (InventoryService.Instance != null)
        {
            if (!InventoryService.Instance.HasItem(ItemId.Flower, amount)) return false;
                        InventoryService.Instance.TryRemoveItem(ItemId.Flower, amount);
            return true;
        }
        if (flowers < amount) return false;
        flowers -= amount;
        return true;
    }

    public bool TrySpendGrass(int amount)
    {
        return TrySpendFlowers(amount);
    }

    public void AddMosquitoRepellent(int amount)
    {
        if (InventoryService.Instance != null)
            InventoryService.Instance.TryAddItem(ItemId.FloralWater, amount, out _);
        else
            mosquitoRepellent += amount;
    }

    public void AddHerbMedicine(int amount)
    {
        if (InventoryService.Instance != null)
            InventoryService.Instance.TryAddItem(ItemId.HerbMedicine, amount, out _);
        else
            herbMedicine += amount;
    }

    public bool CraftRepellent()
    {
        if (ShopService.Instance != null)
        {
            string msg;
            bool ok = ShopService.Instance.TryCraftFloralWater(out msg);
            Debug.Log(ok ? "合成花露水成功。" : "合成花露水失败：" + msg);
            return ok;
        }

        // Fallback to old logic if ShopService not available
        if (!TrySpendFlowers(flowersNeededForRepellent))
        {
            Debug.Log("合成花露水失败，需要花：" + flowersNeededForRepellent);
            return false;
        }
        AddMosquitoRepellent(1);
        Debug.Log("合成花露水成功。");
        return true;
    }

    public bool CraftMedicine()
    {
        if (ShopService.Instance != null)
        {
            string msg;
            bool ok = ShopService.Instance.TryCraftHerbMedicine(out msg);
            Debug.Log(ok ? "合成草药成功。" : "合成草药失败：" + msg);
            return ok;
        }

        // Fallback to old logic if ShopService not available
        if (!TrySpendFlowers(grassNeededForMedicine))
        {
            Debug.Log("合成草药失败，需要草：" + grassNeededForMedicine);
            return false;
        }
        AddHerbMedicine(1);
        Debug.Log("合成草药成功。");
        return true;
    }

    public bool UseRepellent()
    {
        if (InventoryService.Instance != null)
        {
            string msg;
            bool ok = InventoryService.Instance.TryUseItem(ItemId.FloralWater, out msg);
            Debug.Log(ok ? "使用花露水成功。" : "使用花露水失败：" + msg);
            return ok;
        }

        if (mosquitoRepellent <= 0)
        {
            Debug.Log("没有花露水，无法使用。");
            return false;
        }
        mosquitoRepellent--;
        IsRepellentActive = true;
        repellentTimer = repellentDuration;
        OnMosquitoRepellentChanged?.Invoke(mosquitoRepellent);
        OnRepellentStateChanged?.Invoke(true);
        Debug.Log("使用花露水成功，接下来 " + repellentDuration + " 秒内免疫蚊子伤害。");
        return true;
    }

    public bool UseMedicine()
    {
        if (InventoryService.Instance != null)
        {
            string msg;
            bool ok = InventoryService.Instance.TryUseItem(ItemId.HerbMedicine, out msg);
            Debug.Log(ok ? "使用草药成功。" : "使用草药失败：" + msg);
            return ok;
        }

        if (herbMedicine <= 0)
        {
            Debug.Log("没有草药，无法使用。");
            return false;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager 不存在，无法回血。");
            return false;
        }
        herbMedicine--;
        GameManager.Instance.Heal(medicineHealAmount);
        OnHerbMedicineChanged?.Invoke(herbMedicine);
        Debug.Log("使用草药成功，回复血量：" + medicineHealAmount);
        return true;
    }
}
