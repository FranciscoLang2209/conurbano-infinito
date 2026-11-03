using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyStone : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 6f;
    [SerializeField, Min(0.1f)] private float range = 8f;
    [SerializeField, Min(0.01f)] private float radius = 0.12f;
    [SerializeField, Min(1)] private int damage = 1;

    private Health owner;
    private Health target;
    private Vector2 direction;
    private float travelled;
    private int playerMask;

    private void Awake() => playerMask = LayerMask.GetMask("Player");

    public void Initialize(Health shooter, Health newTarget, Vector2 heading)
    {
        if (shooter == null || newTarget == null || heading.sqrMagnitude < 0.0001f || playerMask == 0)
        {
            ObjectPool.Release(gameObject);
            return;
        }
        owner = shooter;
        target = newTarget;
        direction = heading.normalized;
        travelled = 0f;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
        if (owner == null || owner.IsDead || !owner.gameObject.activeInHierarchy ||
            target == null || target.IsDead || !target.gameObject.activeInHierarchy)
        {
            ObjectPool.Release(gameObject);
            return;
        }

        float distance = Mathf.Min(speed * Time.fixedDeltaTime, range - travelled);
        // Sweep the whole step so a fast stone cannot skip the player's collider.
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, direction, distance, playerMask);
        if (hit.collider != null)
        {
            if (hit.collider.TryGetComponent(out Health health)) health.TakeDamage(damage);
            ObjectPool.Release(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * distance);
        travelled += distance;
        transform.Rotate(0f, 0f, -540f * Time.fixedDeltaTime);
        if (travelled >= range) ObjectPool.Release(gameObject);
    }

    private void OnDisable()
    {
        owner = null;
        target = null;
        travelled = 0f;
    }
}
