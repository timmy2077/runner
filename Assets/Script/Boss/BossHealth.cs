using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("生命系统")]
    public int health;
    // public int damage;
    public float flashTime;
    // public GameObject bloodEffect;
    // public GameObject dropCoin;
    // public GameObject floatPoint;

    [Header("生成设置")]
    public GameObject spawnPrefab; // 要生成的预制体

    private SpriteRenderer sr; 
    private Color originalColor;
    private ObjectDamage objectDamage;

        private void Start()
    {
        HealthBar.BossHealthCurrent = health;
        HealthBar.BossHealthMax = health;
        // objectDamage = GameObject.FindGameObjectWithTag("Damage").GetComponent<ObjectDamage>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;   
    }

            public void Update()
    {
        if (health <= 0)
        {
            if (spawnPrefab != null)
            {
                Instantiate(spawnPrefab, transform.position, Quaternion.identity);
            }

            // Instantiate(dropCoin, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

        public void TakeDamage(int damage)
    {
        HealthBar.BossHealthCurrent -= damage;
        // GameObject gb = Instantiate(floatPoint, transform.position, Quaternion.identity) as GameObject;
        health -= damage;
        FlashColor(flashTime);
        // Instantiate(bloodEffect, transform.position, Quaternion.identity);
        // GameController.camShake.Shake();
    }

        void FlashColor(float time)
    {
        sr.color = Color.red;
        Invoke("ResetColor", time);
    }

    void ResetColor()
    {
        sr.color = originalColor;
    }
}
