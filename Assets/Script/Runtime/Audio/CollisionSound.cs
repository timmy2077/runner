using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    public int soundIndex; // 在 Unity 中设置音效索引
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play(soundIndex);
            }
        }
    }
}
