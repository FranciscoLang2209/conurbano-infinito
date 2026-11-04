using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public sealed class ExperienceGem : MonoBehaviour
{
    public static event Action Collected;
    private static Sprite placeholderSprite;
    private long experienceValue;
    public long Value => experienceValue;
    private bool collected;

    private void Awake()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f), 16f);
            }
            spriteRenderer.sprite = placeholderSprite;
        }
    }

    public void Initialize(int value)
    {
        experienceValue = value;
    }

    internal void AddValue(int value) => experienceValue = checked(experienceValue + value);

    private void OnDisable()
    {
        experienceValue = 0;
        collected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Collect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Collect(other);
    }

    private void Collect(Collider2D other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player") ||
            !other.TryGetComponent(out ExperienceSystem experience))
        {
            return;
        }

        TryCollect(experience);
    }

    public void TryCollect(ExperienceSystem experience)
    {
        if (collected || experienceValue <= 0 || experience == null ||
            !experience.TryGetComponent(out Health health) || health.IsDead) return;

        collected = true;
        experience.AddExperience(experienceValue);
        Collected?.Invoke();
        ObjectPool.Release(gameObject);
    }
}
