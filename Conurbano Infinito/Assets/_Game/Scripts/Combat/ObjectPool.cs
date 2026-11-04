using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool : MonoBehaviour
{
    private readonly Dictionary<GameObject, Stack<GameObject>> available = new();
    private readonly List<ExperienceGem> gems = new();
    private int maximumGems = 256, maximumProjectiles = 256;
    public int ActiveCount { get; private set; }
    public int PeakActiveCount { get; private set; }
    public int ActiveProjectileCount { get; private set; }
    public int PeakProjectileCount { get; private set; }
    public int ActiveGemCount => gems.Count;
    public int PeakGemCount { get; private set; }
    public int CreatedCount { get; private set; }
    public long MergedDrops { get; private set; }
    public long SkippedProjectiles { get; private set; }

    public void Configure(RunConfiguration rules)
    {
        maximumGems = rules.MaximumGems;
        maximumProjectiles = rules.MaximumProjectiles;
    }

    public ExperienceGem DropExperience(ExperienceGem prefab, Vector3 position, int value)
    {
        if (value <= 0) return null;
        if (gems.Count >= maximumGems)
        {
            ExperienceGem nearest = null;
            float distance = float.PositiveInfinity;
            foreach (ExperienceGem gem in gems)
            {
                float candidate = (gem.transform.position - position).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = gem; distance = candidate;
            }
            // Even distant drops retain their full XP in the nearest existing gem; nothing expires.
            nearest.AddValue(value);
            MergedDrops++;
            return nearest;
        }
        ExperienceGem created = Spawn(prefab.gameObject, position, Quaternion.identity).GetComponent<ExperienceGem>();
        created.Initialize(value);
        return created;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if ((prefab.TryGetComponent<Projectile>(out _) || prefab.TryGetComponent<EnemyStone>(out _)) &&
            ActiveProjectileCount >= maximumProjectiles)
        {
            SkippedProjectiles++;
            return null;
        }
        if (prefab.TryGetComponent<ExperienceGem>(out _) && gems.Count >= maximumGems) return null;
        if (!available.TryGetValue(prefab, out Stack<GameObject> idle))
        {
            idle = new Stack<GameObject>();
            available.Add(prefab, idle);
        }

        GameObject instance = null;
        while (idle.Count > 0 && instance == null) instance = idle.Pop();
        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
            instance.AddComponent<PooledObject>().Initialize(this, prefab);
            CreatedCount++;
            return instance;
        }

        instance.transform.SetParent(null);
        instance.transform.SetPositionAndRotation(position, rotation);
        if (instance.TryGetComponent(out Rigidbody2D body))
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
        if (instance.TryGetComponent(out Health health)) health.ResetForReuse();
        instance.SetActive(true);
        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance.TryGetComponent(out PooledObject pooled) && pooled.Owner != null)
        {
            pooled.Owner.Return(pooled);
        }
        else
        {
            Destroy(instance);
        }
    }

    private void Return(PooledObject pooled)
    {
        GameObject instance = pooled.gameObject;
        if (!instance.activeSelf) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform);
        available[pooled.Prefab].Push(instance);
    }

    internal void Activated(PooledObject pooled)
    {
        ActiveCount++;
        PeakActiveCount = Mathf.Max(PeakActiveCount, ActiveCount);
        if (pooled.IsProjectile)
        {
            ActiveProjectileCount++;
            PeakProjectileCount = Mathf.Max(PeakProjectileCount, ActiveProjectileCount);
        }
        if (pooled.Gem != null)
        {
            gems.Add(pooled.Gem);
            PeakGemCount = Mathf.Max(PeakGemCount, gems.Count);
        }
    }

    internal void Deactivated(PooledObject pooled)
    {
        ActiveCount--;
        if (pooled.IsProjectile) ActiveProjectileCount--;
        if (pooled.Gem != null) gems.Remove(pooled.Gem);
    }
}
