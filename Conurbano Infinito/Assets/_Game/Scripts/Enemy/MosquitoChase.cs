using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class MosquitoChase : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 2f;

    internal float SpawnSpeedFactor { get; set; } = 1f;

    private Rigidbody2D body;
    private Health targetHealth;
    private static Sprite placeholderSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        SetTarget(target);

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f), 2f);
            }
            spriteRenderer.sprite = placeholderSprite;
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
        if (target == null || (targetHealth != null && targetHealth.IsDead))
        {
            return;
        }

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;
        if (distance <= 0f)
        {
            return;
        }

        Vector2 direction = offset.normalized;
        body.MovePosition(body.position + direction * Mathf.Min(distance, movementSpeed * SpawnSpeedFactor * Time.fixedDeltaTime));
    }
}
