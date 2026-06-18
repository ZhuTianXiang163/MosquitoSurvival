using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastPopupController : MonoBehaviour
{
    public static ToastPopupController Instance { get; private set; }

    [Header("UI References")]
    public GameObject popupRoot;
    public TMP_Text messageText;
    public Button actionButton;
    public TMP_Text actionButtonText;

    [Header("Settings")]
    public float autoHideDelay = 2f;
    public float cooldown = 0.5f; // Prevent rapid repeated shows

    private System.Action onActionClicked;
    private Coroutine hideCoroutine;
    private float lastShowTime = -999f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        popupRoot?.SetActive(false);
        actionButton?.onClick.AddListener(OnActionClicked);
    }

    public void Show(string message, string actionLabel = null, System.Action onAction = null)
    {
        // Prevent showing too frequently
        if (Time.time - lastShowTime < cooldown)
        {
            return;
        }
        lastShowTime = Time.time;

        if (messageText != null)
            messageText.text = message;

        onActionClicked = onAction;

        if (actionButton != null)
        {
            bool hasAction = !string.IsNullOrEmpty(actionLabel) && onAction != null;
            actionButton.gameObject.SetActive(hasAction);
            if (hasAction && actionButtonText != null)
                actionButtonText.text = actionLabel;
        }

        popupRoot?.SetActive(true);

        // Ensure toast is always on top of everything
        if (popupRoot != null)
        {
            popupRoot.transform.SetAsLastSibling();
        }

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(AutoHide());
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideDelay);
        Hide();
    }

    private void OnActionClicked()
    {
        onActionClicked?.Invoke();
        Hide();
    }

    public void Hide()
    {
        popupRoot?.SetActive(false);
    }
}
