using System;
using UnityEngine;

[Serializable]
public class ItemDefinition
{
    public ItemId id;
    public string displayName;
    public ItemCategory category;

    [TextArea]
    public string description;

    [TextArea]
    public string obtainHint;

    public int maxStack;
    public int buyPrice;
    public int sellPrice;

    public bool canBuy;
    public bool canSell;
    public bool canUse;
    public bool isLimitedGoods;

    public Sprite icon;
}
