using UnityEngine;
using UnityEngine.SceneManagement;

// Preserve serialized legacy assets while ensuring only the campaign owns encounters.
public static class JourneyLegacyIsolation
{
    public static void Apply(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            ApplyRoot(root);
    }

    public static void ApplyRoot(GameObject root)
    {
            foreach (var boss in root.GetComponentsInChildren<Boss>(true))
                boss.gameObject.SetActive(false);
            foreach (var trigger in root.GetComponentsInChildren<Bossapper>(true))
                trigger.gameObject.SetActive(false);
            foreach (var health in root.GetComponentsInChildren<BossHealth>(true))
                health.gameObject.SetActive(false);
            foreach (var manager in root.GetComponentsInChildren<GameManager>(true))
                manager.enabled = false;
            foreach (var energy in root.GetComponentsInChildren<EnergyCollector>(true))
                energy.enabled = false;
            foreach (var damage in root.GetComponentsInChildren<ObjectDamage>(true))
                damage.enabled = false;
            // Embedded end/camera triggers belong to the old flow; new stage completion
            // and camera ownership remain with LifeJourneySession.
            foreach (var ending in root.GetComponentsInChildren<TheEnd>(true)) ending.enabled = false;
            foreach (var ending in root.GetComponentsInChildren<Customsclearance>(true)) ending.enabled = false;
            foreach (var detach in root.GetComponentsInChildren<DetachChild>(true)) detach.enabled = false;
    }
}
