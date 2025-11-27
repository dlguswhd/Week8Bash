using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float movePower = 5f;
    public float jumpPower = 15f;

    [Header("슬라이딩 설정")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.3f;
    public float slideCooldown = 0.5f;

    [Header("공격 설정")]
    public float attackDelay = 0.4f;     
    public float dashAttackDuration = 0.5f; 
    public float dashAttackSpeed = 10f;     

    [Header("공격 판정(Hitbox)")]
    public Transform attackPos;   
    public Vector2 attackBoxSize; 
    public LayerMask enemyLayer;  
    public int damage = 20;
    public Vector2 normalAttackSize = new Vector2(1f, 1f); 
    public Vector2 dashAttackSize = new Vector2(2f, 1f);  

    [Header("상태 확인")]
    public bool isJumping = false;
    public bool isSliding = false;
    public bool isAttacking = false;
    public bool isDashAttacking = false; 
    public bool canSlide = true;

    [Header("체력 설정")]
    public int maxHp = 100; 
    public int currentHp;  

    [Header("능력치 (나중에 아이템 먹으면 올라감)")]
    public int defense = 0; 

    [Header("UI 연결")]
    public Text hpText;   
    public Text atkText;  
    public Text defText;  

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;

    Vector2 originalSize;
    Vector2 originalOffset;
    Vector3 defaultScale;

    Coroutine currentSlideRoutine;

    int playerLayerNum;
    int enemyLayerNum;
    void Start()
    {
        defaultScale = transform.localScale;
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        playerLayerNum = LayerMask.NameToLayer("Player");
        enemyLayerNum = LayerMask.NameToLayer("Enemy");
        currentHp = maxHp;
        UpdateStatusUI();
        Physics2D.IgnoreLayerCollision(playerLayerNum, enemyLayerNum, false);

        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;
        }
        // 초기화
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("DashAttack");
        anim.SetBool("IsJumping", false);
    }

    void UpdateStatusUI()
    {
        if (hpText != null) hpText.text = "HP : " + currentHp + " / " + maxHp;

        if (atkText != null) atkText.text = "ATK : " + damage;

        if (defText != null) defText.text = "DEF : " + defense;
    }

    public void TakeDamage(int dmg)
    {
        if (isSliding) return;

        float damageMultiplier = (100f - defense) / 100f;
        int finalDamage = (int)(dmg * damageMultiplier);

        if (finalDamage < 1) finalDamage = 1;

        currentHp -= finalDamage;
        Debug.Log("받은 데미지: " + finalDamage + " (방어력 적용됨)");

        StartCoroutine(OnDamageEffect());
        UpdateStatusUI(); 

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        Debug.Log("④ [Player] 힐 함수 도착! 현재 체력: " + currentHp);

        if (currentHp >= maxHp)
        {
            Debug.Log("⑤ [Player] 이미 풀피라서 회복 안 함 (현재: " + currentHp + ")");
            return;
        }

        if (currentHp <= 0) return;

        currentHp += amount;
        if (currentHp > maxHp) currentHp = maxHp;

        Debug.Log("⑤ [Player] 체력 증가함! 변경된 값: " + currentHp);

        UpdateStatusUI();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking && !isDashAttacking)
        {
            if (isSliding)
            {
                StartCoroutine(DashAttackRoutine());
            }
            else
            {
                StartCoroutine(AttackRoutine());
            }
        }

        if (Input.GetButtonDown("Jump") && !isJumping && !isSliding && !isAttacking && !isDashAttacking)
        {
            isJumping = true;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            anim.SetBool("IsJumping", true);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && canSlide && !isJumping && !isAttacking && !isDashAttacking)
        {
            currentSlideRoutine = StartCoroutine(SlideRoutine());
        }
    }

    void FixedUpdate()
    {
        if (isDashAttacking)
        {
            float direction = transform.localScale.x;
            rigid.linearVelocity = new Vector2(direction * dashAttackSpeed, rigid.linearVelocity.y);
            return;
        }

        if (isSliding || isAttacking)
        {
            if (isAttacking && !isJumping)
                rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            return;
        }

        Move();
    }
    IEnumerator DashAttackRoutine()
    {
        if (currentSlideRoutine != null) StopCoroutine(currentSlideRoutine);

        isSliding = false;
        isDashAttacking = true;

        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }

        anim.SetTrigger("DashAttack");

        CheckAttackHit(dashAttackSize, damage);

        yield return new WaitForSeconds(dashAttackDuration);

        isDashAttacking = false;

        canSlide = true;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger("Attack");

        CheckAttackHit(normalAttackSize, damage);

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
    }


    void CheckAttackHit(Vector2 boxSize, int atkDamage)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPos.position, boxSize, 0, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(atkDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPos.position, normalAttackSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(attackPos.position, dashAttackSize);
    }

    void Move()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        rigid.linearVelocity = new Vector2(xInput * movePower, rigid.linearVelocity.y);

        if (xInput != 0)
        {
            anim.SetBool("IsWalking", true);

            if (xInput > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(defaultScale.x), defaultScale.y, defaultScale.z);
            }
            else
            {
                transform.localScale = new Vector3(-Mathf.Abs(defaultScale.x), defaultScale.y, defaultScale.z);
            }
        }
        else
        {
            anim.SetBool("IsWalking", false);
        }
    }

    IEnumerator SlideRoutine()
    {
        isSliding = true;
        canSlide = false;

        Physics2D.IgnoreLayerCollision(playerLayerNum, enemyLayerNum, true);


        float direction = transform.localScale.x;
        if (Input.GetAxisRaw("Horizontal") != 0)
            direction = Input.GetAxisRaw("Horizontal");

        anim.SetTrigger("Slide");

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            boxCollider.offset = new Vector2(originalOffset.x, originalOffset.y - (originalSize.y * 0.25f));
        }

        rigid.linearVelocity = new Vector2(direction * slideSpeed, rigid.linearVelocity.y);

        yield return new WaitForSeconds(slideDuration);

        Physics2D.IgnoreLayerCollision(playerLayerNum, enemyLayerNum, false);

        isSliding = false;
        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }

        yield return new WaitForSeconds(slideCooldown);
        canSlide = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.7f)
        {
            isJumping = false;
            anim.SetBool("IsJumping", false);
        }
    }


    void Die()
    {
        Debug.Log("플레이어 사망...");
        anim.SetTrigger("Die"); 
        rigid.linearVelocity = Vector2.zero;       
        rigid.bodyType = RigidbodyType2D.Kinematic; 

        GetComponent<Collider2D>().enabled = false; 
        this.enabled = false; 
        rigid.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false; 
    }

    IEnumerator OnDamageEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }


}