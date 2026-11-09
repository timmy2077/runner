using UnityEngine;

// Pool identity is explicit; names are never used to identify a pooled object.
public class JourneyObstacleView : MonoBehaviour
{
    public JourneyObstacle Kind { get; private set; }
    public bool IsRented { get; set; }

    BoxCollider2D hitbox;
    SpriteRenderer visual;
    Transform art;
    JourneyHazard gate;
    public void Build(JourneyObstacle kind)
    {
        Kind = kind;
        hitbox = gameObject.AddComponent<BoxCollider2D>();
        art = new GameObject("Obstacle visual").transform;
        art.SetParent(transform, false);
        visual = art.gameObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = 5;
        if (kind == JourneyObstacle.Gate)
            gate = gameObject.AddComponent<JourneyHazard>();
    }

    public void Configure(Transform parent, Vector2 position, Vector2 size, Sprite sprite, float ground, int seed)
    {
        transform.SetParent(parent, false);
        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        hitbox.size = size;
        hitbox.isTrigger = Kind == JourneyObstacle.Gate;
        hitbox.enabled = !hitbox.isTrigger;
        gameObject.tag = hitbox.isTrigger ? "Untagged" : "Obstacle";
        visual.sprite = sprite;
        visual.color = Color.white;
        art.localScale = sprite == null ? Vector3.one : new Vector3(size.x / Mathf.Max(sprite.bounds.size.x, 0.01f), size.y / Mathf.Max(sprite.bounds.size.y, 0.01f), 1);
        if (gate != null)
            gate.Initialize(JourneyObstacle.Gate, ground, seed, true);
        IsRented = true;
        gameObject.SetActive(true);
    }
}
