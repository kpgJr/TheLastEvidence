using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Interface en jeu : réticule, indication d'interaction, objectif courant, aide des commandes.</summary>
    public class HUDManager : MonoBehaviour
    {
        private TextMeshProUGUI promptText;
        private TextMeshProUGUI objectiveText;
        private Image crosshair;
        private GameObject root;

        private void Start()
        {
            if (UIRoot.Instance == null) return;
            root = UIFactory.CreateRect("HUD", UIRoot.Instance.HudRoot).Stretch().gameObject;
            bool vr = PlatformMode.IsVR;

            if (!vr)
            {
                crosshair = UIFactory.CreateImage("Crosshair", root.transform, new Color(1, 1, 1, 0.55f));
                crosshair.raycastTarget = false;
                crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                crosshair.rectTransform.sizeDelta = new Vector2(6, 6);
            }

            promptText = UIFactory.CreateText(root.transform, "", vr ? 34 : 28, TextAlignmentOptions.Center);
            promptText.rectTransform.Anchor(0.25f, vr ? 0.3f : 0.36f, 0.75f, vr ? 0.42f : 0.46f);
            promptText.outlineWidth = 0.2f;
            promptText.outlineColor = Color.black;

            var objectiveBg = UIFactory.CreateImage("Objective", root.transform, new Color(0, 0, 0, 0.45f));
            objectiveBg.raycastTarget = false;
            objectiveBg.rectTransform.Anchor(vr ? 0.22f : 0.01f, vr ? 0.8f : 0.9f, vr ? 0.78f : 0.38f, vr ? 0.9f : 0.985f);
            objectiveText = UIFactory.CreateText(objectiveBg.transform, "", vr ? 28 : 24, TextAlignmentOptions.MidlineLeft);
            objectiveText.rectTransform.Stretch(16, 4, 16, 4);

            if (!vr)
            {
                var help = UIFactory.CreateText(root.transform, ControlsHelp(), 18, TextAlignmentOptions.BottomRight, UIStyle.TextMuted);
                help.rectTransform.Anchor(0.6f, 0.01f, 0.99f, 0.08f);
            }

            OnObjective(ObjectiveManager.Instance != null && ObjectiveManager.Instance.Current != null
                ? ObjectiveManager.Instance.Current.text : "");
        }

        private static string ControlsHelp()
        {
            string K(UnityEngine.InputSystem.InputAction a) => GameInput.GetKeyboardDisplay(a);
            return $"{K(GameInput.Inventory)} Inventaire   {K(GameInput.Journal)} Journal   " +
                   $"{K(GameInput.Flashlight)} Lampe   {K(GameInput.Examine)} Examiner   Échap Pause";
        }

        private void OnEnable()
        {
            GameEvents.InteractionPromptChanged += OnPrompt;
            GameEvents.ObjectiveChanged += OnObjective;
            GameEvents.GameStateChanged += OnState;
        }

        private void OnDisable()
        {
            GameEvents.InteractionPromptChanged -= OnPrompt;
            GameEvents.ObjectiveChanged -= OnObjective;
            GameEvents.GameStateChanged -= OnState;
        }

        private void OnPrompt(string prompt)
        {
            if (promptText != null) promptText.text = prompt ?? string.Empty;
        }

        private void OnObjective(string objective)
        {
            if (objectiveText != null)
                objectiveText.text = string.IsNullOrEmpty(objective) ? "" : $"<color=#D49A52>Objectif :</color> {objective}";
        }

        private void OnState(GameState state)
        {
            if (root == null) return;
            bool playing = state == GameState.Playing;
            if (crosshair != null) crosshair.enabled = playing;
            if (promptText != null && !playing) promptText.text = "";
            if (objectiveText != null) objectiveText.transform.parent.gameObject.SetActive(state != GameState.Inspecting);
        }
    }
}
