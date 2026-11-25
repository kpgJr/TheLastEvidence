using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace LastEvidence
{
    public enum SfxType
    {
        Click, KeypadBeep, Success, Error, Locked, Pickup, Evidence, DoorOpen, DoorClose,
        Drawer, Switch, Footstep, Paper, Computer, Electric
    }

    public enum AmbienceType { Rain, Wind, Clock, Hum, Music }

    /// <summary>
    /// Gestion centralisée du son : effets 2D, musique, et volumes par catégorie
    /// (général, musique, effets). Si un AudioMixer est assigné (paramètres exposés
    /// « MasterVolume », « MusicVolume », « SfxVolume »), il est utilisé ; sinon les volumes
    /// sont appliqués directement aux AudioSources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [Tooltip("Clips optionnels remplaçant les sons synthétisés (même ordre que SfxType).")]
        [SerializeField] private List<AudioClip> overrideClips = new List<AudioClip>();

        private readonly Dictionary<SfxType, AudioClip> sfxCache = new Dictionary<SfxType, AudioClip>();
        private readonly Dictionary<AmbienceType, AudioClip> ambienceCache = new Dictionary<AmbienceType, AudioClip>();
        private AudioSource sfxSource;
        private AudioSource musicSource;

        public AudioMixerGroup SfxGroup => sfxGroup;
        public float SfxVolumeFactor => mixer != null ? 1f : GameSettings.MasterVolume * GameSettings.SfxVolume;
        public float MusicVolumeFactor => mixer != null ? 1f : GameSettings.MasterVolume * GameSettings.MusicVolume;

        /// <summary>Crée l'AudioManager persistant s'il n'existe pas encore.</summary>
        public static AudioManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.outputAudioMixerGroup = sfxGroup;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.outputAudioMixerGroup = musicGroup;
            GameSettings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplyVolumes;
            if (Instance == this) Instance = null;
        }

        public AudioClip GetClip(SfxType type)
        {
            int index = (int)type;
            if (index < overrideClips.Count && overrideClips[index] != null) return overrideClips[index];
            if (!sfxCache.TryGetValue(type, out var clip))
            {
                clip = ProceduralAudio.Create(type);
                sfxCache[type] = clip;
            }
            return clip;
        }

        public AudioClip GetAmbience(AmbienceType type)
        {
            if (!ambienceCache.TryGetValue(type, out var clip))
            {
                clip = ProceduralAudio.CreateAmbience(type);
                ambienceCache[type] = clip;
            }
            return clip;
        }

        public void PlaySfx(SfxType type, float volume = 1f)
        {
            if (SaveManager.IsRestoring) return;
            sfxSource.PlayOneShot(GetClip(type), volume * SfxVolumeFactor);
        }

        /// <summary>Joue un effet spatialisé (3D) à une position du monde.</summary>
        public void PlaySfxAt(SfxType type, Vector3 position, float volume = 1f)
        {
            if (SaveManager.IsRestoring) return;
            var go = new GameObject("Sfx3D_" + type);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = GetClip(type);
            src.spatialBlend = 1f;
            src.minDistance = 1f;
            src.maxDistance = 15f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.volume = volume * SfxVolumeFactor;
            src.outputAudioMixerGroup = sfxGroup;
            src.Play();
            Destroy(go, src.clip.length + 0.1f);
        }

        public void PlayMusic(AmbienceType type = AmbienceType.Music)
        {
            var clip = GetAmbience(type);
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
            ApplyVolumes();
        }

        public void StopMusic() => musicSource.Stop();

        private void ApplyVolumes()
        {
            if (mixer != null)
            {
                mixer.SetFloat("MasterVolume", ToDecibel(GameSettings.MasterVolume));
                mixer.SetFloat("MusicVolume", ToDecibel(GameSettings.MusicVolume));
                mixer.SetFloat("SfxVolume", ToDecibel(GameSettings.SfxVolume));
            }
            if (musicSource != null) musicSource.volume = 0.5f * MusicVolumeFactor;
        }

        private static float ToDecibel(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
