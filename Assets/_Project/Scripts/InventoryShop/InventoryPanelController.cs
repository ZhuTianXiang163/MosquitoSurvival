using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;

    [Header("Quick Bar")]
    public QuickSlotView[] quickSlots;

    [Header("Capacity Text")]
    public TMP_Text capacityText;

    [Header("Category Tabs")]
    public Button tabAll;
    public Button tabMaterial;
    public Button tabConsumable;
    public Button tabTool;

    [Header("Slots Grid")]
    public Transform slotGridParent;
    public InventorySlotView slotPrefab;

    [Header("Buttons")]
    public Button sortButton;
    public Button toShopButton;
    public Button closeButton;

    [Header("Popups")]
    public ItemDetailPopupController detailPopup;
    public ToastPopupController toastPopup;

    private readonly List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private ItemCategory? currentFilter;

    private void Start()
    {
        tabAll?.onClick.AddListener(() => SetFilter(null));
        tabMaterial?.onClick.AddListener(() => SetFilter(ItemCategory.Material));
        tabConsumable?.onClick.AddListener(() => SetFilter(ItemCategory.Consumable));
        tabTool?.onClick.AddListener(() => SetFilter(ItemCategory.Tool));

        sortButton?.onClick.AddListener(OnSort);
        toShopButton?.onClick.AddListener(ShopPanelController.Show);
        closeButton?.onClick.AddListener(Hide);

        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.OnInventoryChanged += Refresh;
            InventoryService.Instance.OnQuickSlotsChanged += RefreshQuickSlots;
        }

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (InventoryService.Instance == null)
        {
            return;
        }

        RefreshCapacity();
        RefreshGrid();
        RefreshQuickSlots();
        RefreshCategoryTabHighlights();
    }

    private void SetFilter(ItemCategory? category)
    {
        currentFilter = category;
        RefreshGrid();
        RefreshCategoryTabHighlights();
    }

    private void RefreshCategoryTabHighlights()
    {
        SetTab(tabAll, currentFilter == null);
        SetTab(tabMaterial, currentFilter == ItemCategory.Material);
        SetTab(tabConsumable, currentFilter == ItemCategory.Consumable);
        SetTab(tabTool, currentFilter == ItemCategory.Tool);
    }

    private void SetTab(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? new Color(0.3f, 0.6f, 1f) : Color.white;
        button.colors = colors;
    }

    private void RefreshCapacity()
    {
        if (capacityText != null)
        {
            capacityText.text = $"{InventoryService.Instance.UsedSlotCount} / {InventoryService.Instance.Capacity}";
        }
    }

    private void RefreshGrid()
    {
        if (slotGridParent == null || slotPrefab == null || InventoryService.Instance == null)
        {
            return;
        }

        foreach (InventorySlotView view in slotViews)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }
        slotViews.Clear();

        IReadOnlyList<InventorySlotData> slots = currentFilter == null
            ? InventoryService.Instance.Slots
            : InventoryService.Instance.GetSlotsByCategory(currentFilter.Value);

        foreach (InventorySlotData slotData in slots)
        {
            InventorySlotView view = Instantiate(slotPrefab, slotGridParent);
            view.gameObject.SetActive(true);
            view.Setup(slotData, OnSlotClicked, null, null);
            slotViews.Add(view);
        }
    }

    private void RefreshQuickSlots()
    {
        if (quickSlots == null || InventoryService.Instance == null)
        {
            return;
        }

        for (int i = 0; i < quickSlots.Length && i < InventoryService.Instance.QuickSlots.Count; i++)
        {
            quickSlots[i].Setup(i);
        }
    }

    private void OnSlotClicked(InventorySlotView view)
    {
        Debug.Log($"[Inventory] Slot clicked: {view.SlotData.itemId} x{view.SlotData.amount}, detailPopup={detailPopup != null}");
        if (detailPopup != null)
        {
            detailPopup.Show(view.SlotData, true);
        }
        else
        {
            Debug.LogError("[Inventory] detailPopup is null! Cannot show item details.");
        }
    }

    private void OnSort()
    {
        InventoryService.Instance.SortInventory();
        toastPopup?.Show("Inventory sorted");
    }

    public static void Show()
    {
        InventoryPanelController panel = FindObjectOfType<InventoryPanelController>();
        if (panel != null)
        {
            panel.panelRoot?.SetActive(true);
        }
    }

    public void Hide()
    {
        panelRoot?.SetActive(false);
    }
}
