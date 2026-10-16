using System;
using UnityEngine;

public sealed class BossAttack : MonoBehaviour
{
    public static event Action WarningStarted;
    [SerializeField, Min(1)] private int damage = 2;
    [SerializeField, Min(0.1f)] private float attackIntervalSeconds = 5f;
    [SerializeField, Min(0.1f)] private float warningSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float attackHalfSize = 1.3f;

    private Transform target;
    private Health health;
    private Health targetHealth;
    private GameObject warning;
    private Vector2 strikePosition;
    private float strikeTime;
    private float nextAttackTime;
    private static Sprite warningSprite;

    private void Awake() => health = GetComponent<Health>();

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponent<Health>() : null;
        nextAttackTime = Time.time + attackIntervalSeconds;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (target == null || targetHealth == null || targetHealth.IsDead ||
            (health != null && health.IsDead))
        {
            if (warning != null) Destroy(warning);
            warning = null;
            return;
        }

        if (warning != null)
        {
            if (Time.time < strikeTime) return;
            Vector2 offset = (Vector2)target.position - strikePosition;
            if (Mathf.Abs(offset.x) <= attackHalfSize && Mathf.Abs(offset.y) <= attackHalfSize)
            {
                targetHealth.TakeDamage(damage);
            }
            Destroy(warning);
            warning = null;
            nextAttackTime = Time.time + attackIntervalSeconds;
            return;
        }

        if (Time.time < nextAttackTime) return;
        strikePosition = target.position;
        strikeTime = Time.time + warningSeconds;
        warning = new GameObject("Aviso de ataque de La Jefa");
        warning.transform.position = strikePosition;
        warning.transform.localScale = Vector3.one * attackHalfSize * 2f;
        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        if (warningSprite == null)
        {
            warningSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), 2f);
        }
        renderer.sprite = warningSprite;
        renderer.color = new Color(1f, 0.15f, 0.1f, 0.45f);
        renderer.sortingOrder = 1;
        WarningStarted?.Invoke();
    }

    private void OnDisable()
    {
        if (warning != null) Destroy(warning);
        warning = null;
    }
}
