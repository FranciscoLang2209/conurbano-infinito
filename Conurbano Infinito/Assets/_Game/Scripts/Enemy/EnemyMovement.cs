using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 1f;
    [SerializeField] private bool zigzag;
    [SerializeField, Min(0f)] private float zigzagStrength = 0.6f;
    [SerializeField, Min(0f)] private float zigzagFrequency = 3f;

    internal float SpawnSpeedFactor { get; set; } = 1f;

    private Rigidbody2D body;
    private Health targetHealth;
    private static Sprite placeholderSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        SetTarget(target);
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite == null)
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f), 2f);
            }
            renderer.sprite = placeholderSprite;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponent<Health>() : null;
    }

    private void OnDisable() => SpawnSpeedFactor = 1f;

    private void FixedUpdate()
    {
        if (target == null || (targetHealth != null && targetHealth.IsDead)) return;
        Vector2 offset = (Vector2)target.position - body.position;
        if (offset.sqrMagnitude < 0.0001f) return;

        Vector2 direction = offset.normalized;
        if (zigzag)
        {
            direction = (direction + new Vector2(-direction.y, direction.x) *
                Mathf.Sin(Time.time * zigzagFrequency) * zigzagStrength).normalized;
        }
        body.MovePosition(body.position + direction * movementSpeed * SpawnSpeedFactor * Time.fixedDeltaTime);
    }
}
