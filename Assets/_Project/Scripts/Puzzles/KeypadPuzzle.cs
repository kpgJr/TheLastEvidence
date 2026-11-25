using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Énigme 2 : clavier numérique (coffre-fort). Code configurable dans l'Inspector.</summary>
    public class KeypadPuzzle : PuzzleBase
    {
        [SerializeField] private string code = "1403";

        internal static readonly Key[] DigitKeys =
            { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        internal static readonly Key[] NumpadKeys =
            { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        private string entry = string.Empty;
        private TextMeshProUGUI display;
        private TextMeshProUGUI status;

        protected override Vector2 PanelSize => new Vector2(640, 900);

        public void Configure(string secretCode) => code = secretCode;

        /// <summary>Vérifie un code (logique pure, testée en Edit Mode).</summary>
        public static bool Check(string input, string secret) => !string.IsNullOrEmpty(secret) && input == secret;

        protected override void BuildUI(RectTransform body)
        {
            var screen = UIFactory.CreateImage("Screen", body, UIStyle.DeepBlack);
            screen.rectTransform.Anchor(0.05f, 0.8f, 0.95f, 1f);
            display = UIFactory.CreateText(screen.transform, "", 64, TextAlignmentOptions.Center, UIStyle.Amber);
            display.rectTransform.Stretch();
            status = UIFactory.CreateText(body, "", 24, TextAlignmentOptions.Center, UIStyle.TextMuted);
            status.rectTransform.Anchor(0, 0.72f, 1, 0.8f);

            var grid = UIFactory.CreateRect("Keys", body).Anchor(0.05f, 0f, 0.95f, 0.71f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(150, 105);
            layout.spacing = new Vector2(18, 14);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Eff.", "0", "OK" };
            foreach (var k in keys)
            {
                string key = k;
                var b = UIFactory.CreateButton(grid, key, () => Press(key), 44);
                if (key == "OK") b.GetComponent<Image>().color = UIStyle.DesaturatedBlue;
                if (key == "Eff.") b.GetComponent<Image>().color = UIStyle.DarkRed;
            }
        }

        protected override void RefreshUI()
        {
            if (display == null) return;
            if (IsSolved)
            {
                display.text = "OUVERT";
                status.text = "Le coffre est déverrouillé.";
                return;
            }
            display.text = string.Join(" ", entry.PadRight(code.Length, '_').ToCharArray());
            status.text = $"Entrez le code à {code.Length} chiffres";
        }

        /// <summary>Soumet un code complet (tests automatisés, débogage). Retourne vrai si correct.</summary>
        public bool Submit(string input)
        {
            if (IsSolved) return true;
            if (Check(input, code)) { Solve(); return true; }
            Fail("Code incorrect.");
            return false;
        }

        private void Press(string key)
        {
            if (IsSolved) return;
            AudioManager.Instance?.PlaySfx(SfxType.KeypadBeep);
            if (key == "Eff.")
            {
                if (entry.Length > 0) entry = entry.Substring(0, entry.Length - 1);
            }
            else if (key == "OK")
            {
                if (Check(entry, code)) { Solve(); }
                else { Fail("Code incorrect."); entry = string.Empty; }
            }
            else if (entry.Length < code.Length)
            {
                entry += key;
            }
            RefreshUI();
        }

        private void Update()
        {
            // Saisie au clavier physique (mode bureau).
            if (!IsOpen || IsSolved || Keyboard.current == null) return;
            for (int d = 0; d <= 9; d++)
            {
                if (Keyboard.current[DigitKeys[d]].wasPressedThisFrame || Keyboard.current[NumpadKeys[d]].wasPressedThisFrame)
                    Press(d.ToString());
            }
            if (Keyboard.current.backspaceKey.wasPressedThisFrame) Press("Eff.");
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) Press("OK");
        }
    }
}
