using UnityEngine;

// Ordinary waiting gate. Boss attacks have their own single-use warning/active lifecycle.
public class JourneyHazard : MonoBehaviour
{
    BoxCollider2D trigger;
    SpriteRenderer visual;
    float clock;
    bool approached;
    public void Initialize(JourneyObstacle kind, float ground, int seed, bool freshWarning = false)
    {
        trigger = GetComponent<BoxCollider2D>();
        visual = GetComponentInChildren<SpriteRenderer>();
        trigger.enabled = false;
        clock = 0;
        approached = false;
    }

    void Update()
    {
        var journey = LifeJourneySession.Instance;
        var player = PlayerController.Instance;
        if (journey == null || !journey.IsPlaying || player == null)
            return;
        float distance = transform.position.x - player.transform.position.x;
        float warningDistance = Mathf.Max(6, journey.Profile.sprintSpeed * 2.5f);
        if (!approached && distance < warningDistance && distance > -1)
            approached = true;
        if (approached)
            clock += Time.deltaTime;
        float phase = clock % 8;
        bool warning = approached && phase < 2;
        bool active = approached && phase >= 2 && phase < 4;
        trigger.enabled = active;
        if (visual != null)
            visual.color = new Color(1, 1, 1, active ? 1 : warning ? 0.35f + 0.2f * Mathf.Sin(clock * 8) : 0.12f);
        if (distance > -0.5f && distance < warningDistance)
            journey.UI.SetHazardHint(active ? "前方封闭 · 按住 Z / ↓ 等待间隙" : warning ? "前方即将封闭 · 可以停下等待" : "间隙出现了 · 可以前进", 0.3f);
    }

    void OnDisable()
    {
        if (trigger != null)
            trigger.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Touch(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Touch(other);
    }

    void Touch(Collider2D other)
    {
        if (trigger != null && trigger.enabled && other.CompareTag("Player"))
            other.GetComponent<PlayerController>()?.HitHazard();
    }
}
