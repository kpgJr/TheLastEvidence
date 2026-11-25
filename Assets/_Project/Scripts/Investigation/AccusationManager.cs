using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Énigme 6 : identification du responsable. Le joueur choisit un suspect et un mobile ;
    /// les preuves retenues sont celles associées au suspect sur le tableau d'enquête.
    /// Une confirmation est demandée : le joueur peut reprendre l'enquête avant de trancher.
    /// Une fois l'accusation confirmée, la porte principale se déverrouille (mécanisme de sortie).
    /// </summary>
    public class AccusationManager : PuzzleBase
    {
        public static AccusationManager Instance { get; private set; }
        public const string CaseClosedFlag = "case.closed";

        private int selectedSuspect = -1;
        private int selectedMotive = -1;
        private readonly List<Button> suspectButtons = new List<Button>();
        private readonly List<Button> motiveButtons = new List<Button>();
        private TextMeshProUGUI evidenceText;
        private TextMeshProUGUI statusText;
        private Button accuseButton;

        protected override Vector2 PanelSize => new Vector2(1500, 920);

        private CaseData Case => InvestigationManager.Instance != null ? InvestigationManager.Instance.Case : null;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => GameEvents.BoardChanged += OnBoardChanged;
        private void OnDisable() => GameEvents.BoardChanged -= OnBoardChanged;
        private void OnBoardChanged(string id) { if (IsOpen) RefreshUI(); }

        protected override void BuildUI(RectTransform body)
        {
            var data = Case;
            if (data == null) return;

            var suspectsCol = UIFactory.CreateRect("Suspects", body).Anchor(0, 0.22f, 0.32f, 1);
            UIFactory.AddVerticalLayout(suspectsCol.gameObject, 10, 6);
            UIFactory.CreateHeader(suspectsCol, "1. Le coupable");
            for (int i = 0; i < data.suspects.Length; i++)
            {
                int index = i;
                var s = data.suspects[i];
                var b = UIFactory.CreateButton(suspectsCol, $"{s.fullName}\n<size=70%>{s.role}</size>", () => { selectedSuspect = index; RefreshUI(); }, 26);
                b.GetComponent<LayoutElement>().preferredHeight = 100;
                suspectButtons.Add(b);
            }

            var motivesCol = UIFactory.CreateRect("Motives", body).Anchor(0.34f, 0.22f, 0.66f, 1);
            UIFactory.AddVerticalLayout(motivesCol.gameObject, 10, 6);
            UIFactory.CreateHeader(motivesCol, "2. Le mobile");
            for (int i = 0; i < data.motives.Length; i++)
            {
                int index = i;
                var b = UIFactory.CreateButton(motivesCol, data.motives[i], () => { selectedMotive = index; RefreshUI(); }, 24);
                b.GetComponent<LayoutElement>().preferredHeight = 84;
                motiveButtons.Add(b);
            }

            var evidenceCol = UIFactory.CreateRect("Evidence", body).Anchor(0.68f, 0.22f, 1, 1);
            UIFactory.CreateHeader(evidenceCol, "3. Les preuves").rectTransform.Anchor(0, 0.9f, 1, 1);
            var listArea = UIFactory.CreateRect("List", evidenceCol).Anchor(0, 0.15f, 1, 0.89f);
            evidenceText = UIFactory.CreateScrollText(listArea, out var scroll, 22);
            ((RectTransform)scroll.transform).Stretch();
            var boardButton = UIFactory.CreateButton(evidenceCol, "Ouvrir le tableau d'enquête", () => EvidenceBoard.Instance?.Open(), 22);
            ((RectTransform)boardButton.transform).Anchor(0, 0, 1, 0.13f);

            statusText = UIFactory.CreateText(body, "", 24, TextAlignmentOptions.Center, UIStyle.TextMuted);
            statusText.rectTransform.Anchor(0, 0.11f, 1, 0.21f);
            accuseButton = UIFactory.CreateButton(body, "Porter l'accusation", AskConfirmation, 30);
            accuseButton.GetComponent<Image>().color = UIStyle.DarkRed;
            ((RectTransform)accuseButton.transform).Anchor(0.32f, 0, 0.68f, 0.1f);
        }

        protected override void RefreshUI()
        {
            var data = Case;
            if (data == null || evidenceText == null) return;
            for (int i = 0; i < suspectButtons.Count; i++)
                suspectButtons[i].GetComponent<Image>().color = i == selectedSuspect ? UIStyle.Amber * 0.8f : UIStyle.ButtonNormal;
            for (int i = 0; i < motiveButtons.Count; i++)
                motiveButtons[i].GetComponent<Image>().color = i == selectedMotive ? UIStyle.Amber * 0.8f : UIStyle.ButtonNormal;

            if (selectedSuspect < 0)
            {
                evidenceText.text = "<i>Choisissez un suspect pour voir les preuves que vous lui avez associées sur le tableau d'enquête.</i>";
            }
            else
            {
                var ids = LinkedEvidence();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Preuves associées à <b>{data.suspects[selectedSuspect].fullName}</b> :\n");
                if (ids.Count == 0) sb.AppendLine("<i>Aucune. Utilisez le tableau d'enquête pour associer des preuves.</i>");
                foreach (var id in ids)
                {
                    var e = GameDatabase.Instance != null ? GameDatabase.Instance.FindEvidence(id) : null;
                    if (e != null) sb.AppendLine("• " + e.title);
                }
                evidenceText.text = sb.ToString();
            }

            bool closed = ProgressionManager.Instance != null && ProgressionManager.Instance.HasFlag(CaseClosedFlag);
            accuseButton.interactable = !closed && selectedSuspect >= 0 && selectedMotive >= 0;
            statusText.text = closed
                ? "L'accusation a été portée. La porte principale est ouverte : quittez le bâtiment."
                : "Une accusation est définitive. Prenez le temps de vérifier vos preuves.";
        }

        private List<string> LinkedEvidence()
        {
            if (selectedSuspect < 0 || InvestigationManager.Instance == null) return new List<string>();
            return InvestigationManager.Instance.GetEvidenceLinkedTo(Case.suspects[selectedSuspect].id);
        }

        private void AskConfirmation()
        {
            var data = Case;
            if (data == null || selectedSuspect < 0 || selectedMotive < 0) return;
            int count = LinkedEvidence().Count;
            string message = $"Vous accusez <b>{data.suspects[selectedSuspect].fullName}</b>\n" +
                             $"Mobile : <b>{data.motives[selectedMotive]}</b>\n" +
                             $"Preuves présentées : <b>{count}</b>\n\n" +
                             "Cette décision est définitive. Confirmer l'accusation ?";
            ConfirmDialog.Instance?.Show(message, "Confirmer l'accusation", "Reprendre l'enquête", ConfirmAccusation, null);
        }

        /// <summary>Porte directement une accusation confirmée (tests automatisés, débogage).</summary>
        public AccusationOutcome Accuse(int suspectIndex, int motiveIndex)
        {
            selectedSuspect = suspectIndex;
            selectedMotive = motiveIndex;
            ConfirmAccusation();
            return EndingResult.Outcome;
        }

        private void ConfirmAccusation()
        {
            var data = Case;
            var inv = InvestigationManager.Instance;
            if (data == null || inv == null) return;
            inv.AccusationAttempts++;
            var suspect = data.suspects[selectedSuspect];
            var outcome = AccusationEvaluator.Evaluate(data, suspect.id, selectedMotive, LinkedEvidence(), out int keyCount);

            EndingResult.HasResult = true;
            EndingResult.Outcome = outcome;
            EndingResult.AccusedName = suspect.fullName;
            EndingResult.MotiveText = data.motives[selectedMotive];
            EndingResult.KeyEvidenceLinked = keyCount;
            EndingResult.OutcomeText = outcome switch
            {
                AccusationOutcome.Good => data.goodEndingText,
                AccusationOutcome.Incomplete => data.incompleteEndingText,
                _ => data.badEndingText
            };
            EndingResult.TruthSummary = data.truthSummary;

            if (outcome == AccusationOutcome.Good) Solve();
            ProgressionManager.Instance?.SetFlag(CaseClosedFlag);
            GameEvents.RaiseNotification("Accusation enregistrée. La porte principale est déverrouillée.");
            UIManager.Instance?.CloseAll();
        }

        /// <summary>Appelé par la zone de sortie : calcule les statistiques et charge la scène de fin.</summary>
        public void FinishGame()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Ended) return;
            var db = GameDatabase.Instance;
            EndingResult.PlayTime = GameTimer.Instance != null ? GameTimer.Instance.Elapsed : 0;
            EndingResult.EvidenceFound = EvidenceManager.Instance != null ? EvidenceManager.Instance.Count : 0;
            EndingResult.EvidenceTotal = db != null ? db.evidence.Count : 0;
            EndingResult.PuzzlesSolved = PuzzleManager.SolvedCount;
            EndingResult.PuzzlesTotal = PuzzleManager.TotalCount;
            EndingResult.HintsUsed = HintManager.Instance != null ? HintManager.Instance.TotalHintsUsed : 0;
            GameManager.Instance?.SetState(GameState.Ended);
            SaveManager.DeleteSave();
            SceneLoader.LoadEnding();
        }
    }
}
