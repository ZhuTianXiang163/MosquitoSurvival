using System;

[Serializable]
public class InventorySlotData
{
    public string itemId;
    public int amount;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || amount <= 0;
}
