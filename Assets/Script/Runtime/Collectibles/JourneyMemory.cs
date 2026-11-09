using UnityEngine;

public class JourneyMemory : MonoBehaviour
{
    public int slot;
    void OnTriggerEnter2D(Collider2D other)
    {
        var journey = LifeJourneySession.Instance;
        if (!other.CompareTag("Player") || journey == null || !journey.IsPlaying)
            return;
        journey.Remember(slot);
        gameObject.SetActive(false);
    }
}
