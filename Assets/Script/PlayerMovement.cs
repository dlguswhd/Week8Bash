using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float movePower = 5f;    // 걷기 속도
    public float jumpPower = 5f;    // 점프 힘

    [Header("슬라이딩 설정")]
    public float slideSpeed = 10f;      // 슬라이딩 속도
    public float slideDuration = 0.4f;  // 슬라이딩 지속 시간 (초)
    public float slideCooldown = 1.0f;  // 슬라이딩 재사용 대기시간

    [Header("상태 확인")]
    public bool isJumping = false;
    public bool isSliding = false;
    public bool canSlide = true;    // 쿨타임 확인용

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;      // 크기 조절용

    // 콜라이더 원래 크기 저장 변수
    Vector2 originalSize;
    Vector2 originalOffset;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        // 시작할 때 원래 콜라이더 크기를 기억해둡니다.
        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;
        }
    }

    void Update()
    {
        // 1. 점프 (슬라이딩 중에는 점프 불가)
        if (Input.GetButtonDown("Jump") && !isJumping && !isSliding)
        {
            isJumping = true;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }

        // 2. 슬라이딩 (Shift 키 & 땅에 있을 때 & 쿨타임 아닐 때)
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && canSlide && !isJumping)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    void FixedUpdate()
    {
        // 슬라이딩 중에는 일반 이동 입력을 무시하고 코루틴이 속도를 제어하게 둡니다.
        if (isSliding) return;

        Move();
    }

    void Move()
    {
        float xInput = Input.GetAxisRaw("Horizontal");

        // 이동 처리
        rigid.linearVelocity = new Vector2(xInput * movePower, rigid.linearVelocity.y);

        // 애니메이션 처리 (걷기/멈춤)
        // IsWalking 파라미터가 true면 걷기, false면 대기
        if (xInput != 0)
        {
            anim.SetBool("IsWalking", true);
            spriteRenderer.flipX = xInput < 0; // 방향 전환
        }
        else
        {
            anim.SetBool("IsWalking", false);
        }
    }

    // 슬라이딩 로직 (코루틴)
    IEnumerator SlideRoutine()
    {
        isSliding = true;
        canSlide = false; // 쿨타임 시작

        // 슬라이딩 방향 결정 (이동 키를 안 누르면 바라보는 방향으로)
        float direction = spriteRenderer.flipX ? -1f : 1f;
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            direction = Input.GetAxisRaw("Horizontal");
        }

        // 1. 애니메이션 실행
        anim.SetTrigger("Slide");

        // 2. 콜라이더 크기를 납작하게 줄임 (장애물 통과 기능)
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(originalSize.x, originalSize.y * 0.5f); // 높이 반토막
            boxCollider.offset = new Vector2(originalOffset.x, originalOffset.y - (originalSize.y * 0.25f)); // 위치 보정
        }

        // 3. 속도 적용 (순간적으로 빠르게)
        rigid.linearVelocity = new Vector2(direction * slideSpeed, rigid.linearVelocity.y);

        // 4. 지속 시간만큼 대기
        yield return new WaitForSeconds(slideDuration);

        // 5. 슬라이딩 종료 (원래대로 복구)
        isSliding = false;
        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }

        // 6. 쿨타임 대기 후 재사용 가능 상태로
        yield return new WaitForSeconds(slideCooldown);
        canSlide = true;
    }
}