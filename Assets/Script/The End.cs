using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TheEnd : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("游戏结束");
            SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex+1 );
            
        }
    }
}
