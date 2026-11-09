using UnityEngine;

// Adapter for the original boss prefabs; campaign battles use JourneyBossController.
public class BossHealth : MonoBehaviour
{
    public int health = 12;
    public float flashTime = 0.15f;
    public GameObject spawnPrefab;
    bool defeated;
    void Start()
    {
        HealthBar.BossHealthCurrent = health;
        HealthBar.BossHealthMax = Mathf.Max(1, health);
    }

    public void TakeDamage(int amount)
    {
        var journey = LifeJourneySession.Instance;
        if (journey != null && journey.IsStageScene)
        {
            if (journey.Boss != null)
                journey.Boss.TakeHit(amount);
            return;
        }

        if (defeated || amount <= 0)
            return;
        health = Mathf.Max(0, health - amount);
        HealthBar.BossHealthCurrent = health;
        if (health != 0)
            return;
        defeated = true;
        if (spawnPrefab != null)
            Instantiate(spawnPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
