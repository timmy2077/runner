using System.Collections.Generic;
using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    public GameObject backgroundPrefab;
    public float moveSpeed = 2;
    public float spawnInterval = 3;
    public int initialCount = 3;
    readonly List<GameObject> backgrounds = new List<GameObject>();
    float width, fittedHeight, scale = 1;
    SpriteRenderer sourceRenderer;
    GameObject sourcePrefab;
    void Start()
    {
        if (backgroundPrefab == null)
        {
            enabled = false;
            return;
        }

        // Stage references may point at a transparent foreground layer. Include its authored
        // parent so the opaque landscape behind it is present too.
        sourcePrefab = backgroundPrefab.transform.root.gameObject;
        sourceRenderer = sourcePrefab.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            sourcePrefab = backgroundPrefab;
            sourceRenderer = sourcePrefab.GetComponent<SpriteRenderer>();
        }
        if (sourceRenderer == null || sourceRenderer.bounds.size.x < 0.1f || sourceRenderer.bounds.size.y < 0.1f)
        {
            enabled = false;
            return;
        }

        FillView();
    }

    // Follow the final camera pose, including a resized Game view, before rendering.
    void LateUpdate()
    {
        foreach (var background in backgrounds)
            if (background != null)
                background.transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        FillView();
    }

    void FillView()
    {
        var camera = Camera.main;
        if (camera == null || sourceRenderer == null)
            return;
        float viewHeight = camera.orthographicSize * 2 + 0.04f;
        if (!Mathf.Approximately(fittedHeight, viewHeight))
        {
            foreach (var background in backgrounds)
                if (background != null) { background.SetActive(false); Destroy(background); }
            backgrounds.Clear();
            fittedHeight = viewHeight;
            // Uniform scaling retains the artwork's proportions; original prefab assets stay unchanged.
            scale = Mathf.Max(1, viewHeight / sourceRenderer.bounds.size.y);
            width = sourceRenderer.bounds.size.x * scale;
        }
        float centerOffset = (sourceRenderer.bounds.center.x - sourcePrefab.transform.position.x) * scale;
        float verticalOffset = (sourceRenderer.bounds.center.y - sourcePrefab.transform.position.y) * scale;
        float backgroundY = camera.transform.position.y - verticalOffset;
        foreach (var background in backgrounds)
            if (background != null)
            {
                var position = background.transform.position;
                position.y = backgroundY;
                background.transform.position = position;
            }
        float halfView = camera.orthographicSize * camera.aspect;
        float left = camera.transform.position.x - halfView;
        float right = camera.transform.position.x + halfView;
        for (int i = backgrounds.Count - 1; i >= 0; i--)
            if (backgrounds[i] == null || backgrounds[i].transform.position.x + centerOffset + width * 0.5f < left - width)
            {
                if (backgrounds[i] != null)
                    Destroy(backgrounds[i]);
                backgrounds.RemoveAt(i);
            }

        float next = backgrounds.Count == 0 ? left - width * 0.5f - centerOffset : backgrounds[backgrounds.Count - 1].transform.position.x + width;
        // Also refill the left edge after a camera snap or a wider window.
        while (backgrounds.Count > 0 && backgrounds[0].transform.position.x + centerOffset - width * 0.5f > left - width)
            backgrounds.Insert(0, Spawn(backgrounds[0].transform.position.x - width, backgroundY));
        while (next + centerOffset - width * 0.5f < right + width)
        {
            backgrounds.Add(Spawn(next, backgroundY));
            next += width;
        }
    }

    GameObject Spawn(float x, float y)
    {
        var position = sourcePrefab.transform.position;
        position.x = x;
        position.y = y;
        var background = Instantiate(sourcePrefab, position, Quaternion.identity, transform);
        // Child prefab references need the authored parent scale when instantiated alone.
        background.transform.localScale = Vector3.Scale(sourcePrefab.transform.lossyScale, Vector3.one * scale);
        foreach (var renderer in background.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = -100;
        }
        return background;
    }
}
