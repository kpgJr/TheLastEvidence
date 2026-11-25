using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Panneau modal générique (inventaire, journal, énigmes, documents...).
    /// L'ouverture/fermeture passe par UIManager qui gère la pile et l'état du jeu.
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        public event Action Opened;
        public event Action Closed;

        public RectTransform Rect => (RectTransform)transform;
        public RectTransform Body { get; private set; }
        public bool IsOpen => gameObject.activeSelf;
        public bool PausesGame { get; set; }
        public bool CloseOnCancel { get; set; } = true;

        /// <summary>Crée un panneau centré avec un fond, un titre et un bouton Fermer.</summary>
        public static UIPanel Create(string title, Vector2 size, bool withCloseButton = true)
        {
            var root = UIRoot.Instance != null ? UIRoot.Instance.ModalRoot : null;
            if (root == null)
            {
                Debug.LogError("UIRoot manquant dans la scène.");
                return null;
            }
            var bg = UIFactory.CreateImage("Panel_" + title, root, UIStyle.PanelBackground);
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = UIStyle.Amber * 0.6f;
            outline.effectDistance = new Vector2(2, -2);

            var panel = bg.gameObject.AddComponent<UIPanel>();
            var header = UIFactory.CreateText(rt, title, 38, TMPro.TextAlignmentOptions.Center, UIStyle.Amber);
            header.fontStyle = TMPro.FontStyles.Bold;
            header.rectTransform.anchorMin = new Vector2(0, 1);
            header.rectTransform.anchorMax = new Vector2(1, 1);
            header.rectTransform.pivot = new Vector2(0.5f, 1);
            header.rectTransform.sizeDelta = new Vector2(-40, 64);
            header.rectTransform.anchoredPosition = new Vector2(0, -12);

            panel.Body = UIFactory.CreateRect("Body", rt).Stretch(24, withCloseButton ? 86 : 24, 24, 84);

            if (withCloseButton)
            {
                var close = UIFactory.CreateButton(rt, "Fermer", panel.Close, 26);
                var crt = (RectTransform)close.transform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
                crt.pivot = new Vector2(0.5f, 0);
                crt.sizeDelta = new Vector2(260, 58);
                crt.anchoredPosition = new Vector2(0, 16);
            }
            bg.gameObject.SetActive(false);
            return panel;
        }

        public void Open()
        {
            if (IsOpen) return;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            UIManager.Instance?.OnPanelOpened(this);
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            UIManager.Instance?.OnPanelClosed(this);
            Closed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }
    }
}
