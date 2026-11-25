using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastEvidence
{
    [Serializable]
    public class EmailEntry
    {
        public string from;
        public string subject;
        [TextArea(4, 12)] public string body;
        public EvidenceData evidence;
    }

    /// <summary>
    /// Énigme 3 : mot de passe de l'ordinateur. Clavier virtuel (utilisable en VR) + saisie physique.
    /// Une fois déverrouillé, l'ordinateur affiche les courriels de la victime.
    /// </summary>
    public class PasswordPuzzle : PuzzleBase
    {
        [SerializeField] private string password = "REX1998";
        [SerializeField] private string userName = "Victor Lemaire";
        [SerializeField] private List<EmailEntry> emails = new List<EmailEntry>();

        private string entry = string.Empty;
        private GameObject loginView;
        private GameObject desktopView;
        private TextMeshProUGUI field;
        private TextMeshProUGUI status;
        private TextMeshProUGUI emailBody;
        private RectTransform emailList;

        protected override Vector2 PanelSize => new Vector2(1400, 900);

        public void Configure(string pwd, string user, List<EmailEntry> mails)
        {
            password = pwd; userName = user; emails = mails;
        }

        /// <summary>Comparaison insensible à la casse et aux espaces.</summary>
        public static bool Check(string input, string secret) =>
            !string.IsNullOrEmpty(secret) && input != null &&
            string.Equals(input.Trim(), secret.Trim(), StringComparison.OrdinalIgnoreCase);

        protected override void BuildUI(RectTransform body)
        {
            // --- Écran de connexion ---
            loginView = UIFactory.CreateRect("Login", body).Stretch().gameObject;
            var user = UIFactory.CreateText(loginView.transform, $"Session : <b>{userName}</b>", 30, TextAlignmentOptions.Center);
            user.rectTransform.Anchor(0, 0.88f, 1, 1);
            var fieldBg = UIFactory.CreateImage("Field", loginView.transform, UIStyle.DeepBlack);
            fieldBg.rectTransform.Anchor(0.25f, 0.76f, 0.75f, 0.87f);
            field = UIFactory.CreateText(fieldBg.transform, "", 40, TextAlignmentOptions.Center, UIStyle.Amber);
            field.rectTransform.Stretch();
            status = UIFactory.CreateText(loginView.transform, "", 24, TextAlignmentOptions.Center, UIStyle.TextMuted);
            status.rectTransform.Anchor(0, 0.69f, 1, 0.76f);

            var grid = UIFactory.CreateRect("Keyboard", loginView.transform).Anchor(0.02f, 0f, 0.98f, 0.68f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(112, 86);
            layout.spacing = new Vector2(10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            const string keys = "1234567890AZERTYUIOPQSDFGHJKLMWXCVBN";
            foreach (char c in keys)
            {
                string k = c.ToString();
                UIFactory.CreateButton(grid, k, () => Type(k), 34);
            }
            var back = UIFactory.CreateButton(grid, "Eff.", Backspace, 34);
            back.GetComponent<Image>().color = UIStyle.DarkRed;
            var clear = UIFactory.CreateButton(grid, "Effacer", () => { entry = ""; RefreshUI(); }, 24);
            clear.GetComponent<Image>().color = UIStyle.DarkRed;
            var ok = UIFactory.CreateButton(grid, "Valider", Submit, 26);
            ok.GetComponent<Image>().color = UIStyle.DesaturatedBlue;

            // --- Bureau : courriels ---
            desktopView = UIFactory.CreateRect("Desktop", body).Stretch().gameObject;
            var listBg = UIFactory.CreateRect("Inbox", desktopView.transform).Anchor(0, 0, 0.34f, 1);
            emailList = UIFactory.CreateScrollList(listBg, out _);
            ((RectTransform)emailList.parent).Stretch();
            var bodyBg = UIFactory.CreateRect("Mail", desktopView.transform).Anchor(0.36f, 0, 1, 1);
            emailBody = UIFactory.CreateScrollText(bodyBg, out var scroll, 26);
            ((RectTransform)scroll.transform).Stretch();
            emailBody.text = "<i>Sélectionnez un courriel.</i>";
            for (int i = 0; i < emails.Count; i++)
            {
                int index = i;
                var b = UIFactory.CreateButton(emailList, $"{emails[i].subject}\n<size=70%>{emails[i].from}</size>", () => ShowEmail(index), 22);
                b.GetComponent<LayoutElement>().preferredHeight = 96;
            }
        }

        /// <summary>Soumet un mot de passe (tests automatisés, débogage). Retourne vrai si correct.</summary>
        public bool Submit(string input)
        {
            if (IsSolved) return true;
            if (Check(input, password)) { Solve(); return true; }
            Fail("Mot de passe incorrect.");
            return false;
        }

        /// <summary>Ouvre un courriel (enregistre la preuve associée).</summary>
        public void ReadEmail(int index)
        {
            if (!IsSolved || index < 0 || index >= emails.Count) return;
            if (emails[index].evidence != null) EvidenceManager.Instance?.Discover(emails[index].evidence);
        }

        public int EmailCount => emails.Count;

        private void ShowEmail(int index)
        {
            var mail = emails[index];
            emailBody.text = $"<color=#D49A52><b>{mail.subject}</b></color>\n<size=85%>De : {mail.from}</size>\n\n{mail.body}";
            AudioManager.Instance?.PlaySfx(SfxType.Computer, 0.5f);
            if (mail.evidence != null) EvidenceManager.Instance?.Discover(mail.evidence);
        }

        protected override void RefreshUI()
        {
            if (loginView == null) return;
            loginView.SetActive(!IsSolved);
            desktopView.SetActive(IsSolved);
            field.text = entry.Length == 0 ? "<color=#555>mot de passe</color>" : entry;
            if (string.IsNullOrEmpty(status.text) || IsSolved) status.text = "Saisissez le mot de passe";
        }

        private void Type(string k)
        {
            if (entry.Length >= 20) return;
            entry += k;
            AudioManager.Instance?.PlaySfx(SfxType.Click);
            RefreshUI();
        }

        private void Backspace()
        {
            if (entry.Length > 0) entry = entry.Substring(0, entry.Length - 1);
            RefreshUI();
        }

        private void Submit()
        {
            if (Check(entry, password))
            {
                AudioManager.Instance?.PlaySfx(SfxType.Computer);
                Solve();
            }
            else
            {
                status.text = "Mot de passe incorrect.";
                Fail("Mot de passe incorrect.");
                entry = string.Empty;
            }
            RefreshUI();
        }

        private void OnEnable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
        }

        private void OnDisable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
        }

        private void OnTextInput(char c)
        {
            if (!IsOpen || IsSolved || PlatformMode.IsVR) return;
            if (char.IsLetterOrDigit(c)) Type(char.ToUpperInvariant(c).ToString());
        }

        private void Update()
        {
            if (!IsOpen || IsSolved || Keyboard.current == null || PlatformMode.IsVR) return;
            if (Keyboard.current.backspaceKey.wasPressedThisFrame) Backspace();
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) Submit();
        }
    }
}
