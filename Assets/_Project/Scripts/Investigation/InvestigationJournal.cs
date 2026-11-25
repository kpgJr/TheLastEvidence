using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Journal d'enquête (touche J / bouton Y) : résumé, objectifs, preuves, documents, suspects,
    /// mobiles, témoignages, chronologie, énigmes résolues et indices. Mis à jour automatiquement.
    /// </summary>
    public class InvestigationJournal : MonoBehaviour
    {
        private enum Tab { Case, Evidence, Suspects, Timeline, Puzzles }

        private UIPanel panel;
        private RectTransform content;
        private ScrollRect scroll;
        private Tab currentTab = Tab.Case;
        private readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();

        public UIPanel Panel => panel;

        private void Start()
        {
            Build();
            if (UIManager.Instance != null) UIManager.Instance.Journal = this;
        }

        private void OnEnable() => GameEvents.JournalUpdated += OnUpdated;
        private void OnDisable() => GameEvents.JournalUpdated -= OnUpdated;

        private void OnUpdated() { if (panel != null && panel.IsOpen) Refresh(); }

        private void Build()
        {
            panel = UIPanel.Create("Journal d'enquête", new Vector2(1500, 960));
            if (panel == null) return;
            panel.Opened += Refresh;
            var tabs = UIFactory.CreateRect("Tabs", panel.Body).Anchor(0, 0.91f, 1, 1);
            UIFactory.AddHorizontalLayout(tabs.gameObject, 8);
            AddTab(tabs, Tab.Case, "Affaire");
            AddTab(tabs, Tab.Evidence, "Preuves");
            AddTab(tabs, Tab.Suspects, "Suspects");
            AddTab(tabs, Tab.Timeline, "Chronologie");
            AddTab(tabs, Tab.Puzzles, "Énigmes & indices");
            UIFactory.CreateButton(tabs, "Tableau ►", () => EvidenceBoard.Instance?.Open(), 24);
            var area = UIFactory.CreateRect("ContentArea", panel.Body).Anchor(0, 0, 1, 0.89f);
            content = UIFactory.CreateScrollList(area, out scroll, 10);
            ((RectTransform)scroll.transform).Stretch();
        }

        private void AddTab(Transform parent, Tab tab, string label)
        {
            tabButtons[tab] = UIFactory.CreateButton(parent, label, () => { currentTab = tab; Refresh(); }, 24);
        }

        private void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        }

        private TextMeshProUGUI AddText(string text, float size = 24)
        {
            var t = UIFactory.CreateText(content, text, size);
            return t;
        }

        private void Refresh()
        {
            if (content == null) return;
            foreach (var kv in tabButtons)
                kv.Value.GetComponent<Image>().color = kv.Key == currentTab ? UIStyle.DesaturatedBlue : UIStyle.ButtonNormal;
            Clear();
            switch (currentTab)
            {
                case Tab.Case: BuildCase(); break;
                case Tab.Evidence: BuildEvidence(); break;
                case Tab.Suspects: BuildSuspects(); break;
                case Tab.Timeline: BuildTimeline(); break;
                case Tab.Puzzles: BuildPuzzles(); break;
            }
            scroll.verticalNormalizedPosition = 1f;
        }

        private static CaseData Case => InvestigationManager.Instance != null ? InvestigationManager.Instance.Case : null;

        private void BuildCase()
        {
            var sb = new StringBuilder();
            var data = Case;
            if (data != null) sb.AppendLine($"<color=#D49A52><b>{data.caseTitle}</b></color>\n\n{data.caseSummary}\n");
            var om = ObjectiveManager.Instance;
            if (om != null)
            {
                sb.AppendLine("<color=#D49A52><b>Objectifs</b></color>");
                foreach (var o in om.Objectives)
                {
                    if (om.IsCompleted(o)) sb.AppendLine($"<color=#5E8C61>+</color> <s>{o.text}</s>");
                    else if (om.Current == o) sb.AppendLine($"<color=#D49A52>►</color> <b>{o.text}</b>");
                }
            }
            var gt = GameTimer.Instance;
            if (gt != null) sb.AppendLine($"\n<size=80%>Temps d'enquête : {GameTimer.Format(gt.Elapsed)}</size>");
            AddText(sb.ToString());
        }

        private void BuildEvidence()
        {
            var em = EvidenceManager.Instance;
            if (em == null || em.Count == 0) { AddText("<i>Aucune preuve pour l'instant.</i>"); return; }
            int total = GameDatabase.Instance != null ? GameDatabase.Instance.evidence.Count : em.Count;
            AddText($"Preuves découvertes : <b>{em.Count} / {total}</b>");
            AddSection("Preuves matérielles et photographies", em.Discovered.Where(e => e.category == EvidenceCategory.Physical || e.category == EvidenceCategory.Photo));
            AddSection("Documents découverts", em.Discovered.Where(e => e.category == EvidenceCategory.Document));
            AddSection("Témoignages", em.Discovered.Where(e => e.category == EvidenceCategory.Testimony));
            AddSection("Enregistrements", em.Discovered.Where(e => e.category == EvidenceCategory.Recording));
        }

        private void AddSection(string title, IEnumerable<EvidenceData> items)
        {
            var list = items.ToList();
            if (list.Count == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine($"\n<color=#D49A52><b>{title}</b></color>");
            foreach (var e in list)
                sb.AppendLine($"<b>{e.title}</b> <size=75%><color=#9C9384>({e.location})</color></size>\n{e.description}\n");
            AddText(sb.ToString(), 22);
        }

        private void BuildSuspects()
        {
            var data = Case;
            if (data == null) return;
            var sb = new StringBuilder();
            foreach (var s in data.suspects)
            {
                sb.AppendLine($"<color=#D49A52><b>{s.fullName}</b></color> - {s.role}");
                sb.AppendLine(s.profile);
                sb.AppendLine($"<color=#C4B49A>Mobile potentiel :</color> {s.potentialMotive}");
                sb.AppendLine($"<color=#C4B49A>Alibi déclaré :</color> {s.alibi}\n");
            }
            sb.AppendLine("<color=#D49A52><b>Mobiles envisagés</b></color>");
            foreach (var m in data.motives) sb.AppendLine("• " + m);
            AddText(sb.ToString(), 22);
        }

        private void BuildTimeline()
        {
            var em = EvidenceManager.Instance;
            var entries = em == null ? new List<EvidenceData>() :
                em.Discovered.Where(e => !string.IsNullOrEmpty(e.timelineEntry)).OrderBy(e => e.timelineMinutes).ToList();
            if (entries.Count == 0) { AddText("<i>Aucun événement daté pour l'instant.</i>"); return; }
            var sb = new StringBuilder("<color=#D49A52><b>14 octobre - événements établis par les preuves</b></color>\n\n");
            foreach (var e in entries) sb.AppendLine($"• {e.timelineEntry}  <size=70%><color=#9C9384>[{e.title}]</color></size>");
            AddText(sb.ToString(), 23);
        }

        private void BuildPuzzles()
        {
            var hm = HintManager.Instance;
            AddText("<size=85%><i>Les indices sont facultatifs : le jeu peut être terminé sans eux. Leur utilisation est comptabilisée.</i></size>");
            foreach (var p in PuzzleManager.All.Where(p => p != null).OrderBy(p => p.name))
            {
                var sb = new StringBuilder();
                sb.Append(p.IsSolved ? "<color=#5E8C61>+ Résolue</color> - " : "<color=#D49A52>● En cours</color> - ");
                sb.AppendLine($"<b>{p.DisplayName}</b>");
                int level = hm != null ? hm.GetLevel(p.PuzzleId) : 0;
                for (int i = 0; i < level && i < p.Hints.Length; i++) sb.AppendLine($"<size=85%>Indice {i + 1} : {p.Hints[i]}</size>");
                AddText(sb.ToString(), 23);
                if (!p.IsSolved && hm != null && level < HintManager.MaxLevel && level < p.Hints.Length)
                {
                    var puzzle = p;
                    UIFactory.CreateButton(content, $"Révéler l'indice {level + 1}/{HintManager.MaxLevel}", () =>
                    {
                        hm.RevealNext(puzzle.PuzzleId);
                        Refresh();
                    }, 22);
                }
            }
        }
    }
}
