using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

// 角色控制器
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    
    [Header("移动设置")]
    public float WaitTime = 3f; // 等待时间
    public float moveSpeed; // 固定前进速度（跑酷核心）
    public float sprintSpeed = 12f; // 冲刺速度
    public float jumpForce = 6f;
    public float doubleJumpSpeed = 4f;
    private bool canDoubleJump;
    public float DelayTimeBeforeMove = 5f; // 初始等待时间（秒）
    
    [Header("战斗设置")]
    public int damage = 1; // 基础伤害
    public TextMeshProUGUI damageText; // 伤害文本
    public int energy = 0; // 能量值
    public TextMeshProUGUI energyText; // 能量文本

    public TextMeshProUGUI healthText; // 生命值文本
    public TMPro.TextMeshPro lootText; // 3D货物文本
    
    private Rigidbody2D rb;
    private bool isGrounded; // 是否在地面
    private bool isSliding; // 是否滑铲
    private float slideTimer; // 滑铲计时器
    public LayerMask groundLayer;
    private Animator myAnim;
    private bool isStopped; // 是否停止移动
    private bool isSprinting; // 是否在冲刺
    private bool isProtected = false; // 是否受保护
    private bool isSlideRequested = false; // 是否请求滑铲
    private float gameStartTime; // 游戏开始时间
    private bool isInitialWait = true; // 是否处于初始等待状态

    [Header("脚步声")]
    private float footstepTimer = 0f;
    private float footstepInterval = 0.3f; // 脚步声间隔
    private float sprintFootstepInterval = 0.2f; // 冲刺时的脚步声间隔

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        InventoryManager.OnLootTextChanged += HandleLootTextChanged;
    }

    void OnDisable()
    {
        InventoryManager.OnLootTextChanged -= HandleLootTextChanged;
    }

    void HandleLootTextChanged(LootSO lootSO)
    {
        lootText.text = lootSO.lootText;
        StartCoroutine(HideLootText());

    }

    IEnumerator HideLootText()
    {
        lootText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        lootText.gameObject.SetActive(false);
    }

    // 初始化
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myAnim = GetComponentInChildren<Animator>();
        canDoubleJump = false;
        gameStartTime = Time.time;
        isStopped = true; // 初始状态为停止
    }

    // 每帧更新（处理输入）
    void Update()
    {
        // 检测地面（射线检测，避免浮空判定）
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);

        // 检查是否度过了初始5秒等待期
        if (Time.time - gameStartTime >= DelayTimeBeforeMove && isInitialWait)
        {
            myAnim.SetBool("IsRun", true);
            isInitialWait = false;
            isStopped = false; // 5秒后开始移动
        }

        // 初始5秒内禁用输入
        if (isInitialWait)
        {
            return;
        }

        // 停止/继续移动（Z键）
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isStopped = true;
        }
        if (Input.GetKeyUp(KeyCode.Z))
        {
            isStopped = false;
        }

        // 冲刺（Shift键）
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            isSprinting = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            isSprinting = false;
        }

        // 跳跃（支持二段跳）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Debug.Log("跳跃！isGrounded: " + isGrounded);
            Jump();

            // 如果正在滑铲，结束滑铲
            if (isSliding)
            {
                EndSlide();
                myAnim.Play("Up");
            }  
        }
        
        // 按下C键请求滑铲
        if (Input.GetKeyDown(KeyCode.C) && !isSliding)
        {
            isSlideRequested = true;
        }
        // 松开C键取消滑铲请求
        if (Input.GetKeyUp(KeyCode.C))
        {
            isSlideRequested = false;
            if (isSliding)
            {
                EndSlide();
            }
        }
        // 如果请求了滑铲且在地面上，开始滑铲
        if (isSlideRequested && isGrounded && !isSliding)
        {
            StartSlide();
            isSlideRequested = false;
        }
    }

    // 物理更新（固定前进）
    void FixedUpdate()
    {
  
        // 计算当前速度
        float currentSpeed = isStopped ? 0f : (isSprinting ? sprintSpeed : moveSpeed);
        rb.velocity = new Vector2(currentSpeed, rb.velocity.y);
        // 脚步声
        if (isGrounded && !isSliding && !isStopped && currentSpeed > 0)
        {
            float interval = isSprinting ? sprintFootstepInterval : footstepInterval;
            footstepTimer += Time.fixedDeltaTime;
            if (footstepTimer >= interval)
            {
                // SoundManager.Instance.Play("Foot");
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f; // 重置计时器
        }
    }



    // 开始滑铲
    void StartSlide()
    {
        isSliding = true;
        // 缩小碰撞体（模拟滑铲低头）
        myAnim.SetBool("IsSlide", true);
        CapsuleCollider2D coll = GetComponent<CapsuleCollider2D>();
        coll.size = new Vector2(coll.size.x, coll.size.y / 2);
        coll.offset = new Vector2(coll.offset.x, -coll.size.y / 2);
    }

    // 结束滑铲
    void EndSlide()
    {
        isSliding = false;
        myAnim.SetBool("IsSlide", false);
        // 恢复碰撞体
        CapsuleCollider2D coll = GetComponent<CapsuleCollider2D>();
        coll.size = new Vector2(coll.size.x, coll.size.y * 2);
        coll.offset = new Vector2(coll.offset.x, 0);
    }

    void Jump()
{
    if(isGrounded)
    {
        // myAnim.SetBool("Jump", true);
        float currentSpeed = isStopped ? 0f : (isSprinting ? sprintSpeed : moveSpeed);
        rb.velocity = new Vector2(currentSpeed, jumpForce);
        canDoubleJump = true;
    }
    else
    {
        if(canDoubleJump)
        {
            // myAnim.SetBool("DoubleJump", true);
            float currentSpeed = isStopped ? 0f : (isSprinting ? sprintSpeed : moveSpeed);
            rb.velocity = new Vector2(currentSpeed, doubleJumpSpeed);
            canDoubleJump = false;
        }
    }
}

    // 碰撞检测（撞到障碍物失败）
    void OnCollisionEnter2D(Collision2D other)
    {
        if ((other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("Hole")) && !isProtected)
        {
            healthText.text = "×0";
            GameOverUI.Instance.GameOver(); // 调用游戏的失败逻辑
            Time.timeScale = 0;
            
        }
        else if (other.gameObject.CompareTag("Obstacle") && isProtected)
        {
            isProtected = false;
            healthText.text = "×1";
            StartCoroutine(DoBlinks());
            Destroy(other.gameObject);
        }
        else if (other.gameObject.CompareTag("Hole") && isProtected)
        {
            // 找到Hole组件并获取revivePoint
            Hole hole = other.gameObject.GetComponent<Hole>();
            if (hole != null && hole.revivePoint != null)
            {
                transform.position = hole.revivePoint.position;
                isProtected = false;
                healthText.text = "×1";
                StartCoroutine(DoBlinks());
            }
        }

    }

    // 绘制射线（调试用）
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * 0.6f);
    }

    public void UpdateSpeed(float speedBuff)
    {
        moveSpeed += speedBuff;
        Debug.Log("moveSpeed: " + moveSpeed);
    }

    public void SetProtected(bool isProtected)
    {
        this.isProtected = isProtected;
        healthText.text = "×2";
    }
    
    public void UpdateDamage(int damageBuff)
    {
        damage += damageBuff;
        if (damageText != null)
        {
            damageText.text = "×" + damage.ToString();
        }
    }
    
    
    public void UpdateEnergy(int energyBuff)
    {
        energy += energyBuff;
        if (energyText != null)
        {
            energyText.text = "×" + energy.ToString();
        }
    }

        IEnumerator DoBlinks()
    {
        Debug.Log("DoBlinks");
        // 获取所有Sprite渲染器
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No SpriteRenderer found");
            yield break;
        }
        
        for(int i = 0; i < 4; i++)  // 循环次数是闪烁次数的2倍（开关各一次）
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;  // 切换渲染器启用状态
                }
            }
            yield return new WaitForSeconds(0.1f);  // 等待指定时间
        }
        
        // 确保最后恢复为可见状态
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }
}