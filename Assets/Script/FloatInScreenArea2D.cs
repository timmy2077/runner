using UnityEngine;

public class FloatInScreenArea2D : MonoBehaviour
{
    [Header("浮动设置")]
    public float floatSpeedX = 1f;
    public float floatSpeedY = 1.2f;
    public float floatRange = 0.5f;

    [Header("跟随设置")]
    public float followSpeed = 5f;

    [Header("替换图片")]
    public Sprite newSprite; // 碰到 NPC 后替换的图片

    private SpriteRenderer WeaponSprite;

    private Transform player;
    private Transform point;
    private bool isCollected = false;

    private float phaseX, phaseY;
    private Collider2D col;

    void Start()
    {
        phaseX = Random.Range(0f, Mathf.PI * 2);
        phaseY = Random.Range(0f, Mathf.PI * 2);
        col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        WeaponSprite = GetComponent<SpriteRenderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            player = other.transform;
            point = player.Find("FloatPoint");
            if (point == null)
            {
                Debug.LogWarning("FloatInScreenArea2D: 未找到 FloatPoint 子物体！");
                return;
            }
            if (col != null) col.enabled = false;
            
            // 成为 FloatPoint 的子物体
            transform.SetParent(point);
        }

        if (other.CompareTag("NPC"))
        {
            if (WeaponSprite != null && newSprite != null)
            {
                Debug.Log("FloatInScreenArea2D: 替换图片为: " + newSprite.name);
                WeaponSprite.sprite = newSprite;
                
            }
        }
    }

    void Update()
    {
        if (!isCollected || point == null) return;

        // 计算本地坐标浮动偏移
        float offsetX = Mathf.Sin(Time.time * floatSpeedX + phaseX) * floatRange;
        float offsetY = Mathf.Sin(Time.time * floatSpeedY + phaseY) * floatRange;

        // 平滑移动到目标本地位置
        Vector3 targetLocalPos = new Vector3(offsetX, offsetY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, followSpeed * Time.deltaTime);
    }
}
