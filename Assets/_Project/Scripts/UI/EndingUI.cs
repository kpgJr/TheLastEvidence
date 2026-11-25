using TMPro;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Écran de fin : résultat, temps, preuves, énigmes, indices et vérité sur l'affaire.</summary>
    public class EndingUI : MonoBehaviour
    {
        private void Start()
        {
            GameInput.EnsureCreated();
            AudioManager.EnsureExists().PlayMusic();
            if (!PlatformMode.IsVR) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

            string title = !EndingResult.HasResult ? "Fin de l'enquête" : EndingResult.Outcome switch
            {
                AccusationOutcome.Good => "Affaire résolue",
                AccusationOutcome.Incomplete => "Enquête incomplète",
                _ => "Erreur judiciaire"
            };
            var panel = UIPanel.Create(title, new Vector2(1500, 980), false);
            if (panel == null) return;
            panel.CloseOnCancel = false;

            var area = UIFactory.CreateRect("Text", panel.Body).Anchor(0, 0.12f, 1, 1);
            var text = UIFactory.CreateScrollText(area, out var scroll, 25);
            ((RectTransform)scroll.transform).Stretch();

            var sb = new System.Text.StringBuilder();
            if (EndingResult.HasResult)
            {
                sb.AppendLine($"Accusé(e) : <b>{EndingResult.AccusedName}</b> - mobile retenu : <b>{EndingResult.MotiveText}</b>\n");
                sb.AppendLine(EndingResult.OutcomeText + "\n");
                sb.AppendLine("<color=#D49A52><b>Bilan</b></color>");
                sb.AppendLine($"Temps de jeu : {GameTimer.Format(EndingResult.PlayTime)}");
                sb.AppendLine($"Preuves découvertes : {EndingResult.EvidenceFound} / {EndingResult.EvidenceTotal}");
                sb.AppendLine($"Preuves décisives présentées : {EndingResult.KeyEvidenceLinked}");
                sb.AppendLine($"Énigmes résolues : {EndingResult.PuzzlesSolved} / {EndingResult.PuzzlesTotal}");
                sb.AppendLine($"Indices utilisés : {EndingResult.HintsUsed}\n");
                sb.AppendLine("<color=#D49A52><b>La vérité sur l'affaire</b></color>");
                sb.AppendLine(EndingResult.TruthSummary);
            }
            else sb.AppendLine("Aucun résultat d'enquête disponible.");
            text.text = sb.ToString();

            var buttons = UIFactory.CreateRect("Buttons", panel.Body).Anchor(0.2f, 0, 0.8f, 0.1f);
            UIFactory.AddHorizontalLayout(buttons.gameObject, 30);
            UIFactory.CreateButton(buttons, "Menu principal", SceneLoader.LoadMainMenu, 28);
            UIFactory.CreateButton(buttons, "Nouvelle enquête", SceneLoader.NewGame, 28);
            panel.Open();
        }
    }
}
