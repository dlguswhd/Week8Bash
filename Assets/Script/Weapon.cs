using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int damage = 30; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("몬스터 때림!");
            }
        }
    }
}