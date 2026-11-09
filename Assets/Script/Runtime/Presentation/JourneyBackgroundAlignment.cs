using UnityEngine;

// Records the existing landscape bounds. Only the camera is fitted; authored art stays untouched.
[DisallowMultipleComponent]
public class JourneyBackgroundAlignment : MonoBehaviour
{
    SpriteRenderer landscape;
    public Bounds AuthoredBounds => landscape.bounds;

    public void Initialize(SpriteRenderer renderer) { landscape = renderer; }

    public static void FrameCamera(Camera camera, JourneyBackgroundAlignment[] backgrounds)
    {
        if (camera == null || !camera.orthographic || backgrounds.Length == 0) return;
        float bottom = float.NegativeInfinity;
        float top = float.PositiveInfinity;
        foreach (var background in backgrounds)
        {
            bottom = Mathf.Max(bottom, background.AuthoredBounds.min.y);
            top = Mathf.Min(top, background.AuthoredBounds.max.y);
        }
        if (top - bottom <= 0.04f) return;
        // Crop inside the original artwork instead of inventing a solid strip beneath it.
        camera.orthographicSize = Mathf.Min(camera.orthographicSize, (top - bottom) * 0.5f - 0.01f);
        var position = camera.transform.position;
        position.y = bottom + camera.orthographicSize + 0.01f;
        camera.transform.position = position;
    }
}
