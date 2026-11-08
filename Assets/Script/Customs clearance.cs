using UnityEngine;

public class Customsclearance : MonoBehaviour
{
    private CameraFollow cameraFollow;

    private void Start()
    {
        cameraFollow = FindObjectOfType<CameraFollow>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (cameraFollow != null)
            {
                cameraFollow.target = null;
                Debug.Log("相机停止追踪目标");
            }
        }
    }
}
