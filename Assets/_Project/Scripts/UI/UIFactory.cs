using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>
    /// Fabrique d'éléments d'interface (uGUI + TextMeshPro) construits en code.
    /// Les interfaces fonctionnent ainsi à l'identique en mode écran et en mode VR (canvas en espace monde).
    /// </summary>
    public static class UIFactory
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Étire le RectTransform sur son parent avec des marges (gauche, bas, droite, haut).</summary>
        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Place le RectTransform selon des ancres normalisées (0-1) du parent.</summary>
        public static RectTransform Anchor(this RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, float size = 28,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, Color? color = null)
        {
            var rt = CreateRect("Text", parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color ?? UIStyle.TextMain;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.richText = true;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string label, UnityAction onClick, float fontSize = 28)
        {
            var img = CreateImage("Button_" + label, parent, UIStyle.ButtonNormal);
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.5f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = new Color(1.2f, 1.2f, 1.3f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.colorMultiplier = 1.5f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(() => AudioManager.Instance?.PlaySfx(SfxType.Click));

            var text = CreateText(img.transform, label, fontSize, TextAlignmentOptions.Center);
            text.rectTransform.Stretch(8, 4, 8, 4);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = fontSize * 2.1f;
            le.preferredHeight = fontSize * 2.1f;
            return button;
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var t = button.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing = 10, int padding = 10,
            bool controlHeight = false, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = controlHeight;
            v.childAlignment = align;
            return v;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing = 10, int padding = 0)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return h;
        }

        /// <summary>Crée une zone défilante verticale et retourne son contenu (avec VerticalLayoutGroup).</summary>
        public static RectTransform CreateScrollList(Transform parent, out ScrollRect scroll, float spacing = 8)
        {
            var viewportImg = CreateImage("Scroll", parent, new Color(0, 0, 0, 0.25f));
            scroll = viewportImg.gameObject.AddComponent<ScrollRect>();
            viewportImg.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("Content", viewportImg.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            AddVerticalLayout(content.gameObject, spacing, 12);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = (RectTransform)viewportImg.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>Texte long dans une zone défilante.</summary>
        public static TextMeshProUGUI CreateScrollText(Transform parent, out ScrollRect scroll, float fontSize = 26)
        {
            var content = CreateScrollList(parent, out scroll);
            var text = CreateText(content, string.Empty, fontSize);
            return text;
        }

        public static Slider CreateSlider(Transform parent, string label, float min, float max, float value,
            Action<float> onChanged)
        {
            var row = CreateRect("Slider_" + label, parent);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 56;
            var lbl = CreateText(row, label, 26, TextAlignmentOptions.MidlineLeft);
            lbl.rectTransform.Anchor(0, 0, 0.42f, 1);

            var bg = CreateImage("Background", row, UIStyle.Anthracite);
            bg.rectTransform.Anchor(0.45f, 0.35f, 0.88f, 0.65f);
            var slider = bg.gameObject.AddComponent<Slider>();
            var fillArea = CreateRect("Fill Area", bg.transform).Stretch();
            var fill = CreateImage("Fill", fillArea, UIStyle.Amber);
            fill.rectTransform.Stretch();
            var handleArea = CreateRect("Handle Area", bg.transform).Stretch();
            var handle = CreateImage("Handle", handleArea, UIStyle.OldBeige);
            handle.rectTransform.sizeDelta = new Vector2(28, 0);
            handle.rectTransform.anchorMin = new Vector2(0, -0.6f);
            handle.rectTransform.anchorMax = new Vector2(0, 1.6f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            var valueText = CreateText(row, value.ToString("0.00"), 24, TextAlignmentOptions.MidlineRight);
            valueText.rectTransform.Anchor(0.89f, 0, 1, 1);
            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = v.ToString("0.00");
                onChanged?.Invoke(v);
            });
            return slider;
        }

        /// <summary>Ligne « libellé : [valeur] » avec un bouton qui fait défiler les choix.</summary>
        public static Button CreateCycleRow(Transform parent, string label, Func<string> getValue, Action onCycle)
        {
            var row = CreateRect("Row_" + label, parent);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 56;
            var lbl = CreateText(row, label, 26, TextAlignmentOptions.MidlineLeft);
            lbl.rectTransform.Anchor(0, 0, 0.42f, 1);
            Button button = null;
            button = CreateButton(row, getValue(), () =>
            {
                onCycle();
                SetButtonLabel(button, getValue());
            }, 24);
            ((RectTransform)button.transform).Anchor(0.45f, 0.05f, 1f, 0.95f);
            return button;
        }

        public static TextMeshProUGUI CreateHeader(Transform parent, string title)
        {
            var t = CreateText(parent, title, 40, TextAlignmentOptions.Center, UIStyle.Amber);
            t.fontStyle = FontStyles.Bold;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            return t;
        }
    }
}
