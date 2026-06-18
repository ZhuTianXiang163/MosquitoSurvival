using UnityEngine;

/// <summary>
/// Loads Retro Inventory pixel-art sprites from Resources at runtime.
/// Access via RetroInventoryAssets.I (lazy singleton).
/// </summary>
public class RetroInventoryAssets
{
    private static RetroInventoryAssets _instance;
    public static RetroInventoryAssets I => _instance ?? (_instance = new RetroInventoryAssets());

    private const string BASE = "RetroInventory/";

    // Panel backgrounds
    public Sprite PanelBg { get; private set; }
    public Sprite SettingsBg { get; private set; }

    // Slots (1-10)
    public Sprite[] Slots { get; private set; }

    // Icons
    public Sprite CloseIcon { get; private set; }
    public Sprite CloseIcon2 { get; private set; }
    public Sprite HeartRed { get; private set; }
    public Sprite HeartBlue { get; private set; }

    // Health bars
    public Sprite HealthBarBg { get; private set; }
    public Sprite HealthBarLarge { get; private set; }

    private RetroInventoryAssets()
    {
        PanelBg = Load("Inventory_9Slices");
        SettingsBg = Load("Settings");

        Slots = new Sprite[10];
        for (int i = 0; i < 10; i++)
            Slots[i] = Load($"Inventory_Slot_{i + 1}");

        CloseIcon = Load("Settings_Cross01");
        CloseIcon2 = Load("Settings_Cross02");
        HeartRed = Load("Heart_Red");
        HeartBlue = Load("Heart_Blue");

        HealthBarBg = Load("Health_01");
        HealthBarLarge = Load("Health_04");

        Debug.Log($"RetroInventoryAssets loaded: panel={PanelBg != null}, slots[0]={Slots[0] != null}, heart={HeartRed != null}");
    }

    private Sprite Load(string name)
    {
        Sprite s = Resources.Load<Sprite>(BASE + name);
        if (s == null)
            Debug.LogWarning($"RetroInventoryAssets: failed to load '{BASE + name}'");
        return s;
    }
}
