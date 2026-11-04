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
    WeaponRicochets,
    PassiveExplosionRadius,
    PassiveProjectileCount,
    EvolveWeapon
}

public enum EvolutionEffect { None, ThermosBurst, BallReturn }

[CreateAssetMenu(fileName = "Upgrade", menuName = "Conurbano Infinito/Mejora")]
public sealed class UpgradeData : ScriptableObject
{
    [SerializeField] private string stableId;
    public string StableId => stableId;

    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maximumLevel = 5;
    [SerializeField] private UpgradeEffect effect;
    [SerializeField] private float amount = 1f;
    [SerializeField] private WeaponData targetWeapon;
    [SerializeField] private WeaponData[] compatibleWeapons = new WeaponData[0];
    [SerializeField, Min(1)] private int unlockAtLevel = 1;
    [SerializeField] private UpgradeData requiredPassive;
    [SerializeField] private EvolutionEffect evolution;
    [SerializeField, Min(0.01f)] private float pulseDelay = 0.12f;
    [SerializeField, Range(0f, 1f)] private float secondPulseDamage = 0.5f;
    [SerializeField, Min(0)] private int extraRicochets = 2;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaximumLevel => maximumLevel;
    public WeaponData TargetWeapon => targetWeapon;
    public UpgradeEffect Effect => effect;
    public UpgradeData RequiredPassive => requiredPassive;
    public EvolutionEffect Evolution => evolution;
    public float PulseDelay => pulseDelay;
    public float SecondPulseDamage => secondPulseDamage;
    public int ExtraRicochets => extraRicochets;

    private static UpgradeData[] evolutionCards;
    public static UpgradeData[] EvolutionCards
    {
        get
        {
            if (evolutionCards != null) return evolutionCards;
            var loaded = Resources.LoadAll<UpgradeData>("Evolutions");
            System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.StableId, b.StableId));
            if (loaded.Length != 2) throw new System.InvalidOperationException("Se requieren las dos evoluciones en Resources/Evolutions.");
            for (int i = 0; i < loaded.Length; i++)
            {
                UpgradeData card = loaded[i];
                if (string.IsNullOrWhiteSpace(card.StableId) || string.IsNullOrWhiteSpace(card.DisplayName) ||
                    string.IsNullOrWhiteSpace(card.Description) || card.effect != UpgradeEffect.EvolveWeapon ||
                    card.maximumLevel != 1 || card.targetWeapon == null || card.requiredPassive == null ||
                    !card.requiredPassive.IsPassive || card.requiredPassive.MaximumLevel < 1 ||
                    !(card.pulseDelay > 0f) || float.IsInfinity(card.pulseDelay) ||
                    !(card.secondPulseDamage > 0f && card.secondPulseDamage <= 1f) || card.extraRicochets < 0 ||
                    !(card.evolution == EvolutionEffect.ThermosBurst && card.targetWeapon.Pattern == WeaponPattern.Straight ||
                      card.evolution == EvolutionEffect.BallReturn && card.targetWeapon.Pattern == WeaponPattern.Ricochet))
                    throw new System.InvalidOperationException("Evolución incompleta: " + card.name);
                for (int j = 0; j < i; j++)
                    if (loaded[j].StableId == card.StableId || loaded[j].TargetWeapon == card.TargetWeapon)
                        throw new System.InvalidOperationException("Evolución duplicada: " + card.name);
            }
            return evolutionCards = loaded;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvolutionCards() => evolutionCards = null;
    public bool IsPassive => effect <= UpgradeEffect.MaximumHealth ||
        effect == UpgradeEffect.PassiveExplosionRadius || effect == UpgradeEffect.PassiveProjectileCount;

    public bool SupportsWeapon(WeaponData data) =>
        data != null && compatibleWeapons != null && System.Array.IndexOf(compatibleWeapons, data) >= 0;

    public float PassiveBonus(int level) => level < unlockAtLevel ? 0f :
        effect == UpgradeEffect.PassiveProjectileCount ? amount : amount * level;

    public bool CanOffer(WeaponController weapon)
    {
        if (weapon == null) return false;
        if (effect == UpgradeEffect.EvolveWeapon) return weapon.CanEvolve(this);
        if (IsPassive) return weapon.CanUpgradePassive(this);
        if (effect == UpgradeEffect.EquipWeapon)
        {
            return weapon.CanEquipWeapon(targetWeapon);
        }

        if (BuildStatistics.IsWeaponUpgrade(effect))
            return weapon.CanUpgradeWeapon(targetWeapon, effect);

        return false;
    }

    public void Apply(PlayerController movement, WeaponController weapon, Health health)
    {
        if (!CanOffer(weapon)) return;
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
            case UpgradeEffect.EvolveWeapon:
                weapon.Evolve(this);
                break;
        }
        if (IsPassive) weapon.AdvancePassiveLevel(this);
        if (BuildStatistics.IsWeaponUpgrade(effect)) weapon.AdvanceWeaponLevel(targetWeapon);
    }
}
