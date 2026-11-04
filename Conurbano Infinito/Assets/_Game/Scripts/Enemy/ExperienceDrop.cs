using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class ExperienceDrop : MonoBehaviour
{
    [SerializeField] private ExperienceGem gemPrefab;
    [SerializeField, Min(1)] private int experienceValue = 1;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (gemPrefab == null)
        {
            Debug.LogError("ExperienceDrop requiere una gema de experiencia.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        health.Died += DropGem;
    }

    private void OnDisable()
    {
        health.Died -= DropGem;
    }

    private void DropGem()
    {
        if (TryGetComponent(out PooledObject pooled) && pooled.Owner != null)
        {
            pooled.Owner.DropExperience(gemPrefab, transform.position, Mathf.Max(1, experienceValue));
            return;
        }
        ExperienceGem gem = Instantiate(gemPrefab, transform.position, Quaternion.identity);
        gem.Initialize(Mathf.Max(1, experienceValue));
    }
}
