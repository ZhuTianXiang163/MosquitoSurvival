using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPopupController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupRoot;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text obtainHintText;
    public TMP_Text countText;
    public TMP_Text sellPriceText;

    [Header("Buttons")]
    public Button useButton;
    public Button sellOneButton;
    public Button closeButton;

    private InventorySlotData currentSlot;

    private void Start()
    {
        useButton?.onClick.AddListener(OnUse);
        sellOneButton?.onClick.AddListener(OnSellOne);
        closeButton?.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(InventorySlotData slotData, bool showSell = true)
    {
        if (slotData == null || slotData.IsEmpty)
        {
            Hide();
            return;
        }

        currentSlot = slotData;
        popupRoot?.SetActive(true);

        // Ensure popup is on top of everything
        if (popupRoot != null)
        {
            popupRoot.transform.SetAsLastSibling();
        }

        if (!System.Enum.TryParse(currentSlot.itemId, out ItemId id))
        {
            Hide();
            return;
        }

        ItemDefinition def = ItemDatabase.Instance?.Get(id);
        if (def == null)
        {
            Hide();
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = true;
            if (def.icon != null)
            {
                iconImage.sprite = def.icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = InventorySlotView.GetItemColor(id);
            }
        }

        if (nameText != null)
        {
            nameText.text = def.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = def.description;
        }

        if (obtainHintText != null)
        {
            obtainHintText.text = string.IsNullOrEmpty(def.obtainHint) ? string.Empty : $"Source: {def.obtainHint}";
        }

        if (countText != null)
        {
            countText.text = $"Count: {InventoryService.Instance.GetItemCount(id)}";
        }

        if (sellPriceText != null)
        {
            bool canSell = def.canSell && def.sellPrice > 0;
            sellPriceText.gameObject.SetActive(canSell);
            sellPriceText.text = canSell ? $"Sell: {def.sellPrice} coins each" : string.Empty;
        }

        useButton?.gameObject.SetActive(def.canUse);
        sellOneButton?.gameObject.SetActive(showSell && def.canSell && def.sellPrice > 0);
    }

    public void Hide()
    {
        popupRoot?.SetActive(false);
        currentSlot = null;
    }

    private void OnUse()
    {
        if (currentSlot == null || !System.Enum.TryParse(currentSlot.itemId, out ItemId id))
        {
            return;
        }

        InventoryService.Instance.TryUseItem(id, out string message);
        FindObjectOfType<ToastPopupController>()?.Show(message);

        if (InventoryService.Instance.HasItem(id, 1))
        {
            Show(currentSlot, sellOneButton != null && sellOneButton.gameObject.activeSelf);
        }
        else
        {
            Hide();
        }
    }

    private void OnSellOne()
    {
        if (currentSlot == null || !System.Enum.TryParse(currentSlot.itemId, out ItemId id))
        {
            return;
        }

        ShopService.Instance.TrySell(id, 1, out string message);
        FindObjectOfType<ToastPopupController>()?.Show(message);
        Hide();
    }
}
