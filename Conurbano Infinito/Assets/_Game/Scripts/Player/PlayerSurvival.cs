using UnityEngine;

[RequireComponent(typeof(Health), typeof(PlayerController), typeof(SpriteRenderer))]
public sealed class PlayerSurvival : MonoBehaviour
{
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField, Min(0f)] private float feedbackSeconds = 0.2f;

    private Health health;
    private PlayerController movement;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private float feedbackEndTime;

    private void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        feedbackEndTime = 0f;
        spriteRenderer.color = normalColor;
        health.Damaged += ShowDamage;
        health.Died += ShowDefeat;
    }

    private void OnDisable()
    {
        health.Damaged -= ShowDamage;
        health.Died -= ShowDefeat;
        spriteRenderer.color = normalColor;
        feedbackEndTime = 0f;
    }

    private void Update()
    {
        if (feedbackEndTime > 0f && Time.unscaledTime >= feedbackEndTime)
        {
            spriteRenderer.color = normalColor;
            feedbackEndTime = 0f;
        }
    }

    private void ShowDamage()
    {
        spriteRenderer.color = damageColor;
        feedbackEndTime = Time.unscaledTime + feedbackSeconds;
    }

    private void ShowDefeat()
    {
        movement.enabled = false;
    }
}
