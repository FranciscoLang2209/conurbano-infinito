using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float movementSpeed = 5f;

    public float MovementSpeed => movementSpeed;

    public void IncreaseMovementSpeed(float multiplier)
    {
        if (multiplier > 1f)
        {
            movementSpeed *= multiplier;
        }
    }

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private InputAction moveAction;
    private Vector2 moveInput;
    private static Sprite placeholderSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        CreatePlaceholderSpriteIfNeeded();
        moveAction = inputActions != null ? inputActions.FindAction("Player/Move", false) : null;
        if (moveAction == null)
        {
            Debug.LogError("PlayerController requiere la acción Player/Move.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void Update()
    {
        moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        if (animator != null) animator.SetBool("Walking", moveInput.sqrMagnitude > 0f);
        if (Mathf.Abs(moveInput.x) > 0.01f) spriteRenderer.flipX = moveInput.x < 0f;
    }

    private void FixedUpdate()
    {
        body.linearVelocity = moveInput * movementSpeed;
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        if (animator != null) animator.SetBool("Walking", false);
        body.linearVelocity = Vector2.zero;
    }

    private void CreatePlaceholderSpriteIfNeeded()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite != null)
        {
            return;
        }

        if (placeholderSprite == null)
        {
            placeholderSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                2f);
        }
        spriteRenderer.sprite = placeholderSprite;
    }
}
