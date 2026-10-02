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
    private SpriteRenderer impact;
    private Color normalColor;
    private Vector3 normalScale;
    private Quaternion normalRotation;
    private bool normalSimulation;
    private float endTime;
    private float deathStartTime;
    private float impactStartTime;
    private bool dying;
    private static Sprite impactSprite;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
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
        if (dying)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - deathStartTime) / DeathSeconds);
            Color color = normalColor;
            color.a *= 1f - progress;
            spriteRenderer.color = color;
            if (progress >= 1f) ObjectPool.Release(gameObject);
            return;
        }

        if (endTime > 0f && Time.unscaledTime >= endTime)
        {
            spriteRenderer.color = normalColor;
            endTime = 0f;
        }

        if (impact != null && impact.gameObject.activeSelf)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - impactStartTime) / ImpactSeconds);
            impact.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.8f, progress);
            impact.color = new Color(1f, 0.85f, 0.3f, 1f - progress);
            if (progress >= 1f) impact.gameObject.SetActive(false);
        }
    }

    private void Flash()
    {
        spriteRenderer.color = Color.white;
        endTime = Time.unscaledTime + durationSeconds;
        if (impact == null) CreateImpact();
        impact.transform.localScale = Vector3.one * 0.2f;
        impact.color = new Color(1f, 0.85f, 0.3f, 1f);
        impactStartTime = Time.unscaledTime;
        impact.gameObject.SetActive(true);
    }

    public bool BeginDeath()
    {
        if (!isActiveAndEnabled) return false;
        dying = true;
        deathStartTime = Time.unscaledTime;
        if (body != null) body.simulated = false;
        return true;
    }

    private void CreateImpact()
    {
        if (impactSprite == null)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float radius = new Vector2(x - 7.5f, y - 7.5f).magnitude;
                texture.SetPixel(x, y, radius >= 5f && radius <= 7f ? Color.white : Color.clear);
            }
            texture.Apply();
            impactSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0.5f), 16f);
        }

        var effect = new GameObject("Impacto visual");
        effect.transform.SetParent(transform, false);
        impact = effect.AddComponent<SpriteRenderer>();
        impact.sprite = impactSprite;
        impact.sortingLayerID = spriteRenderer.sortingLayerID;
        impact.sortingOrder = spriteRenderer.sortingOrder + 1;
        effect.SetActive(false);
    }
}
