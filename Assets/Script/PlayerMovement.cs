using UnityEngine;
using System.Collections;

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
    public float attackDelay = 0.4f;      // 일반 공격 딜레이
    public float dashAttackDuration = 0.5f; // 대쉬 공격 지속 시간 [NEW]
    public float dashAttackSpeed = 10f;     // 대쉬 공격 이동 속도 [NEW]

    [Header("공격 판정(Hitbox)")]
    public Transform attackPos;   // 공격 범위의 중심점 (오브젝트 연결 필요)
    public Vector2 attackBoxSize; // 공격 범위 크기 (가로, 세로)
    public LayerMask enemyLayer;  // 몬스터만 때리기 위한 레이어 필터
    public int damage = 20;
    public Vector2 normalAttackSize = new Vector2(1f, 1f); // 일반 공격 크기
    public Vector2 dashAttackSize = new Vector2(2f, 1f);   // 대쉬 공격 크기 (가로로 더 길게!)

    [Header("상태 확인")]
    public bool isJumping = false;
    public bool isSliding = false;
    public bool isAttacking = false;
    public bool isDashAttacking = false; // [NEW] 대쉬 공격 중인지
    public bool canSlide = true;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;

    Vector2 originalSize;
    Vector2 originalOffset;

    // 슬라이딩 코루틴을 제어하기 위한 변수
    Coroutine currentSlideRoutine;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

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

    void Update()
    {
        // 1. 공격 입력 (일반 공격 vs 대쉬 공격)
        if (Input.GetMouseButtonDown(0) && !isAttacking && !isDashAttacking)
        {
            if (isSliding)
            {
                // 슬라이딩 중 공격 -> 대쉬 공격 발동!
                StartCoroutine(DashAttackRoutine());
            }
            else
            {
                // 그냥 서 있거나 걷는 중 -> 일반 공격
                StartCoroutine(AttackRoutine());
            }
        }

        // 2. 점프 입력
        if (Input.GetButtonDown("Jump") && !isJumping && !isSliding && !isAttacking && !isDashAttacking)
        {
            isJumping = true;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            anim.SetBool("IsJumping", true);
        }

        // 3. 슬라이딩 입력
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && canSlide && !isJumping && !isAttacking && !isDashAttacking)
        {
            // 코루틴을 변수에 담아 실행 (나중에 멈추기 위해)
            currentSlideRoutine = StartCoroutine(SlideRoutine());
        }
    }

    void FixedUpdate()
    {
        // 대쉬 공격 중일 때는 앞으로 전진
        if (isDashAttacking)
        {
            float direction = spriteRenderer.flipX ? -1f : 1f;
            rigid.linearVelocity = new Vector2(direction * dashAttackSpeed, rigid.linearVelocity.y);
            return;
        }

        // 일반 공격이나 슬라이딩 중이면 키보드 이동 잠금
        if (isSliding || isAttacking)
        {
            if (isAttacking && !isJumping)
                rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            return;
        }

        Move();
    }

    // [NEW] 대쉬 공격 코루틴
    // [NEW] 대쉬 공격 코루틴
    IEnumerator DashAttackRoutine()
    {
        // 1. 기존 슬라이딩 강제 종료
        if (currentSlideRoutine != null) StopCoroutine(currentSlideRoutine);

        isSliding = false;
        isDashAttacking = true;

        // 콜라이더 크기 복구 (슬라이딩 때 작아졌던 것 원상복구)
        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }

        // 2. 대쉬 공격 애니메이션 실행
        anim.SetTrigger("DashAttack");

        // [NEW] 대쉬 공격 판정 실행 (약간의 딜레이 후 때리거나 즉시 때림)
        CheckAttackHit(dashAttackSize, damage);

        // 3. 지속 시간 대기
        yield return new WaitForSeconds(dashAttackDuration);

        // 4. 상태 해제
        isDashAttacking = false;

        // [FIX] 여기서 슬라이딩 쿨타임을 풀어줘야 다시 슬라이딩이 가능합니다!
        canSlide = true;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger("Attack");

        // [NEW] 공격 판정 실행
        // 애니메이션 시작하고 0.1초 뒤에 때리는 게 자연스러우면 딜레이 추가 가능
        // yield return new WaitForSeconds(0.1f); 
        CheckAttackHit(normalAttackSize, damage);

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
    }

    // [NEW] 실제로 적을 감지하고 때리는 함수
    void CheckAttackHit(Vector2 boxSize, int atkDamage)
    {
        // 받아온 boxSize를 사용해 범위 체크
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

    // [NEW] 공격 범위를 에디터에서 눈으로 보기 위한 함수
    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPos.position, normalAttackSize);

        // 파란색으로 대쉬 공격 범위도 같이 그려보기 (참고용)
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
            spriteRenderer.flipX = xInput < 0;
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

        float direction = spriteRenderer.flipX ? -1f : 1f;
        if (Input.GetAxisRaw("Horizontal") != 0) direction = Input.GetAxisRaw("Horizontal");

        anim.SetTrigger("Slide");

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            boxCollider.offset = new Vector2(originalOffset.x, originalOffset.y - (originalSize.y * 0.25f));
        }

        rigid.linearVelocity = new Vector2(direction * slideSpeed, rigid.linearVelocity.y);

        yield return new WaitForSeconds(slideDuration);

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
}