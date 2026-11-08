using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bossapper : MonoBehaviour
{


   void Start()
{

}

    private void OnTriggerEnter2D(Collider2D other)
{
    if(other.gameObject.CompareTag("Player"))
    {


        Debug.Log("Player Enter");

        GameObject appearObj = GameObject.Find("Appear");
        if (appearObj != null)
        {
            foreach (Transform child in appearObj.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log("未找到名为 Appear 的物体");
        }

        gameObject.SetActive(false);
    }
}
}