using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Carte de chronologie déplaçable (glisser-déposer compatible souris et rayon VR).</summary>
    public class TimelineCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private TimelinePuzzle puzzle;
        private int index;
        private Image image;

        public int Index => index;

        public void Init(TimelinePuzzle owner, int cardIndex)
        {
            puzzle = owner;
            index = cardIndex;
            image = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (image != null) image.color = UIStyle.DesaturatedBlue;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (image != null) image.color = UIStyle.Anthracite;
            var over = eventData.pointerCurrentRaycast.gameObject;
            var target = over != null ? over.GetComponentInParent<TimelineCard>() : null;
            if (target != null && target != this) puzzle.Swap(index, target.Index);
        }
    }
}
