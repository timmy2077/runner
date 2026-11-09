using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runtime presentation only: source textures, materials, and the scene's art remain untouched.
public class JourneyPresentation : MonoBehaviour
{
    Volume volume;
    VolumeProfile profile;
    ColorAdjustments color;
    UniversalAdditionalCameraData cameraData;
    bool previousPostProcessing;
    LayerMask previousVolumeMask;
    float death;
    float masterVolume;
    void Awake()
    {
        masterVolume = PlayerPrefs.GetFloat("runner.journey.volume", 0.8f);
        var camera = GetComponent<Camera>();
        if (camera != null)
        {
            cameraData = camera.GetUniversalAdditionalCameraData();
            previousPostProcessing = cameraData.renderPostProcessing;
            previousVolumeMask = cameraData.volumeLayerMask;
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = ~0;
        }

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(0);
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000;
        volume.sharedProfile = profile;
        ApplyVolume();
    }

    public void SetDeath(float amount)
    {
        death = Mathf.Clamp01(amount);
        if (color != null)
            color.saturation.Override(-100 * death);
        ApplyVolume();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    void ApplyVolume()
    {
        AudioListener.volume = masterVolume * Mathf.Lerp(1, 0.25f, death);
    }

    void OnDestroy()
    {
        AudioListener.volume = masterVolume;
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = previousPostProcessing;
            cameraData.volumeLayerMask = previousVolumeMask;
        }

        if (volume != null)
            Destroy(volume);
        if (profile != null)
            Destroy(profile);
    }
}
