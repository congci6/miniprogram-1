using UnityEngine;

namespace PocketCity.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = musicVolume * masterVolume;
            }

            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume * masterVolume;
            }
        }

        public void PlaySound(SoundType soundType)
        {
            if (sfxSource != null && sfxSource.enabled)
            {
                Debug.Log($"[AudioManager] PlaySound: {soundType}");
            }
        }

        public void PlayMusic(string musicName)
        {
            if (musicSource != null)
            {
                Debug.Log($"[AudioManager] PlayMusic: {musicName}");
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume * masterVolume;
            }
        }

        private void UpdateVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume * masterVolume;
            }
        }
    }

    public enum SoundType
    {
        Click,
        BuildingPlaced,
        BuildingDemolished,
        BuildingUpgrade,
        ZonePlaced,
        RoadPlaced,
        CashRegister,
        LevelUp,
        Achievement,
        Warning,
        Disaster,
        DisasterWarning,
        MoneyEarned,
        ProductionComplete,
        CoinCollect,
    }
}
