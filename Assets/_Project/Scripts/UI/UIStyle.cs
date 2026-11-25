using UnityEngine;

namespace LastEvidence
{
    /// <summary>Palette de la direction artistique (voir notes.md §15).</summary>
    public static class UIStyle
    {
        public static readonly Color DeepBlack = Hex("#101114");
        public static readonly Color Anthracite = Hex("#25282D");
        public static readonly Color DesaturatedBlue = Hex("#344A5E");
        public static readonly Color OldBeige = Hex("#C4B49A");
        public static readonly Color Amber = Hex("#D49A52");
        public static readonly Color DarkRed = Hex("#792D35");

        public static readonly Color PanelBackground = new Color(0.063f, 0.067f, 0.078f, 0.96f);
        public static readonly Color ButtonNormal = Hex("#25282D");
        public static readonly Color ButtonHighlight = Hex("#344A5E");
        public static readonly Color TextMain = Hex("#E6DDCB");
        public static readonly Color TextMuted = Hex("#9C9384");
        public static readonly Color Success = Hex("#5E8C61");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
