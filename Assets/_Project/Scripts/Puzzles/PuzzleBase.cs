using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Base de toutes les énigmes : identifiant, indices à 3 niveaux, résolution, retours visuels et sonores.
    /// L'état « résolu » est stocké sous forme de drapeau « puzzle.&lt;id&gt; » (sauvegardé et restauré).
    /// Pour ajouter une énigme : hériter de cette classe, implémenter BuildUI/RefreshUI et appeler Solve().
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        [SerializeField] protected string puzzleId = "puzzle";
        [SerializeField] protected string displayName = "Énigme";
        [Tooltip("Trois niveaux d'aide : général, précis, solution.")]
        [TextArea(2, 4)] [SerializeField] protected string[] hints = new string[3];
        [SerializeField] protected string solvedMessage;
        [SerializeField] protected UnityEvent onSolved;

        protected UIPanel panel;
        private Image feedbackImage;

        public string PuzzleId => puzzleId;
        public string DisplayName => displayName;
        public string[] Hints => hints;
        public bool IsSolved => ProgressionManager.Instance != null && ProgressionManager.Instance.HasFlag("puzzle." + puzzleId);
        public bool IsOpen => panel != null && panel.IsOpen;

        protected virtual Vector2 PanelSize => new Vector2(1100, 820);
        /// <summary>Faux pour les énigmes sans interface (ex. recherche de clé).</summary>
        protected virtual bool HasUI => true;

        public void ConfigureBase(string id, string name, string[] hintTexts, string message)
        {
            puzzleId = id; displayName = name; hints = hintTexts; solvedMessage = message;
        }

        protected virtual void Awake() => PuzzleManager.Register(this);
        protected virtual void OnDestroy() => PuzzleManager.Unregister(this);

        public virtual void Open()
        {
            if (!HasUI) return;
            if (panel == null)
            {
                panel = UIPanel.Create(displayName, PanelSize);
                if (panel == null) return;
                feedbackImage = panel.GetComponent<Image>();
                BuildUI(panel.Body);
            }
            RefreshUI();
            panel.Open();
        }

        public void Close() => panel?.Close();

        protected virtual void BuildUI(RectTransform body) { }
        protected virtual void RefreshUI() { }

        protected void Solve()
        {
            if (IsSolved) return;
            AudioManager.Instance?.PlaySfx(SfxType.Success);
            Flash(UIStyle.Success);
            GameEvents.RaisePuzzleSolved(puzzleId);
            GameEvents.RaiseNotification(string.IsNullOrEmpty(solvedMessage) ? $"Énigme résolue : {displayName}" : solvedMessage);
            onSolved?.Invoke();
            GameEvents.RaiseJournalUpdated();
            RefreshUI();
        }

        protected void Fail(string message)
        {
            AudioManager.Instance?.PlaySfx(SfxType.Error);
            Flash(UIStyle.DarkRed);
            GameEvents.RaisePuzzleFailed(puzzleId);
            if (!string.IsNullOrEmpty(message)) GameEvents.RaiseNotification(message);
        }

        private void Flash(Color color)
        {
            if (feedbackImage != null && feedbackImage.isActiveAndEnabled) StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            Color original = UIStyle.PanelBackground;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                feedbackImage.color = Color.Lerp(color, original, t);
                yield return null;
            }
            feedbackImage.color = original;
        }
    }
}
