using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Transform playerVisual;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] jumpFrames;
    [SerializeField] private Sprite[] fallFrames;
    [SerializeField] private Sprite[] hurtFrames;
    [SerializeField] private Sprite[] emptyFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float runFps = 12f;
    [SerializeField] private float jumpFps = 8f;
    [SerializeField] private float fallFps = 8f;
    [SerializeField] private float hurtFps = 8f;
    [SerializeField] private float emptyFps = 6f;
    [SerializeField] private float attackFps = 12f;
    [SerializeField] private float groundCheckDistance = 0.18f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerEnergyStore energyStore;
    private string currentState;
    private float stateTimer;
    private bool attackPlaying;
    private bool hurtPlaying;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (bodyCollider == null || !bodyCollider.enabled)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        energyStore = GetComponent<PlayerEnergyStore>();
        ResolveVisualReferences();

        SpriteRenderer placeholderRenderer = GetComponent<SpriteRenderer>();
        if (placeholderRenderer != null)
        {
            placeholderRenderer.enabled = false;
        }
    }

    private void Update()
    {
        ResolveVisualReferences();
        if (visualRenderer == null)
        {
            return;
        }

        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        bool grounded = IsGrounded();
        bool empty = energyStore != null && energyStore.currentEnergy <= 0f;

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(velocity.x));
            animator.SetFloat("VerticalVelocity", velocity.y);
            animator.SetBool("IsGrounded", grounded);
            animator.SetBool("IsEmpty", empty);
        }

        UpdateFacing(velocity.x);
        UpdateState(velocity, grounded, empty);
    }

    public void PlayAttack()
    {
        attackPlaying = true;
        hurtPlaying = false;
        SetState("Attack", true);
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void PlayHurt()
    {
        hurtPlaying = true;
        attackPlaying = false;
        SetState("Hurt", true);
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }
    }

    private void ResolveVisualReferences()
    {
        if (playerVisual == null)
        {
            Transform existing = transform.Find("PlayerVisual");
            if (existing != null)
            {
                playerVisual = existing;
            }
        }

        if (visualRenderer == null && playerVisual != null)
        {
            visualRenderer = playerVisual.GetComponent<SpriteRenderer>();
        }

        if (animator == null && playerVisual != null)
        {
            animator = playerVisual.GetComponent<Animator>();
        }
    }

    private void UpdateState(Vector2 velocity, bool grounded, bool empty)
    {
        if (empty)
        {
            PlayLoop("Empty", emptyFrames, emptyFps);
            attackPlaying = false;
            hurtPlaying = false;
            return;
        }

        if (hurtPlaying)
        {
            if (PlayOnce("Hurt", hurtFrames, hurtFps))
            {
                hurtPlaying = false;
            }

            return;
        }

        if (attackPlaying)
        {
            if (PlayOnce("Attack", attackFrames, attackFps))
            {
                attackPlaying = false;
            }

            return;
        }

        if (!grounded && velocity.y > 0.1f)
        {
            PlayLoop("Jump", jumpFrames, jumpFps);
        }
        else if (!grounded && velocity.y < -0.1f)
        {
            PlayLoop("Fall", fallFrames, fallFps);
        }
        else if (Mathf.Abs(velocity.x) > 0.1f)
        {
            PlayLoop("Run", runFrames, runFps);
        }
        else
        {
            PlayLoop("Idle", idleFrames, idleFps);
        }
    }

    private void PlayLoop(string state, Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        SetState(state, false);
        int index = Mathf.FloorToInt(stateTimer * Mathf.Max(1f, fps)) % frames.Length;
        visualRenderer.sprite = frames[index];
    }

    private bool PlayOnce(string state, Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0)
        {
            return true;
        }

        SetState(state, false);
        int index = Mathf.FloorToInt(stateTimer * Mathf.Max(1f, fps));
        if (index >= frames.Length)
        {
            visualRenderer.sprite = frames[frames.Length - 1];
            return true;
        }

        visualRenderer.sprite = frames[index];
        return false;
    }

    private void SetState(string state, bool forceRestart)
    {
        if (forceRestart || currentState != state)
        {
            currentState = state;
            stateTimer = 0f;
        }
        else
        {
            stateTimer += Time.deltaTime;
        }
    }

    private bool IsGrounded()
    {
        if (bodyCollider == null)
        {
            return true;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(groundLayer);
        return bodyCollider.Cast(Vector2.down, filter, groundHits, groundCheckDistance) > 0;
    }

    private void UpdateFacing(float velocityX)
    {
        if (playerVisual == null || Mathf.Abs(velocityX) <= 0.05f)
        {
            return;
        }

        float desiredWorldSign = velocityX >= 0f ? 1f : -1f;
        float parentSign = Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Sign(transform.localScale.x) : 1f;
        Vector3 scale = playerVisual.localScale;
        scale.x = Mathf.Abs(scale.x) * desiredWorldSign / parentSign;
        playerVisual.localScale = scale;
    }
}
