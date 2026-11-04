using System.Collections.Generic;
using UnityEngine;
using EquippedWeapon = BuildState.Weapon;

[RequireComponent(typeof(Health))]
public sealed class WeaponController : MonoBehaviour
{
    public const int MaxEquippedWeapons = 4;
    public const int MaxEquippedPassives = 4;

    [SerializeField] private WeaponData weapon;
    [SerializeField] private Projectile projectilePrefab;

    private readonly List<EquippedWeapon> equipped = new();
    private BuildState build;
    private BuildState Build => build ??= new BuildState(equipped);
    private Health health;
    private ObjectPool pool;
    private int enemyLayerMask;

    // Keep the original getters for the Termo and the existing hito checks.
    public int Damage => weapon == null ? 0 : Mathf.RoundToInt(GetDamage(FindWeapon(weapon)));
    public float DamageMultiplier => 1f + Build.DamagePercentBonus;
    public float FireInterval => weapon == null ? 0f : GetInterval(FindWeapon(weapon));
    public int EquippedWeaponCount => equipped.Count;
    public WeaponData GetEquippedWeapon(int index) => equipped[index].Data;
    public UpgradeData GetEvolution(WeaponData data) => FindWeapon(data)?.Evolution;
    public string GetWeaponDisplayName(WeaponData data) => GetEvolution(data)?.DisplayName ?? data.name;
    public string GetWeaponIdentity(WeaponData data) => GetEvolution(data)?.StableId ?? data.StableId;
    public bool HasDefeatedBoss => Build.BossDefeated;
    public IReadOnlyList<UpgradeData> SelectedPassives => Build.Passives;
    public int GetPassiveLevel(UpgradeData data) => Build.PassiveLevels.TryGetValue(data, out int level) ? level : 0;
    public event System.Action<SynergyData> SynergyActivated;
    public IReadOnlyList<SynergyData> ActiveSynergies => Build.Synergies.Active;
    public bool FestejoReady => Build.Synergies.FestejoReady;
    public string AffinityDescription => Build.Character != null ? Build.Character.AffinityDescription : "Sin afinidad adicional.";

    private void RefreshSynergies() => Build.Synergies.Evaluate(Build, data => SynergyActivated?.Invoke(data));

    internal void RegisterBossDefeated() => Build.BossDefeated = true;

    public bool CanEvolve(UpgradeData recipe) => recipe != null && recipe.Effect == UpgradeEffect.EvolveWeapon &&
        recipe.Evolution != EvolutionEffect.None && recipe.RequiredPassive != null && Build.BossDefeated &&
        FindWeapon(recipe.TargetWeapon) is EquippedWeapon item && item.Evolution == null &&
        item.Level == BuildRules.Current.MaximumWeaponLevel &&
        GetPassiveLevel(recipe.RequiredPassive) == recipe.RequiredPassive.MaximumLevel;

    public bool Evolve(UpgradeData recipe)
    {
        if (!CanEvolve(recipe)) return false;
        // Replace this slot's identity; retain its immutable family definition and all session modifiers.
        FindWeapon(recipe.TargetWeapon).Evolution = recipe;
        return true;
    }

    public bool CanUpgradePassive(UpgradeData data)
    {
        if (data == null || !data.IsPassive) return false;
        int level = GetPassiveLevel(data);
        if (level >= data.MaximumLevel || level == 0 && Build.Passives.Count >= MaxEquippedPassives) return false;
        if (data.Effect != UpgradeEffect.PassiveExplosionRadius && data.Effect != UpgradeEffect.PassiveProjectileCount)
            return true;
        foreach (EquippedWeapon item in equipped)
            if (data.SupportsWeapon(item.Data)) return true;
        return false;
    }

    internal void AdvancePassiveLevel(UpgradeData data)
    {
        int level = GetPassiveLevel(data);
        if (level == 0) Build.Passives.Add(data);
        Build.PassiveLevels[data] = level + 1;
        RefreshSynergies();
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        pool = FindFirstObjectByType<ObjectPool>();
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (TryGetComponent<PlayerController>(out _))
        {
            Build.Character = CharacterData.Selected;
            weapon = Build.Character.StartingWeapon;
        }
        if (weapon == null || projectilePrefab == null || enemyLayerMask == 0 || pool == null)
        {
            Debug.LogError("WeaponController requiere arma, proyectil, capa Enemy y ObjectPool.", this);
            enabled = false;
            return;
        }

        EquipWeapon(weapon);
    }

    private void OnDisable()
    {
        foreach (EquippedWeapon item in equipped)
        {
            item.PulseCount = 0;
            foreach (Projectile orbital in item.Orbitals)
            {
                if (orbital != null)
                {
                    ObjectPool.Release(orbital.gameObject);
                }
            }
            item.Orbitals.Clear();
        }
    }

    public bool IsEquipped(WeaponData data) => FindWeapon(data) != null;

    public bool CanEquipWeapon(WeaponData data) =>
        data != null && !IsEquipped(data) && equipped.Count < MaxEquippedWeapons;

