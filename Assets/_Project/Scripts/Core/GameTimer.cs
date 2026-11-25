using UnityEngine;

namespace LastEvidence
{
    /// <summary>Mesure le temps de jeu effectif (le temps en pause n'est pas compté).</summary>
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        public float Elapsed { get; set; }

        private void Awake() => Instance = this;

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Paused || gm.State == GameState.Ended)) return;
            Elapsed += Time.unscaledDeltaTime;
        }

        public static string Format(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
        }
    }
}
