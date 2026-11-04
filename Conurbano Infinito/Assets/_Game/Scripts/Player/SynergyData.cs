using System;
using UnityEngine;

public enum SynergyEffect { MateDeGuardia, PotreroVeloz, FestejoDelBarrio }

[CreateAssetMenu(fileName = "Sinergia", menuName = "Conurbano Infinito/Sinergia")]
public sealed class SynergyData : ScriptableObject
{
    [SerializeField] private string stableId;
    [SerializeField] private string displayName;
    [SerializeField] private string requirements;
    [SerializeField, TextArea] private string description;
    [SerializeField] private SynergyEffect effect;
    [SerializeField] private string weaponId;
    [SerializeField, Min(1)] private int weaponLevel = 4;
    [SerializeField] private string passiveId;
    [SerializeField, Min(1)] private int passiveLevel = 3;
    [SerializeField] private string secondWeaponId;
    [SerializeField, Min(1)] private int secondWeaponLevel = 4;
    [SerializeField, Min(1)] private int triggerEvery = 1;
    [SerializeField, Min(1f)] private float multiplier = 1f;

    public string StableId => stableId;
    public string DisplayName => displayName;
    public string Requirements => requirements;
    public string Description => description;
    public SynergyEffect Effect => effect;
    public string WeaponId => weaponId;
    public int WeaponLevel => weaponLevel;
    public string PassiveId => passiveId;
    public int PassiveLevel => passiveLevel;
    public string SecondWeaponId => secondWeaponId;
    public int SecondWeaponLevel => secondWeaponLevel;
    public int TriggerEvery => triggerEvery;
    public float Multiplier => multiplier;

    private static SynergyData[] all;
    public static SynergyData[] All
    {
        get
        {
            if (all != null) return all;
            var loaded = Resources.LoadAll<SynergyData>("Synergies");
            Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.stableId, b.stableId));
            if (loaded.Length != 3) throw new InvalidOperationException("Se requieren las tres sinergias en Resources/Synergies.");
            for (int i = 0; i < loaded.Length; i++)
            {
                SynergyData item = loaded[i];
                if (string.IsNullOrWhiteSpace(item.stableId) || string.IsNullOrWhiteSpace(item.displayName) ||
                    string.IsNullOrWhiteSpace(item.requirements) || string.IsNullOrWhiteSpace(item.description) ||
                    string.IsNullOrWhiteSpace(item.weaponId) || item.weaponLevel < 1 || item.triggerEvery < 1 ||
                    !(item.multiplier >= 1f) || float.IsInfinity(item.multiplier) ||
                    !Enum.IsDefined(typeof(SynergyEffect), item.effect) ||
                    (item.effect == SynergyEffect.FestejoDelBarrio ? string.IsNullOrWhiteSpace(item.secondWeaponId) || item.secondWeaponLevel < 1 :
                        string.IsNullOrWhiteSpace(item.passiveId) || item.passiveLevel < 1))
                    throw new InvalidOperationException("Sinergia incompleta: " + item.name);
                for (int j = 0; j < i; j++)
                    if (loaded[j].stableId == item.stableId || loaded[j].effect == item.effect)
                        throw new InvalidOperationException("Sinergia duplicada: " + item.name);
            }
            return all = loaded;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDefinitions() => all = null;
}
