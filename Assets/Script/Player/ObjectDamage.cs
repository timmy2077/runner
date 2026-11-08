using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDamage : MonoBehaviour
{
    
        void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<BoxCollider2D>() != null && PlayerController.Instance != null)
        {
            collision.gameObject.GetComponent<BossHealth>().TakeDamage(PlayerController.Instance.damage);
            Debug.Log("Damage: " + PlayerController.Instance.damage);
            gameObject.SetActive(false);
        }
        
    }

}