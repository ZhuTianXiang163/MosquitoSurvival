using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text amountText;
    public GameObject emptyOverlay;
    public GameObject dragGhostPrefab;

    public InventorySlotData SlotData { get; private set; }

    private System.Action<InventorySlotView> onClicked;
    private System.Action<InventorySlotView> onBeginDrag;
    private System.Action<InventorySlotView> onEndDrag;
    private GameObject dragGhost;

    public void Setup(
        InventorySlotData data,
        System.Action<InventorySlotView> clicked,
        System.Action<InventorySlotView> beginDrag,
        System.Action<InventorySlotView> endDrag)
    {
        SlotData = data;
        onClicked = clicked;
        onBeginDrag = beginDrag;
        onEndDrag = endDrag;

        // Wire up Button.onClick as backup click handler
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log("Slot Button.onClick fired: " + gameObject.name + " empty=" + (SlotData == null || SlotData.IsEmpty));
                if (SlotData != null && !SlotData.IsEmpty)
                {
                    onClicked?.Invoke(this);
                }
            });
        }

        Refresh();
    }

    public void Refresh()
    {
        if (SlotData == null || SlotData.IsEmpty)
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

        if (iconImage != null)
        {
            iconImage.enabled = true;
            if (System.Enum.TryParse(SlotData.itemId, out ItemId id))
            {
                ItemDefinition def = ItemDatabase.Instance?.Get(id);
                if (def != null && def.icon != null)
                {
                    iconImage.sprite = def.icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    // Fallback: colored block per item type (keep default sprite)
                    iconImage.color = GetItemColor(id);
                }
            }
        }

        if (amountText != null)
        {
            amountText.text = SlotData.amount > 1 ? SlotData.amount.ToString() : string.Empty;
        }

        emptyOverlay?.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Slot OnPointerClick fired: " + gameObject.name + " empty=" + (SlotData == null || SlotData.IsEmpty));
        if (SlotData == null || SlotData.IsEmpty)
        {
            return;
        }

        onClicked?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (SlotData == null || SlotData.IsEmpty)
        {
            return;
        }

        if (dragGhostPrefab != null)
        {
            dragGhost = Instantiate(dragGhostPrefab, transform.root);
            CanvasGroup canvasGroup = dragGhost.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            Image ghostIcon = dragGhost.GetComponentInChildren<Image>();
            if (ghostIcon != null && iconImage != null)
            {
                ghostIcon.sprite = iconImage.sprite;
            }
        }

        onBeginDrag?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            dragGhost.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }

        onEndDrag?.Invoke(this);
    }

    /// <summary>
    /// Fallback color when no sprite icon is assigned for an item.
    /// </summary>
    public static Color GetItemColor(ItemId id)
    {
        switch (id)
        {
            case ItemId.Flower:         return new Color(1f, 0.4f, 0.6f);   // pink
            case ItemId.Grass:          return new Color(0.3f, 0.85f, 0.3f); // green
            case ItemId.FloralWater:    return new Color(0.4f, 0.7f, 1f);   // light blue
            case ItemId.HerbMedicine:   return new Color(0.95f, 0.85f, 0.2f); // yellow
            case ItemId.MosquitoSpray:  return new Color(0.6f, 0.4f, 0.9f); // purple
            case ItemId.LifePotion:     return new Color(1f, 0.2f, 0.3f);   // red
            default:                    return Color.gray;
        }
    }
}
