using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildRules", menuName = "Conurbano Infinito/Reglas de build")]
public sealed class BuildRules : ScriptableObject
{
    [SerializeField] private string stableId;
    [SerializeField, Min(1)] private int maximumWeaponLevel = 8;
    [SerializeField, Min(0.01f)] private float minimumAttackInterval = 0.12f;
    [SerializeField, Min(1)] private int maximumProjectileCount = 12;
    [SerializeField, Min(0.1f)] private float maximumMovementSpeed = 10f;

    public string StableId => stableId;
    public int MaximumWeaponLevel => maximumWeaponLevel;
    public float MinimumAttackInterval => minimumAttackInterval;
    public int MaximumProjectileCount => maximumProjectileCount;
    public float MaximumMovementSpeed => maximumMovementSpeed;

    private static BuildRules current;
    public static BuildRules Current
    {
        get
        {
            if (current != null) return current;
            current = Resources.Load<BuildRules>("BuildRules");
            if (current == null || string.IsNullOrWhiteSpace(current.stableId) ||
                current.maximumWeaponLevel < 1 || current.maximumProjectileCount < 1 ||
                !(current.minimumAttackInterval > 0f) || float.IsInfinity(current.minimumAttackInterval) ||
                !(current.maximumMovementSpeed > 0f) || float.IsInfinity(current.maximumMovementSpeed))
                throw new InvalidOperationException("Faltan reglas válidas en Resources/BuildRules.");
            return current;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRules() => current = null;
}
