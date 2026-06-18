using System.IO;
using UnityEngine;

public class PlayerDataService : MonoBehaviour
{
    public static PlayerDataService Instance { get; private set; }

    public InventorySaveData data;

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If parented, detach for DontDestroyOnLoad to work
        if (transform.parent != null)
            transform.SetParent(null);

        savePath = Path.Combine(Application.persistentDataPath, "inventory_shop_save.json");
        
        // Always start with fresh data (no persistence during development)
        data = new InventorySaveData();
        Debug.Log("PlayerDataService: fresh inventory created");
        
        EnsureSlotsCapacity();
        EnsureQuickSlotsCapacity();
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PlayerDataService: save failed: {e.Message}");
        }
    }

    public void ResetSave()
    {
        data = new InventorySaveData();
        EnsureSlotsCapacity();
        EnsureQuickSlotsCapacity();
        Save();
        Debug.Log("PlayerDataService: save reset to initial state");
    }

    private InventorySaveData Load()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                InventorySaveData loaded = JsonUtility.FromJson<InventorySaveData>(json);
                if (loaded != null)
                {
                    Debug.Log("PlayerDataService: save loaded");
                    return loaded;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlayerDataService: load failed, creating new save: {e.Message}");
        }

        Debug.Log("PlayerDataService: new save created");
        return new InventorySaveData();
    }

    public void EnsureSlotsCapacity()
    {
        if (data.slots == null)
        {
            data.slots = new System.Collections.Generic.List<InventorySlotData>();
        }

        while (data.slots.Count < data.capacity)
        {
            data.slots.Add(new InventorySlotData());
        }

        while (data.slots.Count > data.capacity)
        {
            data.slots.RemoveAt(data.slots.Count - 1);
        }
    }

    public void EnsureQuickSlotsCapacity()
    {
        if (data.quickSlots == null)
        {
            data.quickSlots = new System.Collections.Generic.List<QuickSlotData>();
        }

        while (data.quickSlots.Count < 6)
        {
            data.quickSlots.Add(new QuickSlotData());
        }
    }
}
