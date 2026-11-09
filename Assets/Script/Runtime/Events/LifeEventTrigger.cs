using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("Life Journey/人生事件触发器")]
public class LifeEventTrigger : MonoBehaviour
{
    [Tooltip("进入范围后打开的事件 ID。大小写敏感，在自定义 Inspector 中可从事件库选择。")]
    public string eventId = "I01";
    [Tooltip("打开事件前等待角色落地。关闭后可在空中触发，恢复时保留原速度。")]
    public bool requireGrounded = true;
    [Tooltip("开启后，分段生成器将范围附近生成平地；不改变触发器位置，也不改变心魔攻击。")]
    public bool reserveFlatGround = true;
    [Min(0), Tooltip("为事件点两侧额外保留的平地距离（世界单位）。")]
    public float safePadding = 3;
    public Color gizmoColor = new Color(0.4f, 0.85f, 0.95f, 0.3f);

    readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    public BoxCollider2D Area => GetComponent<BoxCollider2D>();
    public bool HasPlayerInside
    {
        get
        {
            occupants.RemoveWhere(item => item == null || !item.enabled || !item.gameObject.activeInHierarchy);
            return occupants.Count > 0;
        }
    }

    void Reset() { Area.isTrigger = true; Area.size = new Vector2(3, 4); }
    void OnValidate() { Area.isTrigger = true; safePadding = Mathf.Max(0, safePadding); }
    void OnTriggerEnter2D(Collider2D other) { Track(other); }
    void OnTriggerStay2D(Collider2D other) { Track(other); }
    void Track(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != PlayerController.Instance || PlayerController.Instance == null) return;
        occupants.Add(other);
        LifeJourneySession.Instance?.Events?.Request(this);
    }
    void OnTriggerExit2D(Collider2D other) { occupants.Remove(other); }
    void OnDisable() { occupants.Clear(); }

    void OnDrawGizmos()
    {
        var area = Area;
        if (area == null) return;
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(area.offset, area.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1);
        Gizmos.DrawWireCube(area.offset, area.size);
        Gizmos.matrix = previous;
    }
}
