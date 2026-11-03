using UnityEngine;

internal static class BuildStatistics
{
    // Damage stays fractional through Health; only the legacy integer getter rounds.
    public static float Damage(BuildState build, BuildState.Weapon weapon) =>
        (weapon.Data.Damage + build.DamageBonus + weapon.DamageBonus) * (1f + build.DamagePercentBonus);

    public static float Interval(BuildState build, BuildState.Weapon weapon) =>
        Mathf.Max(BuildRules.Current.MinimumAttackInterval,
            weapon.Data.FireInterval * build.IntervalMultiplier * weapon.IntervalMultiplier);

    public static int ProjectileCount(BuildState build, BuildState.Weapon weapon) =>
        Mathf.Clamp(weapon.Data.ProjectileCount + weapon.ProjectileCountBonus +
            Mathf.RoundToInt(PassiveBonus(build, weapon.Data, UpgradeEffect.PassiveProjectileCount)),
            1, BuildRules.Current.MaximumProjectileCount);

    public static float ProjectileSpeed(BuildState.Weapon weapon) => weapon.Data.ProjectileSpeed * weapon.SpeedMultiplier;
    public static float ExplosionRadius(BuildState build, BuildState.Weapon weapon) =>
        weapon.Data.ExplosionRadius * weapon.ExplosionRadiusMultiplier *
        (1f + PassiveBonus(build, weapon.Data, UpgradeEffect.PassiveExplosionRadius));

    private static float PassiveBonus(BuildState build, WeaponData weapon, UpgradeEffect effect)
    {
        float bonus = 0f;
        foreach (UpgradeData passive in build.Passives)
            if (passive.Effect == effect && passive.SupportsWeapon(weapon))
                bonus += passive.PassiveBonus(build.PassiveLevels[passive]);
        return bonus;
    }
    public static float MovementSpeed(float baseSpeed, float multiplier) =>
        Mathf.Min(baseSpeed * multiplier, BuildRules.Current.MaximumMovementSpeed);

    public static bool IsWeaponUpgrade(UpgradeEffect effect) =>
        effect >= UpgradeEffect.WeaponDamage && effect <= UpgradeEffect.WeaponRicochets;

    public static bool IsCompatible(UpgradeEffect effect, WeaponData weapon) =>
        weapon != null && IsWeaponUpgrade(effect) &&
        (effect != UpgradeEffect.WeaponExplosionRadius || weapon.Pattern == WeaponPattern.Explosive) &&
        (effect != UpgradeEffect.WeaponRicochets || weapon.Pattern == WeaponPattern.Ricochet);
}
