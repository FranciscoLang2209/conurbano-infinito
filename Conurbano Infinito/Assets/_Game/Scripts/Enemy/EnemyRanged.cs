using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(SpriteRenderer))]
public sealed class EnemyRanged : MonoBehaviour
{
    public static event Action WarningStarted;
    [SerializeField] private EnemyStone stonePrefab;
    [SerializeField, Min(0.1f)] private float movementSpeed = 1.3f;
    [SerializeField, Min(0.1f)] private float attackDistance = 5.5f;
    [SerializeField, Min(0.1f)] private float warningSeconds = 0.9f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 3.5f;

    private Rigidbody2D body;
    private Health health;
    private Health target;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer warning;
    private Animator animator;
    private ObjectPool pool;
    private Vector2 aimDirection;
    private float fireTime;
    private float nextAttackTime;
    private static Sprite warningSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (stonePrefab == null)
        {
            Debug.LogError("EnemyRanged requiere un proyectil de gomera.", this);
            enabled = false;
            return;
        }
        var line = new GameObject("Trayectoria de gomera");
        line.transform.SetParent(transform, false);
        warning = line.AddComponent<SpriteRenderer>();
        if (warningSprite == null)
            warningSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        warning.sprite = warningSprite;
        warning.sortingOrder = spriteRenderer.sortingOrder + 1;
        line.SetActive(false);
    }

    private void OnEnable()
    {
        spriteRenderer.flipX = false;
        nextAttackTime = Time.time + cooldownSeconds;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        CancelWarning();
        target = newTarget != null ? newTarget.GetComponent<Health>() : null;
        pool = TryGetComponent(out PooledObject pooled) ? pooled.Owner : null;
        nextAttackTime = Time.time + cooldownSeconds;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
        body.linearVelocity = Vector2.zero;
        if (health.IsDead || target == null || target.IsDead || !target.gameObject.activeInHierarchy)
        {
            CancelWarning();
            return;
        }

        if (warning.gameObject.activeSelf)
        {
            float progress = 1f - (fireTime - Time.time) / warningSeconds;
            warning.color = new Color(0.89f, 0.37f, 0.3f, Mathf.Lerp(0.3f, 0.9f, progress));
            if (Time.time < fireTime) return;
            Vector3 origin = body.position + aimDirection * 0.45f;
            GameObject stone = pool != null
                ? pool.Spawn(stonePrefab.gameObject, origin, Quaternion.identity)
                : Instantiate(stonePrefab.gameObject, origin, Quaternion.identity);
            stone.GetComponent<EnemyStone>().Initialize(health, target, aimDirection);
            CancelWarning();
            nextAttackTime = Time.time + cooldownSeconds;
            if (animator != null) animator.SetInteger("RangedPhase", 2);
            return;
        }

        Vector2 offset = (Vector2)target.transform.position - body.position;
        if (Mathf.Abs(offset.x) > 0.01f) spriteRenderer.flipX = offset.x < 0f;
        if (offset.sqrMagnitude > attackDistance * attackDistance)
        {
            if (animator != null) animator.SetInteger("RangedPhase", 0);
            body.MovePosition(body.position + offset.normalized * movementSpeed * Time.fixedDeltaTime);
            return;
        }
        if (Time.time < nextAttackTime || offset.sqrMagnitude < 0.0001f) return;

        aimDirection = offset.normalized;
        fireTime = Time.time + warningSeconds;
        warning.transform.position = body.position + aimDirection * 4.25f;
        warning.transform.rotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);
        warning.transform.localScale = new Vector3(8.5f, 0.16f, 1f);
        warning.color = new Color(0.89f, 0.37f, 0.3f, 0.3f);
        warning.gameObject.SetActive(true);
        if (animator != null) animator.SetInteger("RangedPhase", 1);
        WarningStarted?.Invoke();
    }

    private void CancelWarning()
    {
        if (warning != null) warning.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelWarning();
        target = null;
        pool = null;
        body.linearVelocity = Vector2.zero;
    }
}
