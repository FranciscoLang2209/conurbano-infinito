using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponData weapon;
    [SerializeField] private Projectile projectilePrefab;

    private sealed class EquippedWeapon
    {
        public WeaponData Data;
        public float NextAttackTime;
        public int DamageBonus;
        public float IntervalMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public int ProjectileCountBonus;
        public readonly List<Projectile> Orbitals = new();
    }

    private readonly List<EquippedWeapon> equipped = new();
    private Health health;
    private ObjectPool pool;
    private int enemyLayerMask;
    private int damageBonus;
    private float damageMultiplier = 1f;
    private float fireIntervalMultiplier = 1f;

    // Keep the original getters for the Termo and the existing hito checks.
    public int Damage => weapon == null ? 0 : Mathf.RoundToInt(GetDamage(FindWeapon(weapon)));
    public float DamageMultiplier => damageMultiplier;
    public float FireInterval => weapon == null ? 0f : GetInterval(FindWeapon(weapon));
    public int EquippedWeaponCount => equipped.Count;
    public WeaponData GetEquippedWeapon(int index) => equipped[index].Data;

    private void Awake()
    {
        health = GetComponent<Health>();
        pool = FindFirstObjectByType<ObjectPool>();
        enemyLayerMask = LayerMask.GetMask("Enemy");
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

    public bool EquipWeapon(WeaponData data)
    {
        if (data == null || IsEquipped(data))
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
            damageBonus += amount;
            RefreshOrbitals();
        }
    }

    public void ApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f || float.IsInfinity(multiplier) || float.IsNaN(multiplier) ||
            damageMultiplier != 1f) return;

        damageMultiplier = multiplier;
        RefreshOrbitals();
    }

    public void ReduceFireInterval(float multiplier)
    {
        if (multiplier <= 0f || multiplier >= 1f)
        {
            return;
        }

        fireIntervalMultiplier *= multiplier;
        foreach (EquippedWeapon item in equipped)
        {
            item.NextAttackTime = Time.time + Mathf.Max(0f, item.NextAttackTime - Time.time) * multiplier;
        }
        RefreshOrbitals();
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
        if (item == null || multiplier <= 0f || multiplier >= 1f)
        {
            return;
        }

        item.IntervalMultiplier *= multiplier;
        item.NextAttackTime = Time.time + Mathf.Max(0f, item.NextAttackTime - Time.time) * multiplier;
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
        if (item != null && multiplier > 1f)
        {
            item.SpeedMultiplier *= multiplier;
            RefreshOrbitals();
        }
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
        int count = data.ProjectileCount + item.ProjectileCountBonus;

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f :
                Mathf.Lerp(-data.SpreadDegrees / 2f, data.SpreadDegrees / 2f, i / (float)(count - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            Projectile projectile = pool.Spawn(projectilePrefab.gameObject, transform.position,
                Quaternion.identity).GetComponent<Projectile>();
            projectile.SetAppearance(data.ProjectileSprite, data.ProjectileColor,
                data.ProjectileScale, data.Pattern);
            if (data.Pattern == WeaponPattern.Returning)
            {
                projectile.InitializeReturning(transform, shotDirection, GetDamage(item),
                    GetSpeed(item), data.Range);
            }
            else
            {
                projectile.Initialize(shotDirection, GetDamage(item), GetSpeed(item),
                    data.Pierce, data.Range);
            }
        }
    }

    private void EnsureOrbitals(EquippedWeapon item)
    {
        item.Orbitals.RemoveAll(orbital => orbital == null);
        int count = item.Data.ProjectileCount + item.ProjectileCountBonus;
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

    private float GetDamage(EquippedWeapon item) => item == null ? 0f :
        (item.Data.Damage + damageBonus + item.DamageBonus) * damageMultiplier;

    private float GetInterval(EquippedWeapon item) => item == null ? 0f :
        item.Data.FireInterval * fireIntervalMultiplier * item.IntervalMultiplier;

    private float GetSpeed(EquippedWeapon item) => item.Data.ProjectileSpeed * item.SpeedMultiplier;

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
