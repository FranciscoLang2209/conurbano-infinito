using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ContactDamage : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.01f)] private float contactCooldownSeconds = 0.75f;

    private float nextDamageTime;
    private int playerLayer;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnDisable() => nextDamageTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer || Time.time < nextDamageTime ||
            !other.TryGetComponent(out Health targetHealth))
        {
            return;
        }

        if (targetHealth.TakeDamage(damage))
        {
            nextDamageTime = Time.time + contactCooldownSeconds;
        }
    }
}
