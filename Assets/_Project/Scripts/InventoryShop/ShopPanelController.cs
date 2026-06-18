using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;

    [Header("Page Navigation")]
    public GameObject page1Root;
    public GameObject page2Root;
    public Button tabCraftBuyButton;
    public Button tabUpgradeButton;
    private int currentPage = 0;

    [Header("Gold & Materials")]
    public TMP_Text goldText;
    public TMP_Text flowerCountText;
    public TMP_Text grassCountText;

    [Header("Page 1 - Craft")]
    public Button craftButton;
    public TMP_Text craftStatusText;
    public Image craftIcon;
    public Button herbMedicineCraftButton;
    public TMP_Text herbMedicineCraftStatusText;
    public Image herbMedicineCraftIcon;
    public Button lifePotionCraftButton;
    public TMP_Text lifePotionCraftStatusText;
    public Image lifePotionCraftIcon;

    [Header("Page 1 - Buy")]
    public Button herbMedicineBuyButton;
    public TMP_Text herbMedicineBuyPriceText;
    public TMP_Text herbMedicineBuyStatusText;
    public Image herbMedicineBuyIcon;
    public Button floralWaterBuyButton;
    public TMP_Text floralWaterBuyPriceText;
    public TMP_Text floralWaterBuyStatusText;
    public Image floralWaterBuyIcon;
    public Button sprayBuyButton;
    public TMP_Text sprayBuyPriceText;
    public TMP_Text sprayBuyStatusText;
    public TMP_Text sprayCountdownText;
    public Image sprayBuyIcon;

    [Header("Page 2 - Expansion")]
    public Button expandSmallButton;
    public TMP_Text expandSmallText;
    public Image expandSmallIcon;
    public Button expandMediumButton;
    public TMP_Text expandMediumText;
    public Image expandMediumIcon;
    public Button expandLargeButton;
    public TMP_Text expandLargeText;
    public Image expandLargeIcon;

    [Header("Status Message")]
    public TMP_Text statusMessageText;
    public GameObject statusMessageArea;
    private float statusMessageTimer = 0f;

    [Header("Click Protection")]
    private float lastClickTime = -999f;
    private float clickCooldown = 0.3f;

    [Header("Navigation")]
    public Button closeButton;

    [Header("Popups")]
    public ToastPopupController toastPopup;

    private void Start()
    {
        // Page navigation
        tabCraftBuyButton?.onClick.AddListener(() => SwitchPage(0));
        tabUpgradeButton?.onClick.AddListener(() => SwitchPage(1));

        // Page 1: Craft
        craftButton?.onClick.AddListener(OnCraft);
        herbMedicineCraftButton?.onClick.AddListener(OnCraftHerbMedicine);
        lifePotionCraftButton?.onClick.AddListener(OnCraftLifePotion);

        // Page 1: Buy
        herbMedicineBuyButton?.onClick.AddListener(OnBuyHerbMedicine);
        floralWaterBuyButton?.onClick.AddListener(OnBuyFloralWater);
        sprayBuyButton?.onClick.AddListener(OnBuySpray);

        // Page 2: Expansion
        expandSmallButton?.onClick.AddListener(() => OnExpand(ExpansionTier.Small));
        expandMediumButton?.onClick.AddListener(() => OnExpand(ExpansionTier.Medium));
        expandLargeButton?.onClick.AddListener(() => OnExpand(ExpansionTier.Large));

        closeButton?.onClick.AddListener(Hide);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged += _ => Refresh();
        }

        // Default to page 1
        SwitchPage(0);
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        if (statusMessageTimer > 0)
        {
            statusMessageTimer -= Time.deltaTime;
            if (statusMessageTimer <= 0)
            {
                if (statusMessageArea != null)
                {
                    statusMessageArea.SetActive(false);
                }
            }
        }
    }

    private void SwitchPage(int page)
    {
        currentPage = page;
        if (page1Root != null) page1Root.SetActive(page == 0);
        if (page2Root != null) page2Root.SetActive(page == 1);

        // Highlight active tab
        if (tabCraftBuyButton != null)
        {
            ColorBlock cb = tabCraftBuyButton.colors;
            cb.normalColor = page == 0 ? new Color(0.3f, 0.6f, 1f) : Color.white;
            tabCraftBuyButton.colors = cb;
        }
        if (tabUpgradeButton != null)
        {
            ColorBlock cb = tabUpgradeButton.colors;
            cb.normalColor = page == 1 ? new Color(0.3f, 0.6f, 1f) : Color.white;
            tabUpgradeButton.colors = cb;
        }
    }

    public void ShowStatusMessage(string message)
    {
        if (statusMessageText != null && statusMessageArea != null)
        {
            statusMessageText.text = message;
            statusMessageArea.SetActive(true);
            statusMessageTimer = 2f;
        }
    }

    public void Refresh()
    {
        if (ShopService.Instance == null || InventoryService.Instance == null || GameManager.Instance == null)
        {
            return;
        }

        RefreshGoldAndMaterials();
        RefreshIcons();
        RefreshCraft();
        RefreshBuyRows();
        RefreshExpansion();
    }

    private void RefreshIcons()
    {
        // Apply sprites from ItemDatabase to shop card icons
        if (ItemDatabase.Instance == null) return;

        SetIconSprite(craftIcon, ItemId.FloralWater);
        SetIconSprite(herbMedicineCraftIcon, ItemId.HerbMedicine);
        SetIconSprite(lifePotionCraftIcon, ItemId.LifePotion);
        SetIconSprite(herbMedicineBuyIcon, ItemId.HerbMedicine);
        SetIconSprite(floralWaterBuyIcon, ItemId.FloralWater);
        SetIconSprite(sprayBuyIcon, ItemId.MosquitoSpray);
    }

    private void SetIconSprite(Image img, ItemId id)
    {
        if (img == null) return;
        ItemDefinition def = ItemDatabase.Instance.Get(id);
        if (def != null && def.icon != null)
        {
            img.sprite = def.icon;
            img.color = Color.white;
        }
        else
        {
            img.color = InventorySlotView.GetItemColor(id);
        }
    }

    private void RefreshGoldAndMaterials()
    {
        if (goldText != null)
        {
            goldText.text = $"Coins: {GameManager.Instance.Coins}";
        }

        if (flowerCountText != null)
        {
            int flowerCount = InventoryService.Instance.GetItemCount(ItemId.Flower);
            flowerCountText.text = $"Flowers: {flowerCount}";
        }

        if (grassCountText != null)
        {
            int grassCount = InventoryService.Instance.GetItemCount(ItemId.Grass);
            grassCountText.text = $"Grass: {grassCount}";
        }
    }

    private void RefreshCraft()
    {
        // Floral Water
        bool canCraftFloral = ShopService.Instance.CanCraftFloralWater(out string floralReason);
        if (craftButton != null) craftButton.interactable = canCraftFloral;
        if (craftStatusText != null)
        {
            craftStatusText.text = canCraftFloral
                ? $"Flowers: {InventoryService.Instance.GetItemCount(ItemId.Flower)}/2"
                : floralReason;
        }

        // Herb Medicine
        bool canCraftMedicine = ShopService.Instance.CanCraftHerbMedicine(out string medicineReason);
        if (herbMedicineCraftButton != null) herbMedicineCraftButton.interactable = canCraftMedicine;
        if (herbMedicineCraftStatusText != null)
        {
            herbMedicineCraftStatusText.text = canCraftMedicine
                ? $"Grass: {InventoryService.Instance.GetItemCount(ItemId.Grass)}/2"
                : medicineReason;
        }

        // Life Potion
        bool canCraftLife = ShopService.Instance.CanCraftLifePotion(out string lifeReason);
        if (lifePotionCraftButton != null) lifePotionCraftButton.interactable = canCraftLife;
        if (lifePotionCraftStatusText != null)
        {
            int f = InventoryService.Instance.GetItemCount(ItemId.Flower);
            int g = InventoryService.Instance.GetItemCount(ItemId.Grass);
            lifePotionCraftStatusText.text = canCraftLife
                ? $"F:{f}/3 G:{g}/3 Coins:{GameManager.Instance.Coins}/200"
                : lifeReason;
        }
    }

    private void RefreshBuyRows()
    {
        // Buy Herb Medicine
        ItemDefinition herbMedDef = ItemDatabase.Instance.Get(ItemId.HerbMedicine);
        if (herbMedicineBuyPriceText != null)
        {
            herbMedicineBuyPriceText.text = $"{herbMedDef.buyPrice} coins";
        }
        bool canBuyHerbMed = ShopService.Instance.CanBuy(ItemId.HerbMedicine, out string herbMedReason);
        if (herbMedicineBuyButton != null) herbMedicineBuyButton.interactable = canBuyHerbMed;
        if (herbMedicineBuyStatusText != null) herbMedicineBuyStatusText.text = canBuyHerbMed ? "" : herbMedReason;

        // Buy Floral Water
        ItemDefinition floralDef = ItemDatabase.Instance.Get(ItemId.FloralWater);
        if (floralWaterBuyPriceText != null)
        {
            floralWaterBuyPriceText.text = $"{floralDef.buyPrice} coins";
        }
        bool canBuyFloral = ShopService.Instance.CanBuy(ItemId.FloralWater, out string floralReason);
        if (floralWaterBuyButton != null) floralWaterBuyButton.interactable = canBuyFloral;
        if (floralWaterBuyStatusText != null) floralWaterBuyStatusText.text = canBuyFloral ? "" : floralReason;

        // Buy Spray
        ItemDefinition spray = ItemDatabase.Instance.Get(ItemId.MosquitoSpray);
        if (sprayBuyPriceText != null)
        {
            sprayBuyPriceText.text = $"{spray.buyPrice} coins";
        }
        bool canBuySpray = ShopService.Instance.CanBuyLimitedSpray(out string sprayReason);
        if (sprayBuyButton != null) sprayBuyButton.interactable = canBuySpray;
        if (sprayBuyStatusText != null) sprayBuyStatusText.text = canBuySpray ? "" : sprayReason;
        if (sprayCountdownText != null)
        {
            long remaining = ShopService.Instance.GetLimitedSprayRemainingSeconds();
            sprayCountdownText.text = remaining > 0 ? $"Refresh: {remaining}s" : "Available";
        }
    }

    private void RefreshExpansion()
    {
        RefreshExpandButton(expandSmallButton, expandSmallText, ExpansionTier.Small);
        RefreshExpandButton(expandMediumButton, expandMediumText, ExpansionTier.Medium);
        RefreshExpandButton(expandLargeButton, expandLargeText, ExpansionTier.Large);
    }

    private void RefreshExpandButton(Button button, TMP_Text label, ExpansionTier tier)
    {
        bool canExpand = ShopService.Instance.CanExpandBackpack(tier, out string reason);
        if (button != null) button.interactable = canExpand;
        if (label != null) label.text = canExpand ? "" : reason;
    }



    // === ACTION HANDLERS ===

    private bool CanClick()
    {
        if (Time.time - lastClickTime < clickCooldown)
        {
            return false;
        }
        lastClickTime = Time.time;
        return true;
    }

    private void OnCraft()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryCraftFloralWater(out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Craft FloralWater success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Craft FloralWater failed: {message}");
        }
        Refresh();
    }

    private void OnCraftHerbMedicine()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryCraftHerbMedicine(out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Craft HerbMedicine success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Craft HerbMedicine failed: {message}");
        }
        Refresh();
    }

    private void OnCraftLifePotion()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryCraftLifePotion(out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Craft LifePotion success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Craft LifePotion failed: {message}");
        }
        Refresh();
    }

    private void OnBuyHerbMedicine()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryBuy(ItemId.HerbMedicine, out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Buy HerbMedicine success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Buy HerbMedicine failed: {message}");
        }
        Refresh();
    }

    private void OnBuyFloralWater()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryBuy(ItemId.FloralWater, out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Buy FloralWater success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Buy FloralWater failed: {message}");
        }
        Refresh();
    }

    private void OnBuySpray()
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryBuyLimitedSpray(out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Buy Spray success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Buy Spray failed: {message}");
        }
        Refresh();
    }

    private void OnExpand(ExpansionTier tier)
    {
        if (!CanClick() || ShopService.Instance == null) return;
        
        bool success = ShopService.Instance.TryExpandBackpack(tier, out string message);
        if (success) 
        {
            toastPopup?.Show(message);
            Debug.Log($"[Shop] Expand {tier} success: {message}");
        }
        else 
        {
            ShowStatusMessage(message);
            Debug.Log($"[Shop] Expand {tier} failed: {message}");
        }
        Refresh();
    }



    public static void Show()
    {
        ShopPanelController panel = FindObjectOfType<ShopPanelController>();
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
