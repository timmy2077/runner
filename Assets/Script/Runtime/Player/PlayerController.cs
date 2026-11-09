using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("基础移动（人生旅程中由阶段配置覆盖）")]
    public float WaitTime = 3;
    public float moveSpeed = 3;
    public float sprintSpeed = 10;
    public float jumpForce = 9;
    public float doubleJumpSpeed = 8;
    public float DelayTimeBeforeMove = 1;
    public LayerMask groundLayer;
    [Header("输入宽容")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    [HideInInspector]
    public int damage = 1;
    [HideInInspector]
    public int energy;
    [HideInInspector]
    public TextMeshProUGUI damageText, energyText, healthText;
    public TextMeshPro lootText;
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public LifeStageProfile Profile { get; private set; }
    public float CurrentSpeed => stopped || WaitingForBoss ? 0 : (sprinting ? sprintSpeed : moveSpeed);

    bool WaitingForBoss => Journey != null && Journey.Encounter != null && transform.position.x >= Journey.Encounter.MovementLimit;
    public float Gravity => Mathf.Abs(Physics2D.gravity.y * body.gravityScale);

    Rigidbody2D body;
    CapsuleCollider2D capsule;
    Animator animator;
    Vector2 standingSize, standingOffset;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    bool stopped, sprinting, canDoubleJump, protectedOnce;
    bool touchStop, touchSprint, touchSlide, touchJump, touchAttack;
    float groundedAt = -100, jumpRequestedAt = -100, readyAt, invulnerableUntil;
    bool jumpedSinceGrounded;
    float nextAttackAt;
    LifeJourneySession Journey => LifeJourneySession.Instance;

    bool Active => Journey == null || !Journey.IsStageScene || Journey.IsPlaying;

    void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        standingSize = capsule.size;
        standingOffset = capsule.offset;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        Profile = Journey != null && Journey.IsStageScene ? Journey.Profile : null;
        if (Profile != null)
        {
            moveSpeed = Profile.moveSpeed;
            sprintSpeed = Profile.canSprint ? Profile.sprintSpeed : Profile.moveSpeed;
            jumpForce = Profile.jumpForce;
            doubleJumpSpeed = Profile.canDoubleJump ? Profile.doubleJumpForce : 0;
            readyAt = Time.time;
        }
        else
            readyAt = Time.time + DelayTimeBeforeMove;
        if (groundLayer.value == 0)
            groundLayer = 1 << 9;
    }

    void OnEnable()
    {
        InventoryManager.OnLootTextChanged += OnLoot;
    }

    void OnDisable()
    {
        InventoryManager.OnLootTextChanged -= OnLoot;
        ClearInput();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!Active)
        {
            ClearInput();
            return;
        }

        stopped = touchStop || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.DownArrow) || Time.time < readyAt;
        sprinting = (Profile == null || Profile.canSprint) && !stopped && (touchSprint || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        if (Input.GetKeyDown(KeyCode.Space) || touchJump)
        {
            jumpRequestedAt = Time.time;
            touchJump = false;
        }

        if (touchAttack || Input.GetKey(KeyCode.J))
            TryAttack();
        if (WaitingForBoss)
            Journey.UI.SetHazardHint("心魔仍挡在前方 · 按住 J 攻击，留意脚下的预警", 0.6f);
        bool crawl = Profile != null && Profile.stage == LifeStage.Infancy;
        SetSlide(crawl || ((Profile == null || Profile.canSlide) && IsGrounded && (touchSlide || Input.GetKey(KeyCode.C))));
        if (animator != null)
            animator.SetBool("IsRun", !stopped && !crawl);
    }

    void FixedUpdate()
    {
        if (!Active)
            return;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        int hitCount = capsule.Cast(Vector2.down, filter, groundHits, 0.08f);
        IsGrounded = false;
        for (int i = 0; i < hitCount; i++)
            if (groundHits[i].normal.y > 0.5f)
            {
                IsGrounded = true;
                break;
            }

        if (IsGrounded && body.velocity.y <= 0.1f)
        {
            groundedAt = Time.time;
            jumpedSinceGrounded = false;
            canDoubleJump = Profile == null || Profile.canDoubleJump;
        }

        body.velocity = new Vector2(CurrentSpeed, body.velocity.y);
        if (jumpForce > 0 && Time.time - jumpRequestedAt <= jumpBufferTime)
        {
            if (!jumpedSinceGrounded && Time.time - groundedAt <= coyoteTime)
            {
                SetSlide(false);
                body.velocity = new Vector2(CurrentSpeed, jumpForce);
                jumpedSinceGrounded = true;
                groundedAt = -100;
                jumpRequestedAt = -100;
            }
            else if (!IsGrounded && canDoubleJump && doubleJumpSpeed > 0)
            {
                body.velocity = new Vector2(CurrentSpeed, doubleJumpSpeed);
                canDoubleJump = false;
                jumpRequestedAt = -100;
            }
        }

        if (Journey != null && Journey.Course != null && transform.position.y < Journey.Course.GroundTop - 7)
            HitHazard();
    }

    void SetSlide(bool slide)
    {
        if (slide == IsSliding)
            return;
        IsSliding = slide;
        capsule.size = slide ? new Vector2(standingSize.x, standingSize.y * 0.5f) : standingSize;
        capsule.offset = slide ? standingOffset + Vector2.down * standingSize.y * 0.25f : standingOffset;
        if (animator != null)
            animator.SetBool("IsSlide", slide);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Hole"))
            HitHazard();
    }

    public void HitHazard()
    {
        if (!Active || Time.time < invulnerableUntil)
            return;
        if (protectedOnce)
        {
            protectedOnce = false;
            invulnerableUntil = Time.time + 1.5f;
            if (healthText != null)
                healthText.text = "×1";
            return;
        }

        if (Journey != null && Journey.IsStageScene)
            Journey.Die();
        else if (GameOverUI.Instance != null)
            GameOverUI.Instance.GameOver();
    }

    public void SetTouchStop(bool value)
    {
        touchStop = value;
    }

    public void SetTouchSprint(bool value)
    {
        touchSprint = value;
    }

    public void SetTouchSlide(bool value)
    {
        touchSlide = value;
    }

    public void SetTouchAttack(bool value)
    {
        touchAttack = value;
    }

    public void RequestJump()
    {
        if (Active)
            touchJump = true;
    }

    public void TryAttack()
    {
        if (!Active || Journey == null || Journey.Boss == null || !Journey.Boss.CanTakeHit || Time.time < nextAttackAt)
            return;
        nextAttackAt = Time.time + Journey.Settings.attackCooldown;
        var shot = new GameObject("Attack");
        shot.transform.position = transform.position + Vector3.up * 0.2f;
        shot.AddComponent<JourneyProjectile>().Initialize(Journey.Boss, Journey.Settings.projectileSprite);
    }

    public void ClearInput()
    {
        touchStop = touchSprint = touchSlide = touchJump = touchAttack = false;
        stopped = true;
        sprinting = false;
        jumpRequestedAt = -100;
    }

    public void FreezeBody()
    {
        ClearInput();
        body.velocity = Vector2.zero;
    }

    public void UpdateSpeed(float amount)
    {
        // Pickups cannot undo aging or the six-stage speed curve.
        if (Profile == null)
            moveSpeed = Mathf.Max(0.1f, moveSpeed + amount);
    }

    public void SetProtected(bool value)
    {
        protectedOnce = value;
        if (healthText != null)
            healthText.text = value ? "×2" : "×1";
    }

    public void UpdateDamage(int amount)
    {
        if (Profile == null)
            damage += amount;
    }

    public void UpdateEnergy(int amount)
    {
        energy += amount;
        if (energyText != null)
            energyText.text = "×" + energy;
    }

    void OnLoot(LootSO loot)
    {
        if (lootText == null || loot == null)
            return;
        lootText.text = loot.lootText;
        StartCoroutine(HideLoot());
    }

    IEnumerator HideLoot()
    {
        lootText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2);
        if (lootText != null)
            lootText.gameObject.SetActive(false);
    }
}
