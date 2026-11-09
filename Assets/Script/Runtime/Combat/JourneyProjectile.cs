using UnityEngine;

public class JourneyProjectile : MonoBehaviour
{
    JourneyBossController target;
    float lifetime;
    public void Initialize(JourneyBossController boss, Sprite sprite)
    {
        target = boss;
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;
        if (sprite != null)
            transform.localScale = Vector3.one * (0.35f / Mathf.Max(sprite.bounds.size.x, 0.01f));
    }

    void Update()
    {
        var journey = LifeJourneySession.Instance;
        if (journey == null || !journey.IsPlaying)
            return;
        lifetime += Time.deltaTime;
        if (target == null || !target.IsAlive || lifetime > 3)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 18 * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.transform.position) <= 0.7f)
        {
            target.TakeHit(1);
            Destroy(gameObject);
        }
    }
}
