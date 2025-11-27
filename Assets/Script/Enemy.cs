using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("몬스터 설정 (여기서 숫자 바꾸기)")]
    public string monsterName = "몬스터이름";
    public int hp = 100;             
    public float speed = 2.0f;        
    public int damage = 10;           

    [Header("AI 범위 설정")]
    public float detectRange = 6f;    
    public float attackRange = 1.2f;  

    Transform target; 
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer spr;
    bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spr = GetComponent<SpriteRenderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    void Update()
    {
        if (isDead) return;
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist <= detectRange && dist > attackRange)
        {
            MoveToPlayer();
        }
        else 
        {
            StopMoving();
        }
    }

    void MoveToPlayer()
    {
        float dirX = (target.position.x - transform.position.x) > 0 ? 1 : -1;

        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);

        spr.flipX = (dirX == -1);

        anim.SetBool("IsMoving", true);
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        hp -= dmg;
        StartCoroutine(HitColorEffect()); 

        Debug.Log(monsterName + " 남은 체력: " + hp);

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("① [Enemy] Die 함수 시작! 으앙 죽음");

        isDead = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("② [오류] Player 태그를 가진 물체가 없음!");
        }
        else
        {
            Debug.Log("② [Enemy] Player 오브젝트 찾음: " + player.name);

            PlayerMovement playerScript = player.GetComponent<PlayerMovement>();

            if (playerScript == null)
            {
                Debug.LogError("③ [오류] 걔한테 PlayerMovement 스크립트가 없음!");
            }
            else
            {
                Debug.Log("③ [Enemy] 스크립트 찾음 -> 힐(Heal) 명령 보냄!");
                playerScript.Heal(10);
            }
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnEnemyDead();
            GameManager.instance.AddMoney(10);
        }

        anim.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2f);
    }

    IEnumerator HitColorEffect()
    {
        spr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spr.color = Color.white;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }

    }
}