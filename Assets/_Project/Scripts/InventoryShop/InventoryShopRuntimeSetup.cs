using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class InventoryShopRuntimeSetup : MonoBehaviour
{
    [Header("Canvas Settings")]
    public float canvasDistance = 2.0f;
    public float canvasHeightOffset = 0.1f;
    public Vector3 canvasScale = new Vector3(0.002f, 0.002f, 0.002f);
    public float canvasWidth = 1600f;
    public float canvasHeight = 1000f;

    [Header("Font")]
    public TMP_FontAsset fontAsset;

    private ItemDatabase itemDatabase;
    private GameObject canvasObject;
    private GameObject quickBarCanvasObject;
    private InventoryPanelController inventoryPanel;
    private ShopPanelController shopPanel;
    private ToastPopupController toastPopup;
    private ItemDetailPopupController detailPopup;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightHandInteractor;

    // Manual key tracking (XR simulator may consume per-frame events)
    private bool iDown;
    private bool gDown;

    private void Awake()
    {
        if (fontAsset == null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        itemDatabase = ItemDatabase.CreateDefault();
        CreateServices();
        CreateCanvas();
        BuildUI();

        // === 测试用：初始化金币和道具 ===
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCoins(500);
            ShopService.Instance.SyncCoinsFromGameManager();
            GameManager.Instance.OnCoinsChanged += _ => ShopService.Instance.SyncCoinsFromGameManager();
        }
        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.TryAddItem(ItemId.Flower, 3, out _);
            InventoryService.Instance.TryAddItem(ItemId.Grass, 3, out _);
            InventoryService.Instance.TryAddItem(ItemId.HerbMedicine, 1, out _);
            InventoryService.Instance.TryAddItem(ItemId.FloralWater, 1, out _);

            InventoryService.Instance.TryAddItem(ItemId.MosquitoSpray, 1, out _);
            InventoryService.Instance.TryAddItem(ItemId.LifePotion, 1, out _);
            Debug.Log("[Test] Added 500 coins + all items to inventory");
        }

        Debug.Log("InventoryShopRuntimeSetup: setup completed");

        // Start with only quick bar visible, panels hidden
        if (inventoryPanel != null) inventoryPanel.panelRoot.SetActive(false);
        if (shopPanel != null) shopPanel.panelRoot.SetActive(false);
    }

    private void Start()
    {
        // Show mouse cursor for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        // Manual edge detection (XR simulator may consume wasPressedThisFrame)
        bool iPressed = Keyboard.current.iKey.isPressed && !iDown;
        iDown = Keyboard.current.iKey.isPressed;
        bool gPressed = Keyboard.current.gKey.isPressed && !gDown;
        gDown = Keyboard.current.gKey.isPressed;

        // Reset save (debug key)
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (PlayerDataService.Instance != null)
            {
                PlayerDataService.Instance.ResetSave();
                if (inventoryPanel != null) inventoryPanel.Refresh();
                if (shopPanel != null) shopPanel.Refresh();
                toastPopup?.Show("Inventory reset!");
            }
        }



        if (iPressed)
        {
            Debug.Log("I KEY PRESSED");

            // Close shop if open
            if (shopPanel != null && shopPanel.panelRoot.activeSelf)
            {
                shopPanel.panelRoot.SetActive(false);
                Debug.Log("Shop closed by I key");
            }

            if (inventoryPanel != null)
            {
                bool active = !inventoryPanel.panelRoot.activeSelf;
                if (active)
                {
                    PositionCanvasInFrontOfCamera();
                }
                inventoryPanel.panelRoot.SetActive(active);
                Debug.Log("Inventory " + (active ? "opened" : "closed"));
            }
        }

        if (gPressed)
        {
            Debug.Log("G KEY PRESSED");
            // If shop is closed and ray hits mesh28812893, open shop
            if (shopPanel != null && !shopPanel.panelRoot.activeSelf && IsRayHitting("mesh28812893"))
            {
                // Close inventory if open
                if (inventoryPanel != null && inventoryPanel.panelRoot.activeSelf)
                {
                    inventoryPanel.panelRoot.SetActive(false);
                }

                PositionCanvasInFrontOfCamera();
                shopPanel.panelRoot.SetActive(true);
                Debug.Log("Shop opened via mesh28812893 ray hit");
            }
            else
            {
                SimulateUIClick();
            }
        }


    }

    /// <summary>
    /// Check if the XR ray interactor is currently hitting a GameObject whose name
    /// contains the given substring (case-insensitive). Checks both UI raycast and 3D physics.
    /// </summary>
    private bool IsRayHitting(string partialName)
    {
        EnsureInteractor();
        if (rightHandInteractor == null) return false;

        string lower = partialName.ToLowerInvariant();

        // Check UI raycast first
        if (rightHandInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiResult, out int uiIdx))
        {
            if (uiResult.gameObject != null &&
                uiResult.gameObject.name.ToLowerInvariant().Contains(lower))
            {
                return true;
            }
        }

        // Check 3D physics raycast for non-UI objects
        Transform t = rightHandInteractor.transform;
        if (Physics.Raycast(t.position, t.forward, out RaycastHit hit, 50f))
        {
            if (hit.collider != null &&
                hit.collider.gameObject.name.ToLowerInvariant().Contains(lower))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureInteractor()
    {
        if (rightHandInteractor == null)
        {
            rightHandInteractor = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        }
    }

    private void SimulateUIClick()
    {
        EnsureInteractor();

        if (rightHandInteractor == null)
        {
            Debug.LogWarning("No XRRayInteractor found in scene");
            return;
        }

        // Method 1: Try XR UI raycast result (works when XRUIInputModule is active)
        if (rightHandInteractor.TryGetCurrentUIRaycastResult(out RaycastResult result, out int idx))
        {
            GameObject hitObj = result.gameObject;
            if (hitObj != null && TryClickUI(hitObj))
            {
                return;
            }
        }

        // Method 2: EventSystem.RaycastAll fallback (works with TrackedDeviceGraphicRaycaster)
        if (canvasObject != null && EventSystem.current != null)
        {
            Transform t = rightHandInteractor.transform;
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = GetScreenPointFromRay(t.position, t.forward);

            System.Collections.Generic.List<RaycastResult> results =
                new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject != null && TryClickUI(results[i].gameObject))
                {
                    return;
                }
            }
        }

        Debug.Log("XR ray not hitting any UI");
    }

    /// <summary>
    /// Try to click a UI element. Searches the object and its parents for
    /// Button (onClick) or IPointerClickHandler (OnPointerClick).
    /// Returns true if a click was handled.
    /// </summary>
    private bool TryClickUI(GameObject hitObj)
    {
        if (hitObj == null) return false;

        // Search up the hierarchy for Button or IPointerClickHandler
        Transform current = hitObj.transform;
        while (current != null)
        {
            // Check for Button component
            Button button = current.GetComponent<Button>();
            if (button != null && button.interactable && button.gameObject.activeInHierarchy)
            {
                button.onClick.Invoke();
                Debug.Log("G click on Button: " + button.gameObject.name);
                return true;
            }

            // Check for IPointerClickHandler (e.g. InventorySlotView)
            IPointerClickHandler clickHandler = current.GetComponent<IPointerClickHandler>();
            if (clickHandler != null && current.gameObject.activeInHierarchy)
            {
                PointerEventData pe = new PointerEventData(EventSystem.current);
                clickHandler.OnPointerClick(pe);
                Debug.Log("G click on PointerClickHandler: " + current.gameObject.name);
                return true;
            }

            current = current.parent;
        }

        // Last resort: ExecuteEvents on the hit object itself
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(hitObj, pointerData, ExecuteEvents.pointerClickHandler);
            Debug.Log("G click (ExecuteEvents fallback) on: " + hitObj.name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert a world-space ray to an approximate screen position for GraphicRaycaster.
    /// </summary>
    private Vector2 GetScreenPointFromRay(Vector3 origin, Vector3 direction)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return Vector2.zero;

        // Find where the ray hits the canvas plane
        if (canvasObject != null)
        {
            Plane canvasPlane = new Plane(canvasObject.transform.forward, canvasObject.transform.position);
            if (canvasPlane.Raycast(new Ray(origin, direction), out float enter))
            {
                Vector3 hitPoint = origin + direction * enter;
                return cam.WorldToScreenPoint(hitPoint);
            }
        }

        return cam.WorldToScreenPoint(origin + direction * canvasDistance);
    }

    private void CreateServices()
    {
        GameObject root = new GameObject("InventoryShopServices");
        root.transform.SetParent(transform, false);

        root.AddComponent<PlayerDataService>();

        InventoryService inventoryService = root.AddComponent<InventoryService>();
        SetField(inventoryService, "itemDatabase", itemDatabase);

        ShopService shopService = root.AddComponent<ShopService>();
        SetField(shopService, "itemDatabase", itemDatabase);

        root.AddComponent<ItemUseService>();
        root.AddComponent<MosquitoImmunityController>();
        root.AddComponent<MosquitoClearUtility>();
    }

    private void CreateCanvas()
    {
        canvasObject = new GameObject("ShopInventoryCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        // Don't set position in Awake — positioned dynamically when shown
        canvasObject.transform.localScale = canvasScale;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(canvasWidth, canvasHeight);
        scaler.dynamicPixelsPerUnit = 100f;

        // Add BOTH raycasters:
        // - GraphicRaycaster: processes PointerEventData (mouse clicks)
        // - TrackedDeviceGraphicRaycaster: processes TrackedDeviceEventData (XR controller rays)
        // They handle different event types so they don't conflict.
        GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = false;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        canvasObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

        // Add MouseUIHighlight for hover effects
        canvasObject.AddComponent<MouseUIHighlight>();

        // Ensure EventSystem has XRUIInputModule
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            es = eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        // Remove StandaloneInputModule — uses old UnityEngine.Input, incompatible with new Input System
        UnityEngine.EventSystems.StandaloneInputModule standalone =
            es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            Destroy(standalone);
            Debug.Log("Removed StandaloneInputModule");
        }

        // Add XRUIInputModule — handles BOTH mouse and XR controller rays
        var xrInputModule = es.GetComponent<XRUIInputModule>();
        if (xrInputModule == null)
        {
            xrInputModule = es.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("Added XRUIInputModule to EventSystem");
        }

        // Explicitly enable both mouse and XR input on XRUIInputModule
        xrInputModule.enableMouseInput = true;
        xrInputModule.enableXRInput = true;

        Debug.Log("ShopInventoryCanvas created (mouse + XR controller)");
    }

    /// <summary>
    /// Reposition the main canvas to face the player, at eye level, canvasDistance meters ahead.
    /// </summary>
    private void PositionCanvasInFrontOfCamera()
    {
        if (canvasObject == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        Transform camTransform = cam.transform;
        Vector3 pos = camTransform.position + camTransform.forward * canvasDistance;
        pos.y += canvasHeightOffset;
        canvasObject.transform.position = pos;
        canvasObject.transform.rotation = camTransform.rotation;
    }

    private void CreateQuickBarCanvas()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogError("QuickBarCanvas: no camera found");
            return;
        }

        quickBarCanvasObject = new GameObject("QuickBarCanvas", typeof(RectTransform));
        quickBarCanvasObject.transform.SetParent(cam.transform, false);
        quickBarCanvasObject.transform.localPosition = new Vector3(0f, -0.35f, 0.8f);
        quickBarCanvasObject.transform.localRotation = Quaternion.identity;
        quickBarCanvasObject.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);

        RectTransform rect = quickBarCanvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 100f);

        Canvas canvas = quickBarCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        quickBarCanvasObject.AddComponent<GraphicRaycaster>();
        Debug.Log("QuickBarCanvas created, parent=" + cam.name);
    }

    private void BuildUI()
    {
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        // Detail popup MUST be created BEFORE inventory panel (inventory references it)
        detailPopup = BuildDetailPopup(canvasRect);

        // Build main panels
        inventoryPanel = BuildInventoryPanel(canvasRect, null);
        shopPanel = BuildShopPanel(canvasRect);

        // Toast popup (on top of panels)
        toastPopup = BuildToast(canvasRect);

        // Ensure correct render order (last sibling = on top)
        if (inventoryPanel != null && inventoryPanel.panelRoot != null)
            inventoryPanel.panelRoot.transform.SetSiblingIndex(0);
        if (shopPanel != null && shopPanel.panelRoot != null)
            shopPanel.panelRoot.transform.SetSiblingIndex(1);
        if (detailPopup != null && detailPopup.popupRoot != null)
            detailPopup.popupRoot.transform.SetAsLastSibling();
        if (toastPopup != null && toastPopup.popupRoot != null)
            toastPopup.popupRoot.transform.SetAsLastSibling();
    }

    private ToastPopupController BuildToast(RectTransform parent)
    {
        GameObject panel = MakePanel("ToastPopup", parent, UIBeautify.PopupBg);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.08f);
        rect.anchorMax = new Vector2(0.5f, 0.08f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 70f);

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(20, 20, 8, 8);
        layout.spacing = 12;

        TMP_Text message = MakeText("Message", panel.transform, "", 24, TextAlignmentOptions.Center);
        Button actionButton = MakeButton("ActionButton", panel.transform, "", 20);
        TMP_Text actionText = actionButton.GetComponentInChildren<TMP_Text>();

        ToastPopupController controller = panel.AddComponent<ToastPopupController>();
        controller.popupRoot = panel;
        controller.messageText = message;
        controller.actionButton = actionButton;
        controller.actionButtonText = actionText;
        panel.SetActive(false);
        return controller;
    }

    private ItemDetailPopupController BuildDetailPopup(RectTransform parent)
    {
        GameObject panel = MakePanel("ItemDetailPopup", parent, UIBeautify.PopupBg);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 520f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 12;

        Image icon = MakeImage("Icon", panel.transform, Color.gray);
        icon.rectTransform.sizeDelta = new Vector2(100f, 100f);
        icon.preserveAspect = true;

        TMP_Text name = MakeText("Name", panel.transform, "Item", 30, TextAlignmentOptions.Center);
        TMP_Text desc = MakeText("Description", panel.transform, "", 18, TextAlignmentOptions.Left);
        TMP_Text source = MakeText("Source", panel.transform, "", 16, TextAlignmentOptions.Left);
        source.color = UIBeautify.TextDim;
        TMP_Text count = MakeText("Count", panel.transform, "", 18, TextAlignmentOptions.Center);
        TMP_Text sell = MakeText("SellPrice", panel.transform, "", 18, TextAlignmentOptions.Center);
        sell.color = UIBeautify.TextGold;

        GameObject row = new GameObject("Buttons", typeof(RectTransform));
        row.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 16;

        Button use = MakeButton("UseButton", row.transform, "Use", 20);
        Button sellOne = MakeButton("SellButton", row.transform, "Sell", 20, gold: true);
        Button close = MakeCloseButton("CloseButton", row.transform);

        ItemDetailPopupController controller = panel.AddComponent<ItemDetailPopupController>();
        controller.popupRoot = panel;
        controller.iconImage = icon;
        controller.nameText = name;
        controller.descriptionText = desc;
        controller.obtainHintText = source;
        controller.countText = count;
        controller.sellPriceText = sell;
        controller.useButton = use;
        controller.sellOneButton = sellOne;
        controller.closeButton = close;
        panel.SetActive(false);
        return controller;
    }

    private GameObject BuildQuickBar(RectTransform parent)
    {
        GameObject bar = MakePanel("QuickBar", parent, UIBeautify.QuickBarBg);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 86f);
        rect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 10;

        for (int i = 0; i < 6; i++)
        {
            GameObject slot = MakeSlot($"QuickSlot_{i}", bar.transform, i, UIBeautify.QuickSlotBg);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(76f, 76f);
            LayoutElement slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 76f;
            slotLayout.preferredHeight = 76f;

            Image icon = MakeImage("Icon", slot.transform, Color.gray);
            icon.rectTransform.sizeDelta = new Vector2(48f, 48f);
            TMP_Text amount = MakeAnchoredText("Count", slot.transform, "", 14, TextAlignmentOptions.BottomRight, new Vector2(1, 0), new Vector2(-4, 4));
            TMP_Text hotkey = MakeAnchoredText("Hotkey", slot.transform, (i + 1).ToString(), 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(4, -4));
            hotkey.color = UIBeautify.TextDim;
            GameObject overlay = new GameObject("EmptyOverlay", typeof(RectTransform));
            overlay.transform.SetParent(slot.transform, false);
            UIBeautify.SetRounded(overlay, UIBeautify.SlotSprite, new Color(0f, 0f, 0f, 0.25f));
            overlay.GetComponent<Image>().raycastTarget = false;
            Stretch(overlay.GetComponent<RectTransform>());

            QuickSlotView view = slot.AddComponent<QuickSlotView>();
            view.iconImage = icon;
            view.amountText = amount;
            view.hotkeyText = hotkey;
            view.emptyOverlay = overlay;
        }

        return bar;
    }

    private InventoryPanelController BuildInventoryPanel(RectTransform parent, GameObject quickBar)
    {
        GameObject panel = MakePanel("InventoryPanel", parent, UIBeautify.PanelBg);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.20f);
        rect.anchorMax = new Vector2(0.25f, 0.20f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, 740f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 10;

        // Title bar
        GameObject titleBar = MakePanel("TitleBar", panel.transform, UIBeautify.InvTitle);
        titleBar.GetComponent<RectTransform>().sizeDelta = new Vector2(680f, 44f);
        LayoutElement titleLE = titleBar.AddComponent<LayoutElement>();
        titleLE.minHeight = 44f;
        TMP_Text title = MakeText("TitleText", titleBar.transform, "INVENTORY", 26, TextAlignmentOptions.Center);
        Stretch(title.GetComponent<RectTransform>());

        TMP_Text capacity = MakeText("Capacity", panel.transform, "0 / 12", 16, TextAlignmentOptions.Center);
        capacity.color = UIBeautify.TextDim;

        GameObject tabs = new GameObject("Tabs", typeof(RectTransform));
        tabs.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup tabLayout = tabs.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.spacing = 8;

        Button tabAll = MakeButton("TabAll", tabs.transform, "All", 18);
        Button tabMat = MakeButton("TabMaterial", tabs.transform, "Material", 18);
        Button tabCon = MakeButton("TabConsumable", tabs.transform, "Consumable", 18);
        Button tabTool = MakeButton("TabTool", tabs.transform, "Tool", 18);

        GameObject grid = new GameObject("SlotGrid", typeof(RectTransform));
        grid.transform.SetParent(panel.transform, false);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(650f, 390f);
        LayoutElement gridLayoutElement = grid.AddComponent<LayoutElement>();
        gridLayoutElement.preferredWidth = 650f;
        gridLayoutElement.preferredHeight = 390f;

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80f, 80f);
        gridLayout.spacing = new Vector2(8f, 8f);
        gridLayout.padding = new RectOffset(5, 5, 5, 5);

        InventorySlotView slotPrefab = BuildSlotPrefab();

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup buttonLayout = buttons.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.spacing = 16;

        Button sort = MakeButton("SortButton", buttons.transform, "Sort", 20);
        Button close = MakeCloseButton("CloseButton", buttons.transform);

        InventoryPanelController controller = panel.AddComponent<InventoryPanelController>();
        controller.panelRoot = panel;
        controller.quickSlots = quickBar != null ? quickBar.GetComponentsInChildren<QuickSlotView>() : new QuickSlotView[0];
        controller.capacityText = capacity;
        controller.tabAll = tabAll;
        controller.tabMaterial = tabMat;
        controller.tabConsumable = tabCon;
        controller.tabTool = tabTool;
        controller.slotGridParent = grid.transform;
        controller.slotPrefab = slotPrefab;
        controller.sortButton = sort;
        controller.closeButton = close;
        controller.detailPopup = detailPopup;
        controller.toastPopup = toastPopup;

        return controller;
    }

    private ShopPanelController BuildShopPanel(RectTransform parent)
    {
        // Panel root
        GameObject panel = MakePanel("ShopPanel", parent, UIBeautify.PanelBg);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.75f, 0.15f);
        rect.anchorMax = new Vector2(0.75f, 0.15f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, 820f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 8;

        // === TITLE BAR ===
        GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform));
        titleBar.transform.SetParent(panel.transform, false);
        LayoutElement titleLE = titleBar.AddComponent<LayoutElement>();
        titleLE.minHeight = 44f;
        HorizontalLayoutGroup titleLayout = titleBar.AddComponent<HorizontalLayoutGroup>();
        titleLayout.childAlignment = TextAnchor.MiddleCenter;
        titleLayout.spacing = 8;

        MakeText("Title", titleBar.transform, "SHOP", 28, TextAlignmentOptions.Center);
        Button close = MakeCloseButton("Close", titleBar.transform, 32);

        // === INFO BAR (coins + materials) ===
        GameObject infoBar = new GameObject("InfoBar", typeof(RectTransform));
        infoBar.transform.SetParent(panel.transform, false);
        UIBeautify.SetRounded(infoBar, UIBeautify.SlotSprite, new Color(0.08f, 0.09f, 0.14f, 0.9f));
        LayoutElement infoLE = infoBar.AddComponent<LayoutElement>();
        infoLE.minHeight = 36f;
        HorizontalLayoutGroup infoLayout = infoBar.AddComponent<HorizontalLayoutGroup>();
        infoLayout.childAlignment = TextAnchor.MiddleCenter;
        infoLayout.padding = new RectOffset(16, 16, 4, 4);
        infoLayout.spacing = 20;

        TMP_Text coinsText = MakeText("Coins", infoBar.transform, "Coins: 0", 18, TextAlignmentOptions.Left);
        coinsText.color = UIBeautify.TextGold;
        TMP_Text flowerCountText = MakeText("FlowerCount", infoBar.transform, "Flowers: 0", 16, TextAlignmentOptions.Left);
        flowerCountText.color = new Color(1f, 0.4f, 0.6f);
        TMP_Text grassCountText = MakeText("GrassCount", infoBar.transform, "Grass: 0", 16, TextAlignmentOptions.Left);
        grassCountText.color = new Color(0.3f, 0.85f, 0.3f);

        // === PAGE TABS ===
        GameObject tabBar = new GameObject("TabBar", typeof(RectTransform));
        tabBar.transform.SetParent(panel.transform, false);
        LayoutElement tabLE = tabBar.AddComponent<LayoutElement>();
        tabLE.minHeight = 40f;
        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.spacing = 8;

        Button tabCraftBuy = MakeButton("TabCraftBuy", tabBar.transform, "Craft & Buy", 16);
        Button tabUpgrade = MakeButton("TabUpgrade", tabBar.transform, "Upgrades", 16);

        // === PAGE 1: CRAFT + BUY ===
        GameObject page1 = new GameObject("Page1", typeof(RectTransform));
        page1.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup page1Layout = page1.AddComponent<VerticalLayoutGroup>();
        page1Layout.childAlignment = TextAnchor.UpperCenter;
        page1Layout.padding = new RectOffset(0, 0, 4, 4);
        page1Layout.spacing = 6;
        LayoutElement page1LE = page1.AddComponent<LayoutElement>();
        page1LE.flexibleHeight = 1;

        BuildSectionHeader(page1.transform, "Crafting");

        ShopCard floralCard = BuildShopCard(page1.transform, "FloralWater",
            new Color(0.4f, 0.7f, 1f), "Floral Water",
            "Mosquito immunity 10s",
            "Flowers x2", "Craft");

        ShopCard medicineCard = BuildShopCard(page1.transform, "HerbMedicine",
            new Color(0.95f, 0.85f, 0.2f), "Herb Medicine",
            "Restore 10 HP",
            "Grass x2", "Craft");

        ShopCard lifePotionCard = BuildShopCard(page1.transform, "LifePotion",
            new Color(1f, 0.2f, 0.3f), "Life Potion",
            "Full revive on death",
            "Flower x3 + Grass x3 + 200", "Craft");

        BuildSectionHeader(page1.transform, "Buy");

        ShopCard herbMedicineCard = BuildShopCard(page1.transform, "BuyHerbMedicine",
            new Color(0.95f, 0.85f, 0.2f), "Herb Medicine",
            "Restore 10 HP",
            "60 coins", "Buy");

        ShopCard floralWaterCard = BuildShopCard(page1.transform, "BuyFloralWater",
            new Color(0.4f, 0.7f, 1f), "Floral Water",
            "Mosquito immunity 10s",
            "100 coins", "Buy");

        ShopCard sprayCard = BuildShopCard(page1.transform, "BuySpray",
            new Color(0.6f, 0.4f, 0.9f), "Mosquito Spray",
            "Clear all mosquitoes",
            "180 coins", "Buy");
        TMP_Text sprayCountdown = MakeText("SprayCountdown", page1.transform, "", 13, TextAlignmentOptions.Center);
        sprayCountdown.color = UIBeautify.TextDim;

        // === PAGE 2: UPGRADES ===
        GameObject page2 = new GameObject("Page2", typeof(RectTransform));
        page2.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup page2Layout = page2.AddComponent<VerticalLayoutGroup>();
        page2Layout.childAlignment = TextAnchor.UpperCenter;
        page2Layout.padding = new RectOffset(0, 0, 4, 4);
        page2Layout.spacing = 6;
        LayoutElement page2LE = page2.AddComponent<LayoutElement>();
        page2LE.flexibleHeight = 1;
        page2.SetActive(false);

        BuildSectionHeader(page2.transform, "Backpack Expansion");

        ShopCard smallCard = BuildShopCard(page2.transform, "ExpandSmall",
            new Color(0.5f, 0.8f, 0.5f), "Small Pack",
            "+4 inventory slots",
            "150 coins", "Upgrade", iconSize: 36f);
        ShopCard medCard = BuildShopCard(page2.transform, "ExpandMedium",
            new Color(0.5f, 0.7f, 1f), "Medium Pack",
            "+8 inventory slots",
            "400 coins", "Upgrade", iconSize: 50f);
        ShopCard largeCard = BuildShopCard(page2.transform, "ExpandLarge",
            new Color(1f, 0.8f, 0.3f), "Large Pack",
            "+16 inventory slots",
            "900 coins", "Upgrade", iconSize: 64f);

        // === STATUS MESSAGE AREA ===
        GameObject statusArea = new GameObject("StatusArea", typeof(RectTransform));
        statusArea.transform.SetParent(panel.transform, false);
        UIBeautify.SetRounded(statusArea, UIBeautify.SlotSprite, new Color(0.15f, 0.05f, 0.05f, 0.8f));
        LayoutElement statusLE = statusArea.AddComponent<LayoutElement>();
        statusLE.minHeight = 36f;
        statusLE.preferredHeight = 36f;

        TMP_Text statusMessage = MakeText("StatusMessage", statusArea.transform, "", 16, TextAlignmentOptions.Center);
        statusMessage.color = UIBeautify.TextWarn;
        Stretch(statusMessage.GetComponent<RectTransform>());
        statusArea.SetActive(false);

        // === BUILD CONTROLLER ===
        ShopPanelController controller = panel.AddComponent<ShopPanelController>();
        controller.panelRoot = panel;
        controller.goldText = coinsText;
        controller.flowerCountText = flowerCountText;
        controller.grassCountText = grassCountText;

        // Page navigation
        controller.page1Root = page1;
        controller.page2Root = page2;
        controller.tabCraftBuyButton = tabCraftBuy;
        controller.tabUpgradeButton = tabUpgrade;

        // Page 1: Crafting
        controller.craftButton = floralCard.button;
        controller.craftStatusText = floralCard.statusText;
        controller.craftIcon = floralCard.icon;
        controller.herbMedicineCraftButton = medicineCard.button;
        controller.herbMedicineCraftStatusText = medicineCard.statusText;
        controller.herbMedicineCraftIcon = medicineCard.icon;
        controller.lifePotionCraftButton = lifePotionCard.button;
        controller.lifePotionCraftStatusText = lifePotionCard.statusText;
        controller.lifePotionCraftIcon = lifePotionCard.icon;

        // Page 1: Buy
        controller.herbMedicineBuyButton = herbMedicineCard.button;
        controller.herbMedicineBuyPriceText = herbMedicineCard.costText;
        controller.herbMedicineBuyStatusText = herbMedicineCard.statusText;
        controller.herbMedicineBuyIcon = herbMedicineCard.icon;
        controller.floralWaterBuyButton = floralWaterCard.button;
        controller.floralWaterBuyPriceText = floralWaterCard.costText;
        controller.floralWaterBuyStatusText = floralWaterCard.statusText;
        controller.floralWaterBuyIcon = floralWaterCard.icon;
        controller.sprayBuyButton = sprayCard.button;
        controller.sprayBuyPriceText = sprayCard.costText;
        controller.sprayBuyStatusText = sprayCard.statusText;
        controller.sprayCountdownText = sprayCountdown;
        controller.sprayBuyIcon = sprayCard.icon;

        // Page 2: Upgrades
        controller.expandSmallButton = smallCard.button;
        controller.expandSmallText = smallCard.statusText;
        controller.expandSmallIcon = smallCard.icon;
        controller.expandMediumButton = medCard.button;
        controller.expandMediumText = medCard.statusText;
        controller.expandMediumIcon = medCard.icon;
        controller.expandLargeButton = largeCard.button;
        controller.expandLargeText = largeCard.statusText;
        controller.expandLargeIcon = largeCard.icon;

        // Load backpack sprite for expansion cards
        Sprite bagSprite = Resources.Load<Sprite>("ItemIcons/bag");
        if (bagSprite != null)
        {
            smallCard.icon.sprite = bagSprite;
            smallCard.icon.color = Color.white;
            medCard.icon.sprite = bagSprite;
            medCard.icon.color = Color.white;
            largeCard.icon.sprite = bagSprite;
            largeCard.icon.color = Color.white;
            Debug.Log("Backpack icon loaded for expansion cards");
        }
        else
        {
            Debug.LogWarning("Failed to load backpack icon from Resources/ItemIcons/bag");
        }

        // Status
        controller.statusMessageText = statusMessage;
        controller.statusMessageArea = statusArea;
        controller.closeButton = close;
        controller.toastPopup = toastPopup;

        return controller;
    }

    private int _slotPrefabIndex;

    private InventorySlotView BuildSlotPrefab()
    {
        int idx = _slotPrefabIndex++;
        GameObject slot = MakeSlot("InventorySlotPrefab", transform, idx, UIBeautify.SlotBg);
        slot.SetActive(false);
        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80f, 80f);

        Image icon = MakeImage("Icon", slot.transform, Color.gray);
        icon.rectTransform.sizeDelta = new Vector2(48f, 48f);
        icon.preserveAspect = true;
        icon.raycastTarget = false; // CRITICAL: let clicks pass through to slot

        TMP_Text amount = MakeAnchoredText("Amount", slot.transform, "", 14, TextAlignmentOptions.BottomRight, new Vector2(1, 0), new Vector2(-4, 4));
        amount.raycastTarget = false; // CRITICAL: let clicks pass through to slot

        GameObject overlay = new GameObject("EmptyOverlay", typeof(RectTransform));
        overlay.transform.SetParent(slot.transform, false);
        UIBeautify.SetRounded(overlay, UIBeautify.SlotSprite, new Color(0f, 0f, 0f, 0.25f));
        overlay.GetComponent<Image>().raycastTarget = false;
        Stretch(overlay.GetComponent<RectTransform>());

        // Add Button component as reliable click handler
        Button slotButton = slot.AddComponent<Button>();
        ColorBlock cb = slotButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.8f, 0.9f, 1f);
        cb.pressedColor = new Color(0.6f, 0.7f, 0.9f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        slotButton.colors = cb;

        InventorySlotView view = slot.AddComponent<InventorySlotView>();
        view.iconImage = icon;
        view.amountText = amount;
        view.emptyOverlay = overlay;
        return view;
    }

    private Row BuildRow(Transform parent, string name, string buttonLabel, string infoText)
    {
        GameObject row = new GameObject($"Row_{name}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 10;

        Button button = MakeButton($"Button_{name}", row.transform, buttonLabel, 18, gold: true);
        TMP_Text info = MakeText($"Info_{name}", row.transform, infoText, 16, TextAlignmentOptions.Left);
        TMP_Text status = MakeText($"Status_{name}", row.transform, "", 14, TextAlignmentOptions.Left);
        status.color = UIBeautify.TextWarn;
        return new Row(button, info, status);
    }

    private struct ShopCard
    {
        public Button button;
        public TMP_Text costText;
        public TMP_Text statusText;
        public Image icon;
    }

    private ShopCard BuildShopCard(Transform parent, string name, Color iconColor,
        string itemName, string description, string costLabel, string buttonText,
        float iconSize = 50f)
    {
        // Card root
        GameObject card = new GameObject($"Card_{name}", typeof(RectTransform));
        card.transform.SetParent(parent, false);
        UIBeautify.SetRounded(card, UIBeautify.SlotSprite, new Color(0.10f, 0.11f, 0.16f, 0.95f));
        LayoutElement cardLE = card.AddComponent<LayoutElement>();
        cardLE.minHeight = 80f;
        cardLE.preferredHeight = 80f;

        HorizontalLayoutGroup cardLayout = card.AddComponent<HorizontalLayoutGroup>();
        cardLayout.childAlignment = TextAnchor.MiddleLeft;
        cardLayout.padding = new RectOffset(12, 12, 8, 8);
        cardLayout.spacing = 12;

        // Icon (color block)
        Image icon = MakeImage("Icon", card.transform, iconColor);
        icon.preserveAspect = true;
        LayoutElement iconLE = icon.gameObject.AddComponent<LayoutElement>();
        iconLE.minWidth = iconSize;
        iconLE.minHeight = iconSize;
        iconLE.preferredWidth = iconSize;
        iconLE.preferredHeight = iconSize;

        // Info column
        GameObject infoCol = new GameObject("Info", typeof(RectTransform));
        infoCol.transform.SetParent(card.transform, false);
        VerticalLayoutGroup infoLayout = infoCol.AddComponent<VerticalLayoutGroup>();
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.spacing = 2;
        infoLayout.padding = new RectOffset(0, 0, 0, 0);
        LayoutElement infoLE = infoCol.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1;

        MakeText("Name", infoCol.transform, itemName, 18, TextAlignmentOptions.Left);
        TMP_Text descText = MakeText("Desc", infoCol.transform, description, 13, TextAlignmentOptions.Left);
        descText.color = UIBeautify.TextDim;
        TMP_Text costText = MakeText("Cost", infoCol.transform, costLabel, 14, TextAlignmentOptions.Left);
        costText.color = UIBeautify.TextGold;

        // Button + status column
        GameObject btnCol = new GameObject("BtnCol", typeof(RectTransform));
        btnCol.transform.SetParent(card.transform, false);
        VerticalLayoutGroup btnLayout = btnCol.AddComponent<VerticalLayoutGroup>();
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.spacing = 4;
        LayoutElement btnColLE = btnCol.AddComponent<LayoutElement>();
        btnColLE.minWidth = 120f;

        Button button = MakeButton("Button", btnCol.transform, buttonText, 16, gold: true);
        TMP_Text statusText = MakeText("Status", btnCol.transform, "", 12, TextAlignmentOptions.Center);
        statusText.color = UIBeautify.TextWarn;

        ShopCard result = new ShopCard();
        result.button = button;
        result.costText = costText;
        result.statusText = statusText;
        result.icon = icon;
        return result;
    }

    private void BuildSectionHeader(Transform parent, string title)
    {
        GameObject header = new GameObject($"Section_{title}", typeof(RectTransform));
        header.transform.SetParent(parent, false);
        LayoutElement le = header.AddComponent<LayoutElement>();
        le.minHeight = 28f;
        le.preferredHeight = 28f;

        HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(8, 8, 4, 0);

        TMP_Text text = MakeText("Title", header.transform, title, 16, TextAlignmentOptions.Left);
        text.color = new Color(0.6f, 0.7f, 0.9f, 1f);
    }


    private GameObject MakePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Sprite panelSprite = RetroInventoryAssets.I.PanelBg;
        if (panelSprite != null)
        {
            Image img = obj.AddComponent<Image>();
            img.sprite = panelSprite;
            img.color = color;
            img.type = Image.Type.Sliced;
        }
        else
        {
            // Fallback to generated rounded rect
            UIBeautify.SetRounded(obj, UIBeautify.PanelSprite, color);
        }
        return obj;
    }

    private GameObject MakeSlot(string name, Transform parent, int slotIndex, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Sprite[] slots = RetroInventoryAssets.I.Slots;
        Sprite slotSprite = (slots != null && slots.Length > 0)
            ? slots[slotIndex % slots.Length]
            : null;

        if (slotSprite != null)
        {
            Image img = obj.AddComponent<Image>();
            img.sprite = slotSprite;
            img.color = color;
        }
        else
        {
            UIBeautify.SetRounded(obj, UIBeautify.SlotSprite, color);
        }
        return obj;
    }

    private TMP_Text MakeText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = UIBeautify.TextPrimary;
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }

        UIBeautify.AddShadow(obj, new Color(0, 0, 0, 0.5f), new Vector2(1, -1));

        ContentSizeFitter fitter = obj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return tmp;
    }

    private TMP_Text MakeAnchoredText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 anchor, Vector2 position)
    {
        TMP_Text tmp = MakeText(name, parent, text, size, alignment);
        Destroy(tmp.GetComponent<ContentSizeFitter>());
        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(70f, 24f);
        return tmp;
    }

    private Button MakeButton(string name, Transform parent, string label, int size, bool gold = false)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        UIBeautify.SetRounded(obj, UIBeautify.ButtonSprite, gold ? UIBeautify.BtnGold : UIBeautify.BtnNormal);

        Button button = obj.AddComponent<Button>();
        if (gold)
            UIBeautify.StyleButton(button, UIBeautify.BtnGold, UIBeautify.BtnGoldHover, UIBeautify.BtnPressed);
        else
            UIBeautify.StyleButton(button, UIBeautify.BtnNormal, UIBeautify.BtnHover, UIBeautify.BtnPressed);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = 110f;
        layout.minHeight = 42f;

        TMP_Text text = MakeText("Text", obj.transform, label, size, TextAlignmentOptions.Center);
        Destroy(text.GetComponent<ContentSizeFitter>());
        Stretch(text.GetComponent<RectTransform>());

        return button;
    }

    /// <summary>
    /// Create a small close button using the retro cross sprite.
    /// </summary>
    private Button MakeCloseButton(string name, Transform parent, int size = 32)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Sprite cross = RetroInventoryAssets.I.CloseIcon;
        if (cross != null)
        {
            Image img = obj.AddComponent<Image>();
            img.sprite = cross;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            UIBeautify.SetRounded(obj, UIBeautify.ButtonSprite, new Color(0.8f, 0.2f, 0.2f, 1f));
        }

        Button button = obj.AddComponent<Button>();
        UIBeautify.StyleButton(button, new Color(0.8f, 0.25f, 0.25f, 1f),
            new Color(1f, 0.35f, 0.35f, 1f), new Color(0.6f, 0.15f, 0.15f, 1f));

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        return button;
    }

    private Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private readonly struct Row
    {
        public readonly Button button;
        public readonly TMP_Text info;
        public readonly TMP_Text status;

        public Row(Button button, TMP_Text info, TMP_Text status)
        {
            this.button = button;
            this.info = info;
            this.status = status;
        }
    }
}
