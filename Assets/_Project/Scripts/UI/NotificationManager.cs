using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Affiche les notifications (preuve découverte, objet ajouté, sauvegarde...) l'une après l'autre.</summary>
    public class NotificationManager : MonoBehaviour
    {
        [SerializeField] private float displayTime = 2.8f;

        private readonly Queue<string> queue = new Queue<string>();
        private TextMeshProUGUI text;
        private CanvasGroup group;
        private bool showing;

        private void Start()
        {
            if (UIRoot.Instance == null) return;
            bool vr = PlatformMode.IsVR;
            var bg = UIFactory.CreateImage("Notification", UIRoot.Instance.HudRoot, new Color(0.06f, 0.07f, 0.08f, 0.85f));
            bg.raycastTarget = false;
            bg.rectTransform.Anchor(vr ? 0.25f : 0.33f, vr ? 0.62f : 0.82f, vr ? 0.75f : 0.67f, vr ? 0.74f : 0.9f);
            group = bg.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            text = UIFactory.CreateText(bg.transform, "", vr ? 30 : 26, TextAlignmentOptions.Center, UIStyle.OldBeige);
            text.rectTransform.Stretch(12, 4, 12, 4);
        }

        private void OnEnable() => GameEvents.Notification += Enqueue;
        private void OnDisable() => GameEvents.Notification -= Enqueue;

        private void Enqueue(string message)
        {
            if (string.IsNullOrEmpty(message) || SaveManager.IsRestoring) return;
            if (queue.Count > 6) queue.Dequeue();
            queue.Enqueue(message);
            if (!showing && isActiveAndEnabled) StartCoroutine(ShowQueue());
        }

        private IEnumerator ShowQueue()
        {
            showing = true;
            while (queue.Count > 0)
            {
                string msg = queue.Dequeue();
                if (text == null) break;
                text.text = msg;
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f) { group.alpha = t; yield return null; }
                group.alpha = 1;
                yield return new WaitForSecondsRealtime(queue.Count > 2 ? displayTime * 0.6f : displayTime);
                for (float t = 1; t > 0; t -= Time.unscaledDeltaTime / 0.3f) { group.alpha = t; yield return null; }
                group.alpha = 0;
            }
            showing = false;
        }
    }
}
