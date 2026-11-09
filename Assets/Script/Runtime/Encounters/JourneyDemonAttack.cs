using UnityEngine;

// A single attack: fixed world-space warning, active window, then disposal.
public class JourneyDemonAttack : MonoBehaviour
{
    public bool IsDangerous => hitbox != null && hitbox.enabled;

    LifeJourneySession journey;
    JourneyDemonDefinition definition;
    JourneyDemonStrike strike;
    BoxCollider2D hitbox;
    SpriteRenderer visual, marker;
    Transform body;
    float elapsed, ground;
    public void Initialize(LifeJourneySession session, JourneyDemonDefinition settings, JourneyDemonStrike plan)
    {
        journey = session;
        definition = settings;
        strike = plan;
        ground = session.Course.GroundTop;
        transform.position = new Vector3(plan.x, ground, 0);
        var markerRoot = new GameObject("Warning footprint");
        markerRoot.transform.SetParent(transform, false);
        markerRoot.transform.localPosition = new Vector3(0, 0.08f, 0);
        marker = markerRoot.AddComponent<SpriteRenderer>();
        marker.sprite = session.Settings.obstacleSprite;
        marker.sortingOrder = 12;
        Size(markerRoot.transform, marker.sprite, new Vector2(plan.width + plan.travel * 2 + 0.4f, 0.14f));
        body = new GameObject("Strike").transform;
        body.SetParent(transform, false);
        hitbox = body.gameObject.AddComponent<BoxCollider2D>();
        hitbox.size = new Vector2(plan.width, 2.6f);
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        body.gameObject.AddComponent<JourneyDemonHitbox>().Initialize(this);
        var art = new GameObject("Attack visual");
        art.transform.SetParent(body, false);
        visual = art.AddComponent<SpriteRenderer>();
        visual.sprite = session.Settings.obstacleSprite;
        visual.sortingOrder = 7;
        Size(art.transform, visual.sprite, hitbox.size);
        Apply();
    }

    void Update()
    {
        if (journey == null || !journey.IsPlaying)
            return;
        elapsed += Time.deltaTime;
        if (elapsed >= definition.warningSeconds + definition.activeSeconds)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Apply();
    }

    void Apply()
    {
        bool active = elapsed >= definition.warningSeconds;
        hitbox.enabled = active;
        float activeProgress = Mathf.Clamp01((elapsed - definition.warningSeconds) / definition.activeSeconds);
        float x = strike.kind == JourneyObstacle.Moving ? Mathf.Sin(activeProgress * Mathf.PI * 2) * strike.travel : 0;
        float y = strike.kind == JourneyObstacle.Falling || strike.kind == JourneyObstacle.Pressure ? active ? Mathf.Lerp(4, 1.3f, Mathf.Clamp01(activeProgress * 4)) : 4 : 1.3f;
        body.localPosition = new Vector3(x, y, 0);
        var tint = definition.tint;
        tint.a = active ? 1 : 0.3f;
        visual.color = tint;
        marker.color = new Color(1, 0.35f, 0.22f, active ? 0.9f : 0.4f + 0.3f * Mathf.PingPong(elapsed * 3, 1));
        var player = PlayerController.Instance;
        if (player != null && Mathf.Abs(player.transform.position.x - strike.x) < Mathf.Max(8, journey.Profile.moveSpeed * 4))
            journey.UI.SetHazardHint(active ? "心魔出招 · 避开地面标记" : "心魔蓄势 · 地面标记处即将受击", 0.3f);
    }

    public void Touch(Collider2D other)
    {
        if (IsDangerous && journey != null && journey.IsPlaying && other.CompareTag("Player"))
            other.GetComponent<PlayerController>()?.HitHazard();
    }

    void OnDisable()
    {
        if (hitbox != null)
            hitbox.enabled = false;
    }

    static void Size(Transform target, Sprite sprite, Vector2 size)
    {
        if (sprite != null)
            target.localScale = new Vector3(size.x / Mathf.Max(0.01f, sprite.bounds.size.x), size.y / Mathf.Max(0.01f, sprite.bounds.size.y), 1);
    }
}
