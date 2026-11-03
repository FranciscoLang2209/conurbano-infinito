using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 5;
    [SerializeField, Min(0f)] private float invulnerabilitySeconds;
    [SerializeField] private bool destroyOnDeath;

    public int MaximumHealth => maximumHealth + maximumHealthBonus;
    public int CurrentHealth => Mathf.CeilToInt(remainingHealth - 0.0001f);
    public bool IsDead => remainingHealth <= 0.0001f;

    public event Action Damaged;
    public event Action Died;

    private float nextDamageTime;
    private float remainingHealth;
    private int maximumHealthBonus;

    private void Awake()
    {
        remainingHealth = MaximumHealth;
    }

    public void ConfigureStartingHealth(int amount)
    {
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        maximumHealth = amount;
        ResetForReuse();
    }

    public void ResetForReuse()
    {
        maximumHealthBonus = 0;
        remainingHealth = MaximumHealth;
        nextDamageTime = 0f;
    }

    public void IncreaseMaximumHealth(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        maximumHealthBonus += amount;
        remainingHealth += amount;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        remainingHealth = Mathf.Min(MaximumHealth, remainingHealth + amount);
    }

    public bool TakeDamage(int amount) => TakeDamage((float)amount);

    public bool TakeDamage(float amount)
    {
        if (!(amount > 0f) || float.IsInfinity(amount) || IsDead || Time.time < nextDamageTime)
        {
            return false;
        }

        remainingHealth = Mathf.Max(0f, remainingHealth - amount);
        nextDamageTime = Time.time + invulnerabilitySeconds;
        Damaged?.Invoke();

        if (IsDead)
        {
            Died?.Invoke();
            if (destroyOnDeath)
            {
                if (!TryGetComponent(out HitFlash visual) || !visual.BeginDeath())
                    ObjectPool.Release(gameObject);
            }
        }

        return true;
    }
}
