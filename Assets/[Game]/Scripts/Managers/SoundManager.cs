using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    #region Singleton

    public static SoundManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        Initialize();
    }

    #endregion

    public enum SoundType
    {
        Music,
        CarEngine,
        Audience,
        Confetti,
        Bip,
        Biiip,
        LapPass,
        LapFinish,
        ButtonClick,
        ButtonClick2,
        PlayClick,
        ButtonHover
    }

    [System.Serializable]
    public class Sound
    {
        public SoundType soundType;
        public string soundName;
        public AudioClip clip;
        public bool loop;
        public float volume = 1.0f;
        public float pitch = 1.0f;
        [HideInInspector] public AudioSource audioSource;
    }

    [Header("Sounds")] public List<Sound> sounds = new List<Sound>();

    private void Initialize()
    {
        // AudioSource'ları oluştur
        foreach (var sound in sounds)
        {
            sound.audioSource = gameObject.AddComponent<AudioSource>();
            sound.audioSource.clip = sound.clip;
            sound.audioSource.loop = sound.loop;
            sound.audioSource.volume = sound.volume;
            sound.audioSource.pitch = sound.pitch;
        }

        PlaySound(SoundType.Music, true);
    }

    public void PlaySound(SoundType soundType, bool loop = false)
    {
        Sound sound = sounds.Find(s => s.soundType == soundType);

        if (sound != null)
        {
            sound.audioSource.loop = loop;
            sound.audioSource.Play();
        }
        else
        {
            Debug.LogError("Sound not found: " + soundType);
        }
    }

    public void SetSoundPitch(SoundType soundType, float pitch = 1.0f)
    {
        Sound sound = sounds.Find(s => s.soundType == soundType);

        if (sound != null)
        {
            sound.audioSource.pitch = pitch;
        }
        else
        {
            Debug.LogError("Sound not found: " + soundType);
        }
    }

    //set volume
    public void SetSoundVolume(SoundType soundType, float volume)
    {
        Sound sound = sounds.Find(s => s.soundType == soundType);

        if (sound != null)
        {
            sound.audioSource.volume = volume;
        }
        else
        {
            Debug.LogError("Sound not found: " + soundType);
        }
    }

    public void StopSound(SoundType soundType)
    {
        Sound sound = sounds.Find(s => s.soundType == soundType);

        if (sound != null)
        {
            sound.audioSource.Stop();
            sound.audioSource.loop = sound.loop;
            sound.audioSource.volume = sound.volume;
            sound.audioSource.pitch = sound.pitch;
        }
        else
        {
            Debug.LogError("Sound not found: " + soundType);
        }
    }
}