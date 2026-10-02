using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class Projectile : MonoBehaviour
{
    public static event Action Hit;
    private enum Flight { Straight, Returning, Orbiting }

    private readonly HashSet<Health> hitTargets = new();
    private readonly Dictionary<Health, float> nextOrbitHit = new();
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer visualRenderer;
    private Animator animator;
    private Sprite normalSprite;
    private Color normalColor;
    private Vector3 normalScale;
    private Quaternion normalRotation;
    private WeaponPattern visualPattern;
    private Flight flight;
    private Transform owner;
    private float damage;
    private int remainingHits;
    private float maximumDistanceSquared;
    private float speed;
    private float orbitAngle;
    private float orbitRadius;
    private float orbitInterval;
    private Vector2 startPosition;
    private bool initialized;
    private bool returning;
    private static Sprite placeholderSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
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
        normalSprite = spriteRenderer.sprite;
        normalColor = spriteRenderer.color;
        normalScale = transform.localScale;
        normalRotation = transform.localRotation;
        var visual = new GameObject("Visual del proyectil");
        visual.transform.SetParent(transform, false);
        visualRenderer = visual.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = normalSprite;
        visualRenderer.color = normalColor;
        visualRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        visualRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        visualRenderer.sortingOrder = spriteRenderer.sortingOrder;
        visualRenderer.flipX = spriteRenderer.flipX;
        visualRenderer.flipY = spriteRenderer.flipY;
        spriteRenderer.enabled = false;
    }

    private void OnDisable()
    {
        initialized = false;
        owner = null;
        returning = false;
        visualPattern = WeaponPattern.Straight;
        hitTargets.Clear();
        nextOrbitHit.Clear();
        if (body != null) body.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = normalSprite;
            spriteRenderer.color = normalColor;
        }
        if (visualRenderer != null)
        {
            visualRenderer.sprite = normalSprite;
            visualRenderer.color = normalColor;
            visualRenderer.transform.localRotation = Quaternion.identity;
            visualRenderer.transform.localScale = Vector3.one;
        }
        transform.localScale = normalScale;
        transform.localRotation = normalRotation;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        StopAllCoroutines();
    }

    public void SetAppearance(Sprite sprite, Color color, Vector2 scale,
        WeaponPattern pattern = WeaponPattern.Straight)
    {
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        visualRenderer.sprite = sprite;
        visualRenderer.color = color;
        visualRenderer.transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(scale.x, scale.y, 1f);
        transform.localRotation = normalRotation;
        visualPattern = pattern;
    }

    public void Initialize(Vector2 direction, float projectileDamage, float projectileSpeed, int pierce, float range)
    {
        flight = Flight.Straight;
        damage = projectileDamage;
        remainingHits = pierce + 1;
        maximumDistanceSquared = range * range;
        startPosition = body.position;
        body.linearVelocity = direction.normalized * projectileSpeed;
        if (visualPattern == WeaponPattern.Straight) RotateAlongVelocity();
        initialized = true;
    }

    public void InitializeReturning(Transform targetOwner, Vector2 direction, float projectileDamage,
        float projectileSpeed, float range)
    {
        flight = Flight.Returning;
        owner = targetOwner;
        damage = projectileDamage;
        remainingHits = int.MaxValue;
        speed = projectileSpeed;
        maximumDistanceSquared = range * range;
        startPosition = body.position;
        body.linearVelocity = direction.normalized * speed;
        initialized = true;
    }

    public void InitializeOrbiting(Transform targetOwner, float angle, float projectileDamage,
        float radius, float degreesPerSecond, float hitInterval)
    {
        flight = Flight.Orbiting;
        owner = targetOwner;
        orbitAngle = angle;
        orbitRadius = radius;
        speed = degreesPerSecond;
        damage = projectileDamage;
        orbitInterval = hitInterval;
        remainingHits = int.MaxValue;
        body.linearVelocity = Vector2.zero;
        body.position = OrbitPosition();
        initialized = true;
    }

    public void SetOrbitStats(float projectileDamage, float hitInterval, float degreesPerSecond)
    {
        damage = projectileDamage;
        orbitInterval = hitInterval;
        speed = degreesPerSecond;
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        if (flight == Flight.Orbiting)
        {
            if (owner == null)
            {
                ObjectPool.Release(gameObject);
                return;
            }

            orbitAngle += speed * Time.fixedDeltaTime;
            body.MovePosition(OrbitPosition());
        }
        else if (flight == Flight.Returning)
        {
            if (owner == null)
            {
                ObjectPool.Release(gameObject);
                return;
            }

            if (!returning && (body.position - startPosition).sqrMagnitude >= maximumDistanceSquared)
            {
                returning = true;
                hitTargets.Clear();
            }

            if (returning)
            {
                Vector2 toOwner = (Vector2)owner.position - body.position;
                if (toOwner.sqrMagnitude <= Mathf.Pow(speed * Time.fixedDeltaTime + 0.25f, 2f))
                {
                    ObjectPool.Release(gameObject);
                    return;
                }
                body.linearVelocity = toOwner.normalized * speed;
            }
        }
        else if ((body.position - startPosition).sqrMagnitude >= maximumDistanceSquared)
        {
            ObjectPool.Release(gameObject);
            return;
        }

        if (visualPattern == WeaponPattern.Straight) RotateAlongVelocity();
        else visualRenderer.transform.Rotate(0f, 0f,
            (visualPattern == WeaponPattern.Orbiting ? 360f : 540f) * Time.fixedDeltaTime);
    }

    private Vector2 OrbitPosition()
    {
        float radians = orbitAngle * Mathf.Deg2Rad;
        return (Vector2)owner.position + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
    }

    private void RotateAlongVelocity()
    {
        Vector2 velocity = body.linearVelocity;
        visualRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        if (flight == Flight.Orbiting && other.TryGetComponent(out Health target))
        {
            nextOrbitHit.Remove(target);
        }
    }

    private void TryHit(Collider2D other)
    {
        if (!initialized || other.gameObject.layer != LayerMask.NameToLayer("Enemy") ||
            !other.TryGetComponent(out Health target) || target.IsDead)
        {
            return;
        }

        if (flight == Flight.Orbiting)
        {
            if (nextOrbitHit.TryGetValue(target, out float nextTime) && Time.time < nextTime)
            {
                return;
            }
            if (target.TakeDamage(damage))
            {
                Hit?.Invoke();
                nextOrbitHit[target] = Time.time + orbitInterval;
                if (target.IsDead) nextOrbitHit.Remove(target);
            }
            return;
        }

        if (remainingHits <= 0 || !hitTargets.Add(target))
        {
            return;
        }

        if (!target.TakeDamage(damage))
        {
            return;
        }

        Hit?.Invoke();

        if (target.IsDead) hitTargets.Remove(target);

        if (flight == Flight.Straight && --remainingHits <= 0)
        {
            ObjectPool.Release(gameObject);
        }
    }
}
