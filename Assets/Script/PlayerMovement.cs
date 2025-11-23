using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float movePower = 5f;   
    public float jumpPower = 5f;

    [Header("슬라이딩 설정")]
    public float slideSpeed = 10f;    
    public float slideDuration = 0.4f;  
    public float slideCooldown = 1.0f;  

    [Header("상태 확인")]
    public bool isJumping = false;
    public bool isSliding = false;
    public bool canSlide = true;   

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;     

    Vector2 originalSize;
    Vector2 originalOffset;

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
    }

    void Update()
    {

        if (Input.GetButtonDown("Jump") && !isJumping && !isSliding)
        {
            isJumping = true;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && canSlide && !isJumping)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    void FixedUpdate()
    {

        if (isSliding) return;

        Move();
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
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            direction = Input.GetAxisRaw("Horizontal");
        }

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
}