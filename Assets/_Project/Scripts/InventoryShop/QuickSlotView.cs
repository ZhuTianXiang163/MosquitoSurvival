using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text amountText;
    public TMP_Text hotkeyText;
    public GameObject emptyOverlay;

    public int SlotIndex { get; private set; }
    public QuickSlotData Data { get; private set; }

    public void Setup(int index)
    {
        SlotIndex = index;
        Data = InventoryService.Instance.QuickSlots[index];
        Refresh();
    }

    public void Refresh()
    {
        if (Data == null || Data.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            if (amountText != null)
            {
                amountText.text = string.Empty;
            }

            emptyOverlay?.SetActive(true);
            return;
        }

        if (!System.Enum.TryParse(Data.itemId, out ItemId id))
        {
            Data.itemId = null;
            Refresh();
            return;
        }

        int count = InventoryService.Instance.GetItemCount(id);
        if (count <= 0)
        {
            Data.itemId = null;
            PlayerDataService.Instance.Save();
            Refresh();
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = true;
            ItemDefinition def = ItemDatabase.Instance?.Get(id);
            if (def != null && def.icon != null)
            {
                iconImage.sprite = def.icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = InventorySlotView.GetItemColor(id);
            }
        }

        if (amountText != null)
        {
            amountText.text = count.ToString();
        }

        emptyOverlay?.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Data == null || Data.IsEmpty)
        {
            return;
        }

        InventoryService.Instance.TryUseQuickSlot(SlotIndex, out string message);
        FindObjectOfType<ToastPopupController>()?.Show(message);
        Refresh();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotView slotView = eventData.pointerDrag?.GetComponent<InventorySlotView>();
        if (slotView == null || slotView.SlotData == null || slotView.SlotData.IsEmpty)
        {
            return;
        }

        if (!System.Enum.TryParse(slotView.SlotData.itemId, out ItemId id))
        {
            return;
        }

        InventoryService.Instance.TryBindQuickSlot(SlotIndex, id, out string message);
        FindObjectOfType<ToastPopupController>()?.Show(message);
        Refresh();
    }
}
