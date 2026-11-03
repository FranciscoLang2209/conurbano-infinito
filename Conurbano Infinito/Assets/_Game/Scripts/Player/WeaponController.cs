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
    public IReadOnlyList<UpgradeData> SelectedPassives => Build.Passives;
    public int GetPassiveLevel(UpgradeData data) => Build.PassiveLevels.TryGetValue(data, out int level) ? level : 0;

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
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        pool = FindFirstObjectByType<ObjectPool>();
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (TryGetComponent<PlayerController>(out _)) weapon = CharacterData.Selected.StartingWeapon;
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
        if (item != null && item.Level < BuildRules.Current.MaximumWeaponLevel) item.Level++;
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
        if (Time.timeScale == 0f || health.IsDead)
        {
            return;
        }

        foreach (EquippedWeapon item in equipped)
        {
            if (item.Data.Pattern == WeaponPattern.Orbiting)
            {
                EnsureOrbitals(item);
            }
            else if (Time.time >= item.NextAttackTime)
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
        Vector2 offset = (Vector2)target.transform.position - (Vector2)transform.position;
        Vector2 direction = offset.sqrMagnitude > 0f ? offset.normalized : Vector2.right;
        int count = BuildStatistics.ProjectileCount(Build, item);

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f :
                Mathf.Lerp(-data.SpreadDegrees / 2f, data.SpreadDegrees / 2f, i / (float)(count - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            Projectile projectile = pool.Spawn(projectilePrefab.gameObject, transform.position,
                Quaternion.identity).GetComponent<Projectile>();
            projectile.SetAppearance(data.ProjectileSprite, data.ProjectileColor,
                data.ProjectileScale, data.Pattern);
            if (data.Pattern == WeaponPattern.Explosive)
            {
                projectile.InitializeExplosive(target.transform.position, GetDamage(item),
                    GetSpeed(item), BuildStatistics.ExplosionRadius(Build, item));
            }
            else if (data.Pattern == WeaponPattern.Returning)
            {
                projectile.InitializeReturning(transform, shotDirection, GetDamage(item),
                    GetSpeed(item), data.Range);
            }
            else
            {
                projectile.Initialize(shotDirection, GetDamage(item), GetSpeed(item),
                    data.Pierce + item.RicochetBonus, data.Range);
            }
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
            Projectile orbital = pool.Spawn(projectilePrefab.gameObject, transform.position,
                Quaternion.identity).GetComponent<Projectile>();
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
    private float GetSpeed(EquippedWeapon item) => BuildStatistics.ProjectileSpeed(item);

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
