using UnityEngine;

namespace LastEvidence
{
    /// <summary>Source sonore 3D en boucle (pluie, vent, horloge, bourdonnement électrique).</summary>
    public class AmbientSoundSource : MonoBehaviour
    {
        [SerializeField] private AmbienceType type = AmbienceType.Rain;
        [SerializeField, Range(0, 1)] private float baseVolume = 0.5f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private bool playOnStart = true;
        [Tooltip("Ne joue que si ce drapeau est actif (ex. courant rétabli).")]
        [SerializeField] private string requiredFlag;

        private AudioSource source;

        public void Configure(AmbienceType ambience, float volume, float distance, string flag = null)
        {
            type = ambience; baseVolume = volume; maxDistance = distance; requiredFlag = flag;
        }

        private void Start()
        {
            var audio = AudioManager.EnsureExists();
            source = gameObject.AddComponent<AudioSource>();
            source.clip = audio.GetAmbience(type);
            source.loop = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = maxDistance;
            source.outputAudioMixerGroup = audio.SfxGroup;
            source.time = Random.Range(0f, source.clip.length * 0.9f);
            ApplyVolume();
            if (playOnStart && ConditionMet()) source.Play();
        }

        private void OnEnable()
        {
            GameSettings.Changed += ApplyVolume;
            GameEvents.FlagSet += OnFlag;
            GameEvents.StateRestored += Refresh;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplyVolume;
            GameEvents.FlagSet -= OnFlag;
            GameEvents.StateRestored -= Refresh;
        }

        private bool ConditionMet() =>
            string.IsNullOrEmpty(requiredFlag) ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.HasFlag(requiredFlag));

        private void OnFlag(string flag) { if (flag == requiredFlag) Refresh(); }

        private void Refresh()
        {
            if (source == null) return;
            if (ConditionMet() && !source.isPlaying) source.Play();
            else if (!ConditionMet() && source.isPlaying) source.Stop();
        }

        private void ApplyVolume()
        {
            if (source != null && AudioManager.Instance != null)
                source.volume = baseVolume * AudioManager.Instance.SfxVolumeFactor;
        }
    }
}
