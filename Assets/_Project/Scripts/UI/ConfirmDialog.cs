using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Boîte de dialogue de confirmation (accusation, retour au menu, nouvelle partie).</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }

        private UIPanel panel;
        private TextMeshProUGUI message;
        private Button confirmButton;
        private Button cancelButton;
        private Action onConfirm;
        private Action onCancel;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build()
        {
            panel = UIPanel.Create("Confirmation", new Vector2(1000, 560), false);
            if (panel == null) return;
            message = UIFactory.CreateText(panel.Body, "", 28, TextAlignmentOptions.Center);
            message.rectTransform.Anchor(0, 0.3f, 1, 1);
            confirmButton = UIFactory.CreateButton(panel.Body, "Confirmer", () => Choose(true), 26);
            confirmButton.GetComponent<Image>().color = UIStyle.DarkRed;
            ((RectTransform)confirmButton.transform).Anchor(0.05f, 0.02f, 0.48f, 0.22f);
            cancelButton = UIFactory.CreateButton(panel.Body, "Annuler", () => Choose(false), 26);
            ((RectTransform)cancelButton.transform).Anchor(0.52f, 0.02f, 0.95f, 0.22f);
        }

        public void Show(string text, string confirmLabel, string cancelLabel, Action confirm, Action cancel)
        {
            if (panel == null) Build();
            if (panel == null) return;
            message.text = text;
            UIFactory.SetButtonLabel(confirmButton, confirmLabel);
            UIFactory.SetButtonLabel(cancelButton, cancelLabel);
            onConfirm = confirm;
            onCancel = cancel;
            panel.Open();
        }

        private void Choose(bool confirmed)
        {
            var action = confirmed ? onConfirm : onCancel;
            onConfirm = null;
            onCancel = null;
            panel.Close();
            action?.Invoke();
        }
    }
}
