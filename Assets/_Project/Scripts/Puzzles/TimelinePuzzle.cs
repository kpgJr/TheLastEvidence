using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastEvidence
{
    [Serializable]
    public class TimelineEvent
    {
        public string id;
        [TextArea(1, 3)] public string text;
    }

    /// <summary>
    /// Énigme 5 : chronologie. Le joueur réordonne les événements par glisser-déposer
    /// (souris ou rayon VR) ou avec les flèches. L'ordre correct est celui de la liste dans l'Inspector.
    /// </summary>
    public class TimelinePuzzle : PuzzleBase
    {
        [SerializeField] private List<TimelineEvent> correctOrder = new List<TimelineEvent>();
        [Tooltip("Ordre initial affiché (indices dans correctOrder).")]
        [SerializeField] private int[] initialOrder;

        private readonly List<TimelineEvent> currentOrder = new List<TimelineEvent>();
        private RectTransform listRoot;
        private TextMeshProUGUI status;

        protected override Vector2 PanelSize => new Vector2(1300, 940);

        public void Configure(List<TimelineEvent> events, int[] startOrder)
        {
            correctOrder = events; initialOrder = startOrder;
        }

        public static int CountCorrect(IList<string> orderIds, IList<TimelineEvent> solution)
        {
            int n = 0;
            for (int i = 0; i < orderIds.Count && i < solution.Count; i++)
                if (orderIds[i] == solution[i].id) n++;
            return n;
        }

        /// <summary>Soumet un ordre d'identifiants d'événements (tests automatisés, débogage).</summary>
        public bool Submit(IList<string> orderedIds)
        {
            if (IsSolved) return true;
            if (orderedIds != null && orderedIds.Count == correctOrder.Count && CountCorrect(orderedIds, correctOrder) == correctOrder.Count)
            {
                Solve();
                return true;
            }
            Fail("La chronologie est incohérente.");
            return false;
        }

        public List<string> CorrectOrderIds => correctOrder.ConvertAll(e => e.id);

        private void EnsureOrder()
        {
            if (currentOrder.Count == correctOrder.Count) return;
            currentOrder.Clear();
            if (initialOrder != null && initialOrder.Length == correctOrder.Count)
                foreach (int i in initialOrder) currentOrder.Add(correctOrder[i]);
            else
            {
                currentOrder.AddRange(correctOrder);
                currentOrder.Reverse();
            }
        }

        protected override void BuildUI(RectTransform body)
        {
            EnsureOrder();
            var info = UIFactory.CreateText(body,
                "Remettez les événements du 14 octobre dans l'ordre (du plus ancien au plus récent).\nGlissez les cartes ou utilisez les flèches.",
                22, TextAlignmentOptions.Center, UIStyle.TextMuted);
            info.rectTransform.Anchor(0, 0.9f, 1, 1);
            listRoot = UIFactory.CreateRect("Cards", body).Anchor(0.02f, 0.17f, 0.98f, 0.89f);
            UIFactory.AddVerticalLayout(listRoot.gameObject, 10, 4, true);
            status = UIFactory.CreateText(body, "", 24, TextAlignmentOptions.Center);
            status.rectTransform.Anchor(0, 0.1f, 1, 0.17f);
            var validate = UIFactory.CreateButton(body, "Valider la chronologie", Validate, 28);
            validate.GetComponent<Image>().color = UIStyle.DesaturatedBlue;
            ((RectTransform)validate.transform).Anchor(0.3f, 0f, 0.7f, 0.09f);
        }

        protected override void RefreshUI()
        {
            EnsureOrder();
            if (IsSolved)
            {
                currentOrder.Clear();
                currentOrder.AddRange(correctOrder);
            }
            if (listRoot == null) return;
            for (int i = listRoot.childCount - 1; i >= 0; i--) Destroy(listRoot.GetChild(i).gameObject);
            for (int i = 0; i < currentOrder.Count; i++) CreateCard(i, currentOrder[i]);
            if (IsSolved) status.text = "<color=#5E8C61>Chronologie reconstituée.</color>";
        }

        private void CreateCard(int index, TimelineEvent ev)
        {
            var card = UIFactory.CreateImage("Card_" + ev.id, listRoot, UIStyle.Anthracite);
            var drag = card.gameObject.AddComponent<TimelineCard>();
            drag.Init(this, index);
            var number = UIFactory.CreateText(card.transform, (index + 1) + ".", 30, TextAlignmentOptions.Center, UIStyle.Amber);
            number.rectTransform.Anchor(0, 0, 0.06f, 1);
            var text = UIFactory.CreateText(card.transform, ev.text, 24, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.Anchor(0.07f, 0, 0.84f, 1);
            if (IsSolved) return;
            var up = UIFactory.CreateButton(card.transform, "▲", () => Move(index, -1), 26);
            ((RectTransform)up.transform).Anchor(0.85f, 0.1f, 0.92f, 0.9f);
            var down = UIFactory.CreateButton(card.transform, "▼", () => Move(index, 1), 26);
            ((RectTransform)down.transform).Anchor(0.925f, 0.1f, 0.995f, 0.9f);
        }

        public void Move(int index, int direction) => Swap(index, index + direction);

        public void Swap(int a, int b)
        {
            if (IsSolved || a == b || a < 0 || b < 0 || a >= currentOrder.Count || b >= currentOrder.Count) return;
            var item = currentOrder[a];
            currentOrder.RemoveAt(a);
            currentOrder.Insert(b, item);
            AudioManager.Instance?.PlaySfx(SfxType.Paper, 0.5f);
            status.text = "";
            RefreshUI();
        }

        private void Validate()
        {
            if (IsSolved) return;
            int correct = CountCorrect(currentOrder.ConvertAll(e => e.id), correctOrder);
            if (correct == correctOrder.Count) Solve();
            else
            {
                status.text = $"{correct} / {correctOrder.Count} événements sont à la bonne place.";
                Fail("La chronologie est incohérente.");
            }
        }
    }
}
