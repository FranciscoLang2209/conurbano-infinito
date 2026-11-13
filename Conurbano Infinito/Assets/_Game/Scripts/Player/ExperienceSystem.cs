using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class ExperienceSystem : MonoBehaviour
{
    [SerializeField, Min(1)] private int initialExperienceRequired = 5;
    [SerializeField, Min(1)] private int experienceIncreasePerLevel = 3;
    [SerializeField, Min(0f)] private float baseCollectionRadius = 1.2f;

    public int Level { get; private set; } = 1;
    public int CurrentExperience { get; private set; }
    public int RequiredExperience => (int)Math.Min(int.MaxValue,
        (long)Math.Max(1, initialExperienceRequired) + (long)(Level - 1) * Math.Max(1, experienceIncreasePerLevel));
    public float CollectionRadius => baseCollectionRadius *
        (Time.time < collectionBonusUntil ? collectionMultiplier : 1f);

    public event Action<int> LevelUp;

    private readonly List<Collider2D> nearbyPickups = new();
    private ContactFilter2D pickupFilter;
    private Health health;
    private float collectionMultiplier = 1f;
    private float collectionBonusUntil;
    public float CollectionBonusRemaining => Mathf.Max(0f, collectionBonusUntil - Time.time);
    public float CollectionBonusPercent => CollectionBonusRemaining > 0f ? (collectionMultiplier - 1f) * 100f : 0f;
    public void ClearTemporaryCollection() { collectionMultiplier = 1f; collectionBonusUntil = 0f; }

    private void Awake()
    {
        health = GetComponent<Health>();
        pickupFilter = new ContactFilter2D();
        pickupFilter.SetLayerMask(LayerMask.GetMask("Pickup"));
        pickupFilter.useTriggers = true;
    }

    private void FixedUpdate()
    {
        if (Time.time >= collectionBonusUntil || health.IsDead) return;
        float radiusSquared = CollectionRadius * CollectionRadius;
        Physics2D.OverlapCircle(transform.position, CollectionRadius, pickupFilter, nearbyPickups);
        foreach (Collider2D pickup in nearbyPickups)
            if (pickup != null &&
                ((Vector2)pickup.transform.position - (Vector2)transform.position).sqrMagnitude <= radiusSquared &&
                pickup.TryGetComponent(out ExperienceGem gem))
                gem.TryCollect(this);
    }

    public void ApplyCollectionRadiusMultiplier(float multiplier, float duration)
    {
        if (!(multiplier > 1f) || !(duration > 0f) || float.IsInfinity(multiplier) || float.IsInfinity(duration)) return;
        collectionMultiplier = Mathf.Max(CollectionBonusRemaining > 0f ? collectionMultiplier : 1f, multiplier);
        collectionBonusUntil = Time.time + duration;
    }

    public void AddExperience(int amount) => AddExperience((long)amount);

    public void AddExperience(long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long total = checked((long)CurrentExperience + amount);
        while (total >= RequiredExperience && Level < int.MaxValue)
        {
            total -= RequiredExperience;
            Level++;
            CurrentExperience = (int)Math.Min(int.MaxValue, total);
            LevelUp?.Invoke(Level);
        }

        CurrentExperience = (int)Math.Min(int.MaxValue, total);
    }
}
