using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpImpulse = 11f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D body;
    private PhysicsMaterial2D frictionlessMaterial;
    private float moveInput;
    private bool jumpRequested;
    private bool facingRight = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ApplyFrictionlessColliderMaterial();
    }

    private void Update()
    {
        ReadKeyboard();

        if (moveInput > 0.01f)
        {
            SetFacingRight(true);
        }
        else if (moveInput < -0.01f)
        {
            SetFacingRight(false);
        }
    }

    private void FixedUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        body.linearVelocity = velocity;

        if (jumpRequested && IsGrounded())
        {
            body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        }

        jumpRequested = false;
    }

    private void ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            moveInput = 0f;
            return;
        }

        moveInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            moveInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            moveInput += 1f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody != body)
            {
                return true;
            }
        }

        return false;
    }

    private void SetFacingRight(bool shouldFaceRight)
    {
        if (facingRight == shouldFaceRight)
        {
            return;
        }

        facingRight = shouldFaceRight;

        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        visualRoot.localScale = scale;
    }

    private void ApplyFrictionlessColliderMaterial()
    {
        frictionlessMaterial = new PhysicsMaterial2D("PlayerFrictionless")
        {
            friction = 0f,
            bounciness = 0f
        };

        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D playerCollider in colliders)
        {
            playerCollider.sharedMaterial = frictionlessMaterial;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
