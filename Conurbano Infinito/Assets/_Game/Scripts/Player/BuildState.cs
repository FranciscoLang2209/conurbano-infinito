using System.Collections.Generic;

// Only session state; definitions remain in the referenced ScriptableObjects.
internal sealed class BuildState
{
    internal sealed class Weapon
    {
        public WeaponData Data;
        public UpgradeData Evolution;
        public float PulseTime;
        public int PulseCount;
        public float PulseDamage, PulseSpeed;
        public int PulsePierce;
        public UnityEngine.Vector2 PulseDirection;
        public int Level = 1;
        public int DamageBonus;
        public float IntervalMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public int ProjectileCountBonus;
        public int RicochetBonus;
        public float ExplosionRadiusMultiplier = 1f;
        public float NextAttackTime;
        public readonly List<Projectile> Orbitals = new();
    }

    public readonly List<Weapon> Weapons;
    public readonly List<UpgradeData> Passives = new();
    public readonly Dictionary<UpgradeData, int> PassiveLevels = new();
    public readonly BuildSynergies Synergies = new();
    public CharacterData Character;
    public bool BossDefeated;
    public BuildState(List<Weapon> weapons) => Weapons = weapons;
    public int DamageBonus;
    public float DamagePercentBonus;
    public float IntervalMultiplier = 1f;
    public TimedBonus DamageBoost, AttackBoost;
}

internal struct TimedBonus
{
    private float bonus, until;
    public float Remaining => UnityEngine.Mathf.Max(0f, until - UnityEngine.Time.time);
    public float Value => Remaining > 0f ? bonus : 0f;

    public void Renew(float value, float duration)
    {
        if (!(value > 0f && value < 1f) || !(duration > 0f) || float.IsInfinity(duration)) return;
        // Renew the duration, retaining the stronger active effect; never stack intensity.
        bonus = UnityEngine.Mathf.Max(Value, value);
        until = UnityEngine.Time.time + duration;
    }
}
