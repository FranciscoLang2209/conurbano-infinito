using UnityEngine;

public enum UpgradeEffect
{
    Damage,
    FireInterval,
    MovementSpeed,
    MaximumHealth,
    EquipWeapon,
    WeaponDamage,
    WeaponFireInterval,
    WeaponProjectileCount,
    WeaponProjectileSpeed,
    WeaponExplosionRadius,
    WeaponRicochets
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Conurbano Infinito/Mejora")]
public sealed class UpgradeData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maximumLevel = 5;
    [SerializeField] private UpgradeEffect effect;
    [SerializeField] private float amount = 1f;
    [SerializeField] private WeaponData targetWeapon;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaximumLevel => maximumLevel;
    public WeaponData TargetWeapon => targetWeapon;
    public UpgradeEffect Effect => effect;

    public bool CanOffer(WeaponController weapon)
    {
        if (effect == UpgradeEffect.EquipWeapon)
        {
            return weapon.CanEquipWeapon(targetWeapon);
        }

        if (effect == UpgradeEffect.WeaponDamage || effect == UpgradeEffect.WeaponFireInterval ||
            effect == UpgradeEffect.WeaponProjectileCount || effect == UpgradeEffect.WeaponProjectileSpeed ||
            effect == UpgradeEffect.WeaponExplosionRadius || effect == UpgradeEffect.WeaponRicochets)
        {
            return targetWeapon != null && weapon.IsEquipped(targetWeapon) &&
                (effect != UpgradeEffect.WeaponRicochets || targetWeapon.Pattern == WeaponPattern.Ricochet);
        }

        return true;
    }

    public void Apply(PlayerController movement, WeaponController weapon, Health health)
    {
        switch (effect)
        {
            case UpgradeEffect.Damage:
                weapon.IncreaseDamage(Mathf.RoundToInt(amount));
                break;
            case UpgradeEffect.FireInterval:
                weapon.ReduceFireInterval(amount);
                break;
            case UpgradeEffect.MovementSpeed:
                movement.IncreaseMovementSpeed(amount);
                break;
            case UpgradeEffect.MaximumHealth:
                health.IncreaseMaximumHealth(Mathf.RoundToInt(amount));
                break;
            case UpgradeEffect.EquipWeapon:
                weapon.EquipWeapon(targetWeapon);
                break;
            case UpgradeEffect.WeaponDamage:
                weapon.IncreaseWeaponDamage(targetWeapon, Mathf.RoundToInt(amount));
                break;
            case UpgradeEffect.WeaponFireInterval:
                weapon.ReduceWeaponFireInterval(targetWeapon, amount);
                break;
            case UpgradeEffect.WeaponProjectileCount:
                weapon.IncreaseWeaponProjectileCount(targetWeapon, Mathf.RoundToInt(amount));
                break;
            case UpgradeEffect.WeaponProjectileSpeed:
                weapon.IncreaseWeaponProjectileSpeed(targetWeapon, amount);
                break;
            case UpgradeEffect.WeaponExplosionRadius:
                weapon.IncreaseWeaponExplosionRadius(targetWeapon, amount);
                break;
            case UpgradeEffect.WeaponRicochets:
                weapon.IncreaseWeaponRicochets(targetWeapon, Mathf.RoundToInt(amount));
                break;
        }
    }
}
