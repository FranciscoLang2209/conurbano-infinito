using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(SpriteRenderer))]
public sealed class EnemyCharge : MonoBehaviour
{
    public static event Action WarningStarted;
    [SerializeField, Min(0.1f)] private float movementSpeed = 1.7f;
    [SerializeField, Min(0.1f)] private float triggerDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float warningSeconds = 0.65f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 8f;
    [SerializeField, Min(0.1f)] private float chargeSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float recoverySeconds = 1.1f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 2f;

    private enum Phase { Chase, Warning, Charge, Recovery }
    private Phase phase;
    private Rigidbody2D body;
    private Health health;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer warning;
    private GameObject recoveryIndicator;
    private Transform target;
    private Health targetHealth;
    private Vector2 chargeDirection;
    private float phaseEndTime;
    private float nextChargeTime;
    private static Sprite warningSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        var indicator = new GameObject("Aviso de embestida");
        indicator.transform.SetParent(transform, false);
        warning = indicator.AddComponent<SpriteRenderer>();
        if (warningSprite == null)
            warningSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        warning.sprite = warningSprite;
        warning.color = new Color(0.95f, 0.77f, 0.28f, 0.5f);
        warning.sortingLayerID = spriteRenderer.sortingLayerID;
        warning.sortingOrder = spriteRenderer.sortingOrder - 1;
        indicator.SetActive(false);
        recoveryIndicator = new GameObject("Ventana de contraataque");
        recoveryIndicator.transform.SetParent(transform, false);
        recoveryIndicator.transform.localPosition = new Vector3(0f, -0.65f, 0f);
        recoveryIndicator.transform.localScale = new Vector3(0.8f, 0.12f, 1f);
        var recovery = recoveryIndicator.AddComponent<SpriteRenderer>();
        recovery.sprite = warningSprite;
        recovery.color = new Color32(97, 187, 211, 255);
        recovery.sortingLayerID = spriteRenderer.sortingLayerID;
        recovery.sortingOrder = spriteRenderer.sortingOrder + 1;
        recoveryIndicator.SetActive(false);
    }

    private void OnEnable()
    {
        phase = Phase.Chase;
        nextChargeTime = Time.time + cooldownSeconds;
        spriteRenderer.flipX = false;
    }

    private void OnDisable()
    {
        target = null;
        targetHealth = null;
        phase = Phase.Chase;
        body.linearVelocity = Vector2.zero;
        warning.gameObject.SetActive(false);
        recoveryIndicator.SetActive(false);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponent<Health>() : null;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
        body.linearVelocity = Vector2.zero;
        if (health.IsDead || target == null || (targetHealth != null && targetHealth.IsDead))
        {
            warning.gameObject.SetActive(false);
            recoveryIndicator.SetActive(false);
            phase = Phase.Chase;
            return;
        }

        if (phase == Phase.Warning)
        {
            warning.color = new Color(0.95f, 0.77f, 0.28f,
                Mathf.Lerp(0.25f, 0.8f, 1f - (phaseEndTime - Time.time) / warningSeconds));
            if (Time.time < phaseEndTime) return;
            warning.gameObject.SetActive(false);
            phase = Phase.Charge;
            phaseEndTime = Time.time + chargeSeconds;
        }

        if (phase == Phase.Charge)
        {
            if (Time.time < phaseEndTime)
            {
                body.MovePosition(body.position + chargeDirection * chargeSpeed * Time.fixedDeltaTime);
                return;
            }
            phase = Phase.Recovery;
            phaseEndTime = Time.time + recoverySeconds;
            recoveryIndicator.SetActive(true);
        }

        if (phase == Phase.Recovery)
        {
            if (Time.time < phaseEndTime) return;
            recoveryIndicator.SetActive(false);
            phase = Phase.Chase;
            nextChargeTime = Time.time + cooldownSeconds;
        }

        Vector2 offset = (Vector2)target.position - body.position;
        if (offset.sqrMagnitude < 0.0001f) return;
        Vector2 direction = offset.normalized;
        if (Mathf.Abs(direction.x) > 0.01f) spriteRenderer.flipX = direction.x < 0f;
        if (Time.time >= nextChargeTime && offset.sqrMagnitude <= triggerDistance * triggerDistance)
        {
            chargeDirection = direction;
            phase = Phase.Warning;
            phaseEndTime = Time.time + warningSeconds;
            float distance = chargeSpeed * chargeSeconds;
            warning.transform.position = body.position + direction * distance / 2f;
            warning.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            warning.transform.localScale = new Vector3(distance, 0.8f, 1f);
            warning.color = new Color(0.95f, 0.77f, 0.28f, 0.25f);
            warning.gameObject.SetActive(true);
            WarningStarted?.Invoke();
            return;
        }
        body.MovePosition(body.position + direction *
            Mathf.Min(offset.magnitude, movementSpeed * Time.fixedDeltaTime));
    }
}
