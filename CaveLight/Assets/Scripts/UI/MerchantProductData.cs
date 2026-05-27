using UnityEngine;

public enum MerchantProductType
{
    MaxEnergy,
    RestoreEnergy,
    EnergySaver,
    AttackDamage,
    HeavyStrike,
    AttackSpeed,
    AbsorbRadius,
    AbsorbSpeed,
    LightRadius,
    StableLight,
    InvincibleTime,
    DamageBuffer,
    Utility
}

public class MerchantProductData
{
    public readonly string id;
    public readonly string displayName;
    public readonly string description;
    public readonly int cost;
    public readonly MerchantProductType type;
    public readonly float value;
    public readonly Color iconColor;
    public readonly string iconText;

    public MerchantProductData(string id, string displayName, string description, int cost, MerchantProductType type, float value, Color iconColor, string iconText)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.cost = cost;
        this.type = type;
        this.value = value;
        this.iconColor = iconColor;
        this.iconText = iconText;
    }
}
