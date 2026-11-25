using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Paramètres : volumes, sensibilité, luminosité, résolution, plein écran, qualité, remappage clavier.
    /// Les options d'affichage et de clavier sont masquées en VR.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance { get; private set; }

        private UIPanel panel;
        private readonly List<(InputAction action, Button button)> rebindButtons = new List<(InputAction, Button)>();
        private InputActionRebindingExtensions.RebindingOperation rebinding;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            rebinding?.Dispose();
            if (Instance == this) Instance = null;
        }

        public void Open()
        {
            if (panel == null) Build();
            panel?.Open();
        }

        private void Build()
        {
            panel = UIPanel.Create("Paramètres", new Vector2(1200, 960));
            if (panel == null) return;
            panel.Closed += GameSettings.Save;
            var content = UIFactory.CreateScrollList(panel.Body, out var scroll, 8);
            ((RectTransform)scroll.transform).Stretch();

            UIFactory.CreateHeader(content, "Audio");
            UIFactory.CreateSlider(content, "Volume général", 0, 1, GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
            UIFactory.CreateSlider(content, "Musique", 0, 1, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            UIFactory.CreateSlider(content, "Effets sonores", 0, 1, GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);

            UIFactory.CreateHeader(content, "Affichage");
            UIFactory.CreateSlider(content, "Luminosité", -1f, 1.5f, GameSettings.Brightness, v => GameSettings.Brightness = v);
            UIFactory.CreateCycleRow(content, "Qualité graphique",
                () => QualitySettings.names.Length > 0 ? QualitySettings.names[Mathf.Clamp(GameSettings.QualityLevel, 0, QualitySettings.names.Length - 1)] : "-",
                () => GameSettings.QualityLevel = (GameSettings.QualityLevel + 1) % Mathf.Max(1, QualitySettings.names.Length));

            if (!PlatformMode.IsVR)
            {
                UIFactory.CreateCycleRow(content, "Résolution", ResolutionLabel, CycleResolution);
                UIFactory.CreateCycleRow(content, "Plein écran", () => GameSettings.Fullscreen ? "Oui" : "Non",
                    () => GameSettings.Fullscreen = !GameSettings.Fullscreen);

                UIFactory.CreateHeader(content, "Contrôles");
                UIFactory.CreateSlider(content, "Sensibilité souris", 0.2f, 3f, GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v);
                var note = UIFactory.CreateText(content,
                    "Les déplacements utilisent la position physique des touches : ZQSD sur clavier AZERTY, WASD sur QWERTY (flèches aussi acceptées).",
                    20, TextAlignmentOptions.Left, UIStyle.TextMuted);
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
                foreach (var action in GameInput.RebindableActions)
                {
                    var a = action;
                    Button b = UIFactory.CreateCycleRow(content, ActionLabel(a.name), () => GameInput.GetKeyboardDisplay(a), () => { });
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => StartRebind(a, b));
                    rebindButtons.Add((a, b));
                }
                UIFactory.CreateButton(content, "Réinitialiser les touches", () =>
                {
                    GameInput.ResetBindingOverrides();
                    RefreshRebindLabels();
                }, 24);
            }
            else
            {
                var vrHelp = UIFactory.CreateText(content,
                    "<b>Commandes VR (Meta Quest)</b>\nJoystick gauche : se déplacer (clic : courir)\nJoystick droit : tourner\n" +
                    "Gâchette droite : interagir / cliquer\nGâchette gauche : lampe torche\nA : inventaire   Y : journal\n" +
                    "X : examiner   B : retour   Menu : pause",
                    24, TextAlignmentOptions.Left);
                vrHelp.gameObject.AddComponent<LayoutElement>().preferredHeight = 260;
            }
        }

        private static string ActionLabel(string actionName) => actionName switch
        {
            "Interact" => "Interagir",
            "Run" => "Courir",
            "Crouch" => "S'accroupir",
            "Flashlight" => "Lampe torche",
            "Inventory" => "Inventaire",
            "Journal" => "Journal",
            "Examine" => "Examiner",
            _ => actionName
        };

        private static string ResolutionLabel()
        {
            var res = Screen.resolutions;
            int i = GameSettings.ResolutionIndex;
            if (i >= 0 && i < res.Length) return $"{res[i].width} × {res[i].height}";
            return $"{Screen.width} × {Screen.height}";
        }

        private static void CycleResolution()
        {
            var res = Screen.resolutions;
            if (res.Length == 0) return;
            GameSettings.ResolutionIndex = (GameSettings.ResolutionIndex + 1) % res.Length;
        }

        private void StartRebind(InputAction action, Button button)
        {
            int index = GameInput.GetKeyboardBindingIndex(action);
            if (index < 0) return;
            rebinding?.Dispose();
            UIFactory.SetButtonLabel(button, "Appuyez sur une touche…");
            action.Disable();
            rebinding = action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => FinishRebind(action))
                .OnCancel(op => FinishRebind(action))
                .Start();
        }

        private void FinishRebind(InputAction action)
        {
            rebinding?.Dispose();
            rebinding = null;
            action.Enable();
            GameInput.SaveBindingOverrides();
            RefreshRebindLabels();
        }

        private void RefreshRebindLabels()
        {
            foreach (var (action, button) in rebindButtons)
                UIFactory.SetButtonLabel(button, GameInput.GetKeyboardDisplay(action));
        }
    }
}
