using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private Vector2 groundCheckSize = new(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;

    private bool isFacingRight = true;
    private bool isActive = false;
    private AudioSource walkAudio;

    private void Start()
    {
        // Som de andar
        walkAudio = AudioManager.Instance.PlayLoop("Andando", gameObject);
        if (walkAudio != null) walkAudio.Stop();
    }

    private void Update()
    {
        if (!IsOwner || !isActive) return; // Apenas o jogador local controla

        float moveX = moveAction.action.ReadValue<Vector2>().x;
        bool grounded = IsGrounded();

        // Áudio de movimento
        if (grounded && Mathf.Abs(moveX) > 0.1f)
        {
            if (!walkAudio.isPlaying) walkAudio.Play();
        }
        else if (walkAudio.isPlaying)
        {
            walkAudio.Stop();
        }

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (jumpAction.action.WasPerformedThisFrame() && grounded)
        {
            Jump();
        }

        Flip(moveX);
        HandleAnimations(moveX, grounded);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        AudioManager.Instance.PlaySound("Pulando");
    }

    private void Flip(float moveX)
    {
        if (moveX > 0 && !isFacingRight || moveX < 0 && isFacingRight)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1;
            transform.localScale = ls;
        }
    }

    private void HandleAnimations(float moveX, bool grounded)
    {
        anim.SetBool("isWalking", Mathf.Abs(moveX) > 0.1f && grounded);
        anim.SetBool("IsGrounded", grounded);
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0f, groundLayer);
    }

    public void StartTurn()
    {
        isActive = true;
        playerUI?.StartTurn();
    }

    public void EndTurn()
    {
        isActive = false;
        rb.linearVelocity = Vector2.zero;
        playerUI?.EndTurn();
        if (walkAudio != null && walkAudio.isPlaying)
            walkAudio.Stop();
    }
}
