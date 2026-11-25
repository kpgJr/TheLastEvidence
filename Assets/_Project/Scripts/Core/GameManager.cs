using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Gère uniquement l'état global (jeu, menu, examen, pause, fin), le curseur et l'échelle de temps.
    /// La logique métier est répartie dans les autres gestionnaires.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>Vrai si le joueur peut se déplacer et interagir avec le monde.</summary>
        public bool IsGameplayActive => State == GameState.Playing;

        private void Awake()
        {
            Instance = this;
            GameInput.EnsureCreated();
            Time.timeScale = 1f;
            ApplyCursor();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;
            ApplyCursor();
            GameEvents.RaiseGameStateChanged(newState);
        }

        private void ApplyCursor()
        {
            if (PlatformMode.IsVR) return;
            bool locked = State == GameState.Playing;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyCursor();
        }
    }
}
