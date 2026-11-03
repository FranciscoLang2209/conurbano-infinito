using System.Collections.Generic;

// Only session state; definitions remain in the referenced ScriptableObjects.
internal sealed class BuildState
{
    internal sealed class Weapon
    {
        public WeaponData Data;
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
    public BuildState(List<Weapon> weapons) => Weapons = weapons;
    public int DamageBonus;
    public float DamagePercentBonus;
    public float IntervalMultiplier = 1f;
}
