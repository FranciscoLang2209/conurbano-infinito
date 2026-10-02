using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public sealed class EnemyVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Transform target;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        spriteRenderer.flipX = false;
        animator.Rebind();
        animator.Update(0f);
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    private void Update()
    {
        if (target == null) return;
        float horizontal = target.position.x - transform.position.x;
        if (Mathf.Abs(horizontal) > 0.01f) spriteRenderer.flipX = horizontal < 0f;
    }
}
