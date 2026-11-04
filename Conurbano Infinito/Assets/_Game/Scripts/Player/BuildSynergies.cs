using System;
using System.Collections.Generic;

// Session counters belong to the build, never to the recipe or pooled projectile.
internal sealed class BuildSynergies
{
    public readonly List<SynergyData> Active = new();
    private SynergyData mate, potrero, festejo;
    private int mateAttacks, ballHits;
    public bool FestejoReady { get; private set; }

    public void Evaluate(BuildState build, Action<SynergyData> activated)
    {
        foreach (SynergyData data in SynergyData.All)
        {
            if (Active.Contains(data) || WeaponLevel(build, data.WeaponId) < data.WeaponLevel) continue;
            if (data.Effect == SynergyEffect.FestejoDelBarrio)
            {
                if (WeaponLevel(build, data.SecondWeaponId) < data.SecondWeaponLevel) continue;
            }
            else
            {
                int level = 0;
                foreach (var passive in build.PassiveLevels)
                    if (passive.Key.StableId == data.PassiveId) level = passive.Value;
                if (level < data.PassiveLevel) continue;
            }
            Active.Add(data);
            switch (data.Effect)
            {
                case SynergyEffect.MateDeGuardia: mate = data; break;
                case SynergyEffect.PotreroVeloz: potrero = data; break;
                case SynergyEffect.FestejoDelBarrio: festejo = data; break;
            }
            activated?.Invoke(data);
        }
    }

    private static int WeaponLevel(BuildState build, string id)
    {
        foreach (BuildState.Weapon weapon in build.Weapons)
            if (weapon.Data.StableId == id) return weapon.Level;
        return 0;
    }

    public float SpeedMultiplier(WeaponData weapon) =>
        potrero != null && potrero.WeaponId == weapon.StableId ? potrero.Multiplier : 1f;

    public void BeginAttack(WeaponData weapon, out int extraProjectiles, out float radiusMultiplier)
    {
        extraProjectiles = 0;
        radiusMultiplier = 1f;
        if (mate != null && mate.WeaponId == weapon.StableId && ++mateAttacks == mate.TriggerEvery)
        {
            mateAttacks = 0;
            extraProjectiles = 1;
        }
        if (festejo != null && festejo.SecondWeaponId == weapon.StableId && FestejoReady)
        {
            radiusMultiplier = festejo.Multiplier;
            FestejoReady = false;
        }
    }

    public void DirectHit(WeaponData weapon)
    {
        // A ready charge cannot bank more hits toward a second charge.
        if (festejo == null || weapon == null || weapon.StableId != festejo.WeaponId || FestejoReady) return;
        if (++ballHits < festejo.TriggerEvery) return;
        ballHits = 0;
        FestejoReady = true;
    }
}
