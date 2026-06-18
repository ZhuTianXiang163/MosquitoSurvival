using System;
using System.Collections.Generic;

[Serializable]
public class InventorySaveData
{
    public int coins;
    public int capacity = 12;

    public int smallExpansionCount;
    public int mediumExpansionCount;
    public int largeExpansionCount;

    public List<InventorySlotData> slots = new List<InventorySlotData>();
    public List<QuickSlotData> quickSlots = new List<QuickSlotData>();

    public int limitedSprayRemaining = 1;
    public long limitedSprayRefreshUnixSeconds;
}
