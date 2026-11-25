using System;
using TMPro;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Fenêtre de lecture des documents, écrans et messages.</summary>
    public class DocumentUI : MonoBehaviour
    {
        public static DocumentUI Instance { get; private set; }

        private UIPanel panel;
        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private UnityEngine.UI.ScrollRect scroll;
        private Action onClosed;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build()
        {
            panel = UIPanel.Create("Document", new Vector2(1150, 900));
            if (panel == null) return;
            title = panel.GetComponentInChildren<TextMeshProUGUI>();
            var paper = UIFactory.CreateImage("Paper", panel.Body, new Color(0.77f, 0.71f, 0.6f, 1f));
            paper.rectTransform.Stretch();
            body = UIFactory.CreateScrollText(paper.transform, out scroll, 27);
            ((RectTransform)scroll.transform).Stretch(16, 16, 16, 16);
            scroll.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            body.color = new Color(0.12f, 0.1f, 0.08f);
            panel.Closed += () =>
            {
                var callback = onClosed;
                onClosed = null;
                callback?.Invoke();
            };
        }

        public void Show(string docTitle, string text, Action closedCallback = null)
        {
            if (panel == null) Build();
            if (panel == null) return;
            title.text = docTitle;
            body.text = text;
            onClosed = closedCallback;
            scroll.verticalNormalizedPosition = 1f;
            panel.Open();
        }
    }
}