    public bool EquipWeapon(WeaponData data)
    {
        if (!CanEquipWeapon(data))
        {
            return false;
        }

        equipped.Add(new EquippedWeapon { Data = data });
        RefreshSynergies();
        return true;
    }

    public void IncreaseDamage(int amount)
    {
        if (amount > 0)
        {
            Build.DamageBonus += amount;
            RefreshOrbitals();
        }
    }

    public void ApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f || float.IsInfinity(multiplier) || float.IsNaN(multiplier) ||
            Build.DamagePercentBonus != 0f) return;

        Build.DamagePercentBonus = multiplier - 1f;
        RefreshOrbitals();
    }

    public void ReduceFireInterval(float multiplier)
    {
        if (!(multiplier > 0f && multiplier < 1f))
        {
            return;
        }

        foreach (EquippedWeapon item in equipped)
        {
            float previousInterval = GetInterval(item);
            item.NextAttackTime = Time.time + Mathf.Max(0f, item.NextAttackTime - Time.time) *
                Mathf.Max(BuildRules.Current.MinimumAttackInterval, previousInterval * multiplier) / previousInterval;
        }
        Build.IntervalMultiplier *= multiplier;
        RefreshOrbitals();
    }

    public int GetWeaponLevel(WeaponData data) => FindWeapon(data)?.Level ?? 0;

    public bool CanUpgradeWeapon(WeaponData data, UpgradeEffect effect) =>
        BuildStatistics.IsCompatible(effect, data) && FindWeapon(data) is EquippedWeapon item &&
        item.Level < BuildRules.Current.MaximumWeaponLevel;

    internal void AdvanceWeaponLevel(WeaponData data)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && item.Level < BuildRules.Current.MaximumWeaponLevel)
        {
            item.Level++;
            RefreshSynergies();
        }
    }

    public void IncreaseWeaponDamage(WeaponData data, int amount)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && amount > 0)
        {
            item.DamageBonus += amount;
            RefreshOrbitals();
        }
    }

    public void ReduceWeaponFireInterval(WeaponData data, float multiplier)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item == null || !(multiplier > 0f && multiplier < 1f))
        {
            return;
        }

        float previousInterval = GetInterval(item);
        item.IntervalMultiplier *= multiplier;
        item.NextAttackTime = Time.time + Mathf.Max(0f, item.NextAttackTime - Time.time) * GetInterval(item) / previousInterval;
        RefreshOrbitals();
    }

    public void IncreaseWeaponProjectileCount(WeaponData data, int amount)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && amount > 0)
        {
            item.ProjectileCountBonus += amount;
        }
    }

    public void IncreaseWeaponProjectileSpeed(WeaponData data, float multiplier)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && multiplier > 1f && !float.IsInfinity(multiplier))
        {
            item.SpeedMultiplier *= multiplier;
            RefreshOrbitals();
        }
    }

    public void IncreaseWeaponExplosionRadius(WeaponData data, float multiplier)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && data.Pattern == WeaponPattern.Explosive &&
            multiplier > 1f && !float.IsInfinity(multiplier))
            item.ExplosionRadiusMultiplier *= multiplier;
    }

    public void IncreaseWeaponRicochets(WeaponData data, int amount)
    {
        EquippedWeapon item = FindWeapon(data);
        if (item != null && data.Pattern == WeaponPattern.Ricochet && amount > 0)
            item.RicochetBonus += amount;
    }

    private void Update()
    {
        if (health.IsDead)
        {
            foreach (EquippedWeapon item in equipped) item.PulseCount = 0;
            return;
        }
        if (Time.timeScale == 0f)
        {
            return;
        }

        foreach (EquippedWeapon item in equipped)
        {
            if (item.PulseCount > 0 && Time.time >= item.PulseTime)
            {
                FireSecondPulse(item);
                item.PulseCount = 0;
            }
            if (item.Data.Pattern == WeaponPattern.Orbiting)
            {
                EnsureOrbitals(item);
            }
            else if (item.PulseCount == 0 && Time.time >= item.NextAttackTime)
            {
                Health target = FindNearestEnemy(item.Data.Range);
                if (target != null)
                {
                    Fire(item, target);
                    item.NextAttackTime = Time.time + GetInterval(item);
                }
            }
        }
    }

    private void Fire(EquippedWeapon item, Health target)
    {
        WeaponData data = item.Data;
        Build.Synergies.BeginAttack(data, out int extraProjectiles, out float radiusMultiplier);
        Vector2 offset = (Vector2)target.transform.position - (Vector2)transform.position;
        Vector2 direction = offset.sqrMagnitude > 0f ? offset.normalized : Vector2.right;
        int count = Mathf.Min(BuildStatistics.ProjectileCount(Build, item) + extraProjectiles,
            BuildRules.Current.MaximumProjectileCount);

        if (item.Evolution != null && item.Evolution.Evolution == EvolutionEffect.ThermosBurst)
        {
            item.PulseTime = Time.time + item.Evolution.PulseDelay;
            item.PulseCount = Mathf.Min(BuildStatistics.ProjectileCount(Build, item),
                BuildRules.Current.MaximumProjectileCount - count);
            item.PulseDamage = GetDamage(item) * item.Evolution.SecondPulseDamage;
            item.PulseSpeed = GetSpeed(item);
            item.PulsePierce = BuildStatistics.Pierce(Build, item);
            item.PulseDirection = direction;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f :
                Mathf.Lerp(-data.SpreadDegrees / 2f, data.SpreadDegrees / 2f, i / (float)(count - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            GameObject spawned = pool.Spawn(projectilePrefab.gameObject, transform.position, Quaternion.identity);
            if (spawned == null) continue;
            Projectile projectile = spawned.GetComponent<Projectile>();
            projectile.SetAppearance(data.ProjectileSprite, data.ProjectileColor,
                data.ProjectileScale, data.Pattern);
            if (item.Evolution != null) projectile.SetEvolutionTint();
            if (data.Pattern == WeaponPattern.Explosive)
            {
                projectile.InitializeExplosive(target.transform.position, GetDamage(item),
                    GetSpeed(item), BuildStatistics.ExplosionRadius(Build, item) * radiusMultiplier);
            }
            else if (data.Pattern == WeaponPattern.Returning)
            {
                projectile.InitializeReturning(transform, shotDirection, GetDamage(item),
                    GetSpeed(item), data.Range);
            }
            else
            {
                projectile.Initialize(shotDirection, GetDamage(item), GetSpeed(item),
                    BuildStatistics.Pierce(Build, item), data.Range);
            }
            projectile.SetSource(this, data);
            projectile.SetRicochetReturn(item.Evolution != null && item.Evolution.Evolution == EvolutionEffect.BallReturn);
        }
    }

    private void FireSecondPulse(EquippedWeapon item)
    {
        WeaponData data = item.Data;
        for (int i = 0; i < item.PulseCount; i++)
        {
            float angle = item.PulseCount == 1 ? 0f : Mathf.Lerp(-data.SpreadDegrees / 2f,
                data.SpreadDegrees / 2f, i / (float)(item.PulseCount - 1));
            GameObject spawned = pool.Spawn(projectilePrefab.gameObject, transform.position, Quaternion.identity);
            if (spawned == null) continue;
            Projectile shot = spawned.GetComponent<Projectile>();
            shot.SetAppearance(data.ProjectileSprite, data.ProjectileColor, data.ProjectileScale, data.Pattern);
            shot.SetEvolutionTint();
            shot.Initialize(Quaternion.Euler(0f, 0f, angle) * item.PulseDirection,
                item.PulseDamage, item.PulseSpeed, item.PulsePierce, data.Range);
            shot.SetSource(this, data);
        }
    }

    private void EnsureOrbitals(EquippedWeapon item)
    {
        item.Orbitals.RemoveAll(orbital => orbital == null);
        int count = BuildStatistics.ProjectileCount(Build, item);
        if (item.Orbitals.Count == count)
        {
            return;
        }

        while (item.Orbitals.Count < count)
        {
            GameObject spawned = pool.Spawn(projectilePrefab.gameObject, transform.position, Quaternion.identity);
            if (spawned == null) break;
            Projectile orbital = spawned.GetComponent<Projectile>();
            orbital.SetAppearance(item.Data.ProjectileSprite, item.Data.ProjectileColor,
                item.Data.ProjectileScale, item.Data.Pattern);
            item.Orbitals.Add(orbital);
        }

        for (int i = 0; i < item.Orbitals.Count; i++)
        {
            item.Orbitals[i].InitializeOrbiting(transform, i * 360f / count,
                GetDamage(item), item.Data.Range, GetSpeed(item), GetInterval(item));
        }
    }

    private void RefreshOrbitals()
    {
        foreach (EquippedWeapon item in equipped)
        {
            if (item.Data.Pattern != WeaponPattern.Orbiting)
            {
                continue;
            }

            foreach (Projectile orbital in item.Orbitals)
            {
                if (orbital != null)
                {
                    orbital.SetOrbitStats(GetDamage(item), GetInterval(item), GetSpeed(item));
                }
            }
        }
    }

    private float GetDamage(EquippedWeapon item) => item == null ? 0f : BuildStatistics.Damage(Build, item);
    private float GetInterval(EquippedWeapon item) => item == null ? 0f : BuildStatistics.Interval(Build, item);
    private float GetSpeed(EquippedWeapon item) => BuildStatistics.ProjectileSpeed(Build, item);

    internal void RegisterDirectHit(WeaponData source)
    {
        if (isActiveAndEnabled && health != null && !health.IsDead && Time.timeScale > 0f)
            Build.Synergies.DirectHit(source);
    }

    private EquippedWeapon FindWeapon(WeaponData data)
    {
        foreach (EquippedWeapon item in equipped)
        {
            if (item.Data == data)
            {
                return item;
            }
        }
        return null;
    }

    private Health FindNearestEnemy(float range)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, range, enemyLayerMask);
        Health nearest = null;
        float nearestDistanceSquared = range * range;

        foreach (Collider2D collider in nearby)
        {
            if (!collider.TryGetComponent(out Health candidate) || candidate.IsDead)
            {
                continue;
            }

            float distanceSquared = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearest = candidate;
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearest;
    }
}
