using UnityEngine;

public class ItemUseService : MonoBehaviour
{
    public static ItemUseService Instance { get; private set; }

    [Header("Herb medicine heal amount")]
    [SerializeField] private int herbMedicineHealAmount = 10;

    [Header("Floral water immunity duration")]
    [SerializeField] private float floralWaterImmunityDuration = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool UseItem(ItemId itemId, out string message)
    {
        // Block all item usage during death/revive choice
        if (GameManager.Instance != null && GameManager.Instance.IsDeadOrDying)
        {
            message = "Cannot use items while dead";
            return false;
        }

        switch (itemId)
        {
            case ItemId.HerbMedicine:
                return UseHerbMedicine(out message);
            case ItemId.FloralWater:
                return UseFloralWater(out message);
            case ItemId.MosquitoSpray:
                return UseMosquitoSpray(out message);

            case ItemId.LifePotion:
                return UseLifePotion(out message);
            case ItemId.Flower:
            case ItemId.Grass:
                message = "This is a material, cannot be used directly";
                return false;
            default:
                message = "Unknown item";
                return false;
        }
    }

    private bool UseHerbMedicine(out string message)
    {
        if (GameManager.Instance == null)
        {
            message = "GameManager is not available";
            return false;
        }

        int hpBefore = GameManager.Instance.CurrentHealth;
        if (hpBefore >= GameManager.Instance.MaxHealth)
        {
            message = "Health is already full";
            return false;
        }

        GameManager.Instance.Heal(herbMedicineHealAmount);
        int hpAfter = GameManager.Instance.CurrentHealth;
        message = $"Use HerbMedicine: HP {hpBefore}->{hpAfter}";
        Debug.Log(message);
        return true;
    }

    private bool UseFloralWater(out string message)
    {
        if (MosquitoImmunityController.Instance == null)
        {
            message = "Immunity controller is not available";
            return false;
        }

        MosquitoImmunityController.Instance.Activate(floralWaterImmunityDuration);
        message = $"Use FloralWater: immunity {(int)floralWaterImmunityDuration}s";
        Debug.Log(message);
        return true;
    }

    private bool UseMosquitoSpray(out string message)
    {
        if (MosquitoClearUtility.Instance == null)
        {
            message = "Clear utility is not available";
            return false;
        }

        MosquitoClearUtility.Instance.ClearAllMosquitoes();
        message = "Use MosquitoSpray: all mosquitoes cleared";
        Debug.Log(message);
        return true;
    }

    private bool UseLifePotion(out string message)
    {
        if (GameManager.Instance == null)
        {
            message = "GameManager is not available";
            return false;
        }

        int hpBefore = GameManager.Instance.CurrentHealth;
        if (hpBefore >= GameManager.Instance.MaxHealth)
        {
            message = "Health is already full";
            return false;
        }

        int healAmount = GameManager.Instance.MaxHealth - hpBefore;
        GameManager.Instance.Heal(healAmount);
        int hpAfter = GameManager.Instance.CurrentHealth;
        message = $"Use LifePotion: HP {hpBefore}->{hpAfter}";
        Debug.Log(message);
        return true;
    }
}
