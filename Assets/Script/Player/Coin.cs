using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.CompareTag("Player") 
        // && 
            // other.GetType().ToString() == "UnityEngine.CapsuleCollider2D"
        )
        {
            // SoundManager.Instance.Play("PickCoin");
            // CoinUI.CurrentCoinQuantity += 1;
            gameObject.SetActive(false);
        }
    }
}

