using UnityEngine;

namespace LastEvidence
{
    /// <summary>Crée l'AudioManager si besoin et lance la musique d'ambiance de la scène.</summary>
    public class SceneAudioStarter : MonoBehaviour
    {
        [SerializeField] private bool playMusic = true;

        private void Start()
        {
            var audio = AudioManager.EnsureExists();
            if (playMusic) audio.PlayMusic();
        }
    }
}
