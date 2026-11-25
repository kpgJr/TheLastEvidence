using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Tableau d'enquête : visualise suspects, preuves, mobiles et événements,
    /// et permet d'associer chaque preuve à un suspect (associations vérifiées lors de l'accusation).
    /// </summary>
    public class EvidenceBoard : MonoBehaviour
    {
        public static EvidenceBoard Instance { get; private set; }

        private UIPanel panel;
        private RectTransform evidenceList;
        private readonly List<TextMeshProUGUI> suspectColumns = new List<TextMeshProUGUI>();
        private TextMeshProUGUI detailText;
        private EvidenceData selected;

        public UIPanel Panel => panel;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            GameEvents.EvidenceDiscovered += OnChanged;
            GameEvents.BoardChanged += OnBoardChanged;
        }

        private void OnDisable()
        {
            GameEvents.EvidenceDiscovered -= OnChanged;
            GameEvents.BoardChanged -= OnBoardChanged;
        }

        private void OnChanged(EvidenceData e) { if (panel != null && panel.IsOpen) Refresh(); }
        private void OnBoardChanged(string id) { if (panel != null && panel.IsOpen) Refresh(); }

        public void Open()
        {
            if (panel == null) Build();
            if (panel == null) return;
            Refresh();
            panel.Open();
        }

        private CaseData Case => InvestigationManager.Instance != null ? InvestigationManager.Instance.Case : null;

        private void Build()
        {
            var data = Case;
            if (data == null) return;
            panel = UIPanel.Create("Tableau d'enquête", new Vector2(1560, 960));
            if (panel == null) return;
            var body = panel.Body;

            var left = UIFactory.CreateRect("EvidenceColumn", body).Anchor(0, 0.25f, 0.3f, 1);
            UIFactory.CreateHeader(left, "Preuves").rectTransform.Anchor(0, 0.9f, 1, 1);
            var listArea = UIFactory.CreateRect("ListArea", left).Anchor(0, 0, 1, 0.89f);
            evidenceList = UIFactory.CreateScrollList(listArea, out var scroll, 6);
            ((RectTransform)scroll.transform).Stretch();

            float width = 0.68f / data.suspects.Length;
            for (int i = 0; i < data.suspects.Length; i++)
            {
                var s = data.suspects[i];
                float x0 = 0.32f + i * width;
                var col = UIFactory.CreateImage("Suspect_" + s.id, body, UIStyle.Anthracite);
                col.rectTransform.Anchor(x0, 0.25f, x0 + width - 0.01f, 1);
                var portrait = UIFactory.CreateImage("Portrait", col.transform, s.portraitColor);
                portrait.rectTransform.Anchor(0.3f, 0.78f, 0.7f, 0.98f);
                var initials = UIFactory.CreateText(portrait.transform, Initials(s.fullName), 48, TextAlignmentOptions.Center, UIStyle.DeepBlack);
                initials.rectTransform.Stretch();
                var name = UIFactory.CreateText(col.transform, $"<b>{s.fullName}</b>\n<size=75%>{s.role}</size>", 24, TextAlignmentOptions.Center);
                name.rectTransform.Anchor(0, 0.66f, 1, 0.78f);
                var linked = UIFactory.CreateText(col.transform, "", 19, TextAlignmentOptions.TopLeft);
                linked.rectTransform.Anchor(0.04f, 0.12f, 0.96f, 0.65f);
                suspectColumns.Add(linked);
                string suspectId = s.id;
                var link = UIFactory.CreateButton(col.transform, "Associer la preuve ici", () => LinkSelected(suspectId), 20);
                ((RectTransform)link.transform).Anchor(0.05f, 0.01f, 0.95f, 0.1f);
            }

            var detailBg = UIFactory.CreateImage("Details", body, new Color(0, 0, 0, 0.3f));
            detailBg.rectTransform.Anchor(0, 0, 0.82f, 0.23f);
            detailText = UIFactory.CreateText(detailBg.transform, "", 22);
            detailText.rectTransform.Stretch(14, 8, 14, 8);
            var unlink = UIFactory.CreateButton(body, "Retirer l'association", () => LinkSelected(null), 20);
            ((RectTransform)unlink.transform).Anchor(0.83f, 0.06f, 1, 0.17f);
        }

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            string r = "";
            foreach (var p in parts) if (p.Length > 0) r += p[0];
            return r;
        }

        private void LinkSelected(string suspectId)
        {
            if (selected == null)
            {
                GameEvents.RaiseNotification("Sélectionnez d'abord une preuve.");
                return;
            }
            InvestigationManager.Instance?.Link(selected.id, suspectId);
            AudioManager.Instance?.PlaySfx(SfxType.Paper, 0.7f);
        }

        private void Refresh()
        {
            var data = Case;
            var em = EvidenceManager.Instance;
            var inv = InvestigationManager.Instance;
            if (data == null || em == null || inv == null || evidenceList == null) return;

            for (int i = evidenceList.childCount - 1; i >= 0; i--) Destroy(evidenceList.GetChild(i).gameObject);
            foreach (var e in em.Discovered)
            {
                var ev = e;
                string linkedId = inv.GetLinkedSuspect(e.id);
                var suspect = linkedId != null ? System.Array.Find(data.suspects, s => s.id == linkedId) : null;
                string label = suspect != null ? $"{e.title}\n<size=70%><color=#D49A52>→ {suspect.fullName}</color></size>" : e.title;
                var b = UIFactory.CreateButton(evidenceList, label, () => { selected = ev; Refresh(); }, 20);
                b.GetComponent<LayoutElement>().preferredHeight = 70;
                if (selected == e) b.GetComponent<Image>().color = UIStyle.DesaturatedBlue;
            }

            for (int i = 0; i < data.suspects.Length && i < suspectColumns.Count; i++)
            {
                var s = data.suspects[i];
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"<color=#C4B49A>Mobile possible :</color> {s.potentialMotive}");
                sb.AppendLine($"<color=#C4B49A>Alibi :</color> {s.alibi}\n");
                foreach (var id in inv.GetEvidenceLinkedTo(s.id))
                {
                    var e = GameDatabase.Instance?.FindEvidence(id);
                    if (e != null) sb.AppendLine("• " + e.title);
                }
                suspectColumns[i].text = sb.ToString();
            }

            if (selected != null)
            {
                string timeline = string.IsNullOrEmpty(selected.timelineEntry) ? "" : $"\n<color=#D49A52>Événement :</color> {selected.timelineEntry}";
                detailText.text = $"<b>{selected.title}</b>  <size=75%>({selected.location})</size>\n{selected.description}{timeline}";
            }
            else detailText.text = em.Count == 0
                ? "<i>Aucune preuve collectée pour l'instant.</i>"
                : "<i>Sélectionnez une preuve à gauche, puis associez-la au suspect qu'elle concerne.</i>";
        }
    }
}
