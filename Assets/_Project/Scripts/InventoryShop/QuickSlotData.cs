using System;

[Serializable]
public class QuickSlotData
{
    public string itemId;

    public bool IsEmpty => string.IsNullOrEmpty(itemId);
}
