using UnityEngine;

[RequireComponent(typeof(Health), typeof(SpriteRenderer))]
public sealed class HitFlash : MonoBehaviour
{
    [SerializeField, Min(0f)] private float durationSeconds = 0.1f;
    private const float DeathSeconds = 0.22f;
    private const float ImpactSeconds = 0.16f;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Animator animator;
    private bool normalAnimation;
    private SpriteRenderer impact;
    private Color normalColor;
    private Vector3 normalScale;
    private Quaternion normalRotation;
    private bool normalSimulation;
    private float endTime;
    private float deathStartTime;
    private float impactStartTime;
    private bool dying;
    private static Sprite[] impactFrames;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator != null) normalAnimation = animator.enabled;
        normalColor = spriteRenderer.color;
        normalScale = transform.localScale;
        normalRotation = transform.localRotation;
        if (body != null) normalSimulation = body.simulated;
    }

    private void OnEnable()
    {
        dying = false;
        endTime = 0f;
        health.Damaged += Flash;
    }

    private void OnDisable()
    {
        health.Damaged -= Flash;
        StopAllCoroutines();
        spriteRenderer.color = normalColor;
        transform.localScale = normalScale;
        transform.localRotation = normalRotation;
        if (body != null) body.simulated = normalSimulation;
        if (animator != null) animator.enabled = normalAnimation;
        if (impact != null)
        {
            impact.gameObject.SetActive(false);
            impact.color = Color.white;
            impact.transform.localScale = Vector3.one;
            impact.transform.localRotation = Quaternion.identity;
        }
        dying = false;
        endTime = 0f;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (impact != null && impact.gameObject.activeSelf)
        {
            float progress = Mathf.Clamp01((Time.time - impactStartTime) / ImpactSeconds);
            impact.sprite = impactFrames[Mathf.Min((int)(progress * impactFrames.Length), impactFrames.Length - 1)];
            if (progress >= 1f) impact.gameObject.SetActive(false);
        }
        if (dying)
        {
            float progress = Mathf.Clamp01((Time.time - deathStartTime) / DeathSeconds);
            Color color = normalColor;
            color.a *= 1f - progress;
            spriteRenderer.color = color;
            transform.localScale = Vector3.Scale(normalScale,
                new Vector3(Mathf.Lerp(1.12f, 0.35f, progress), Mathf.Lerp(0.85f, 0.35f, progress), 1f));
            transform.localRotation = normalRotation * Quaternion.Euler(0, 0,
                (spriteRenderer.flipX ? 24f : -24f) * progress);
            if (progress >= 1f) ObjectPool.Release(gameObject);
            return;
        }

        if (endTime > 0f && Time.time >= endTime)
        {
            spriteRenderer.color = normalColor;
            endTime = 0f;
        }
    }

    private void Flash()
    {
        spriteRenderer.color = Color.white;
        endTime = Time.time + durationSeconds;
        if (impact == null) CreateImpact();
        impact.sprite = impactFrames[0];
        impact.transform.localScale = Vector3.one;
        impact.color = Color.white;
        impactStartTime = Time.time;
        impact.gameObject.SetActive(true);
    }

    public bool BeginDeath()
    {
        if (!isActiveAndEnabled) return false;
        dying = true;
        deathStartTime = Time.time;
        if (body != null) body.simulated = false;
        if (animator != null) animator.enabled = false;
        return true;
    }

    private void CreateImpact()
    {
        if (impactFrames == null || impactFrames[0] == null)
        {
            var texture = Resources.Load<Texture2D>("NinjaAdventure/impact");
            impactFrames = new Sprite[4];
            for (int i = 0; i < impactFrames.Length; i++)
                impactFrames[i] = Sprite.Create(texture, new Rect(i * 32, 0, 32, 32),
                    new Vector2(0.5f, 0.5f), 48f);
        }

        var effect = new GameObject("Impacto visual");
        effect.transform.SetParent(transform, false);
        impact = effect.AddComponent<SpriteRenderer>();
        impact.sprite = impactFrames[0];
        impact.sortingLayerID = spriteRenderer.sortingLayerID;
        impact.sortingOrder = spriteRenderer.sortingOrder + 1;
        effect.SetActive(false);
    }
}
