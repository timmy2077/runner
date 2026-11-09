using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("设置")]
    public float volume = 1f;
    [Header("音效列表")]
    public List<SoundData> sounds = new List<SoundData>();
    private AudioSource audioSrc;
    private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        audioSrc = GetComponent<AudioSource>();
        foreach (var sound in sounds)
        {
            if (!soundDict.ContainsKey(sound.name))
                soundDict.Add(sound.name, sound.clip);
        }
    }

    public void Play(string name, float vol = -1f)
    {
        if (soundDict.TryGetValue(name, out var clip))
        {
            audioSrc.PlayOneShot(clip, vol >= 0 ? vol : volume);
        }
    }

    public void Play(int index, float vol = -1f)
    {
        if (index >= 0 && index < sounds.Count)
        {
            audioSrc.PlayOneShot(sounds[index].clip, vol >= 0 ? vol : volume);
        }
    }

    public void SetVolume(float v) => volume = Mathf.Clamp01(v);
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [System.Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
    }
}
