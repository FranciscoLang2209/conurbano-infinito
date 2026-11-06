using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class ExperienceDrop : MonoBehaviour
{
    [SerializeField] private ExperienceGem gemPrefab;
    [SerializeField, Min(1)] private int experienceValue = 1;
    [SerializeField] private EnemyScoreCategory scoreCategory;

    public EnemyScoreCategory ScoreCategory => scoreCategory;
    public event System.Action<EnemyScoreCategory> EnemyKilled;

    private Health health;
    private bool deathReported;

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
        deathReported = false;
        health.Died += DropGem;
    }

    private void OnDisable()
    {
        health.Died -= DropGem;
        // Listeners belong to this appearance, never to the next pooled life.
        EnemyKilled = null;
    }

    private void DropGem()
    {
        if (deathReported) return;
        deathReported = true;
        EnemyKilled?.Invoke(scoreCategory);
        if (TryGetComponent(out PooledObject pooled) && pooled.Owner != null)
        {
            pooled.Owner.DropExperience(gemPrefab, transform.position, Mathf.Max(1, experienceValue));
            return;
        }
        ExperienceGem gem = Instantiate(gemPrefab, transform.position, Quaternion.identity);
        gem.Initialize(Mathf.Max(1, experienceValue));
    }
}
