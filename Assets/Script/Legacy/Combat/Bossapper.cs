using UnityEngine;

// Compatibility for old demo prefabs. Campaign encounters use JourneyEncounterDirector.
public class Bossapper : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        var session = LifeJourneySession.Instance;
        if (session != null && session.IsStageScene)
            return;
        var appearance = GameObject.Find("Appear");
        if (appearance != null)
            foreach (Transform child in appearance.transform)
                child.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
