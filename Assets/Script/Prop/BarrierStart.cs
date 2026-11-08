using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierStart : MonoBehaviour
{
    public Rigidbody2D[] rbs;

    private void Start()
    {
        foreach (Rigidbody2D rb in rbs)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (Rigidbody2D rb in rbs)
            {
                rb.gravityScale = 1;
            }
        }
    }
}
