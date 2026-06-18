using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Coins")]
    [SerializeField] private int coins = 0;
    [SerializeField] private int coinsPerMosquito = 80;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int Coins => coins;
    public int CoinsPerMosquito => coinsPerMosquito;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public bool IsPlayerDead => playerDead;

    public bool IsDeadOrDying => playerDead;

    public event Action<int> OnCoinsChanged;
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;
    public event Action OnMosquitoKilled;

    private int reviveChances = 0;

    public int ReviveChances => reviveChances;

    public event Action<int> OnReviveChancesChanged;

    private bool playerDead = false;

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
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnCoinsChanged?.Invoke(coins);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        coins += amount;
        Debug.Log("Coins: " + coins);

        OnCoinsChanged?.Invoke(coins);
    }

    public void AddMosquitoKillReward()
    {
        AddCoins(coinsPerMosquito);
    }

    public void TakeDamage(int amount)
    {
        if (playerDead || amount <= 0)
        {
            return;
        }

        int hpBefore = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        Debug.Log($"Damage: -{amount}, HP {hpBefore}->{currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            // Priority 1: Master Sword revive chances
            if (reviveChances > 0)
            {
                reviveChances--;
                currentHealth = maxHealth;
                Debug.Log($"Master Sword revive! HP 0->{currentHealth}, remaining: {reviveChances}");
                OnReviveChancesChanged?.Invoke(reviveChances);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                return;
            }

            // No revive available — player dies
            playerDead = true;
            Debug.Log("Player died.");
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (playerDead || amount <= 0)
        {
            return;
        }

        int hpBefore = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"Heal: +{amount}, HP {hpBefore}->{currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (coins < amount)
        {
            return false;
        }

        int coinsBefore = coins;
        coins -= amount;
        Debug.Log($"Spend coins: {coinsBefore}->{coins}");
        OnCoinsChanged?.Invoke(coins);
        return true;
    }

    public void SetCoins(int value)
    {
        coins = Mathf.Max(0, value);
        OnCoinsChanged?.Invoke(coins);
    }

    public void RegisterMosquitoKill(int rewardAmount)
    {
        Debug.Log("GameManager: mosquito kill registered.");
        AddCoins(rewardAmount);
        OnMosquitoKilled?.Invoke();
    }

    public void GrantReviveChance()
    {
        reviveChances++;
        Debug.Log("Granted Master Sword revive. Total: " + reviveChances);
        OnReviveChancesChanged?.Invoke(reviveChances);
    }
}
