using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUDUI : MonoBehaviour
{
    [Header("Coin UI")]
    [SerializeField] private TMP_Text coinText;

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Status UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float statusShowTime = 2f;

    private Coroutine statusCoroutine;
    private bool wasRevengeActive = false;
    private int lastHealth;
    private int lastCoins;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameHUDUI: GameManager.Instance is null. HUD will not update.");
            return;
        }

        GameManager.Instance.OnCoinsChanged += UpdateCoins;
        GameManager.Instance.OnHealthChanged += UpdateHealth;
        GameManager.Instance.OnPlayerDied += ShowDeathMessage;

        lastCoins = GameManager.Instance.Coins;
        lastHealth = GameManager.Instance.CurrentHealth;

        UpdateCoins(GameManager.Instance.Coins);
        UpdateHealth(GameManager.Instance.CurrentHealth, GameManager.Instance.MaxHealth);
    }

    private void Update()
    {
        CheckRevengeStatus();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= UpdateCoins;
            GameManager.Instance.OnHealthChanged -= UpdateHealth;
            GameManager.Instance.OnPlayerDied -= ShowDeathMessage;
        }
    }

    private void UpdateCoins(int coins)
    {
        if (coinText != null)
        {
            coinText.text = "Coins: " + coins;
        }

        if (coins > lastCoins)
        {
            int gained = coins - lastCoins;
            ShowStatusMessage("+" + gained + " coins");
        }

        lastCoins = coins;
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = "HP: " + currentHealth + " / " + maxHealth;
        }

        if (currentHealth < lastHealth)
        {
            int damage = lastHealth - currentHealth;
            ShowStatusMessage("Damage: -" + damage);
        }
        else if (currentHealth > lastHealth)
        {
            int heal = currentHealth - lastHealth;
            ShowStatusMessage("Healed: +" + heal);
        }

        lastHealth = currentHealth;
    }

    private void CheckRevengeStatus()
    {
        if (SwarmRevengeManager.Instance == null)
        {
            return;
        }

        bool isRevengeActive = SwarmRevengeManager.Instance.IsRevengeActive;

        if (isRevengeActive && !wasRevengeActive)
        {
            ShowStatusMessage("Mosquito revenge is active!");
        }

        if (!isRevengeActive && wasRevengeActive)
        {
            ShowStatusMessage("Mosquito revenge ended.");
        }

        wasRevengeActive = isRevengeActive;
    }

    private void ShowDeathMessage()
    {
        ShowStatusMessage("You died!");
    }

    public void ShowStatusMessage(string message)
    {
        if (statusText == null)
        {
            return;
        }

        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
        }

        statusCoroutine = StartCoroutine(ShowStatusCoroutine(message));
    }

    private IEnumerator ShowStatusCoroutine(string message)
    {
        statusText.text = message;

        yield return new WaitForSecondsRealtime(statusShowTime);

        statusText.text = "";
        statusCoroutine = null;
    }
}
