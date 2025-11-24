using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    int currentHealth;

    SpriteRenderer spriteRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 플레이어가 이 함수를 호출해서 데미지를 줍니다.
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 피격 효과 (빨간색으로 깜빡임)
        StartCoroutine(HitEffect());

        Debug.Log("몬스터 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator HitEffect()
    {
        spriteRenderer.color = Color.red; // 빨간색
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white; // 원래 색
    }

    void Die()
    {
        Debug.Log("몬스터 사망!");
        // 죽는 애니메이션이 있다면 여기서 실행

        // 오브젝트 삭제
        Destroy(gameObject);
    }
}