using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Objet examinable en détail (rotation, zoom). Peut cacher un indice sur une face précise
    /// (ex. date au verso d'une photo) : l'indice est révélé lorsque cette face est tournée vers le joueur.
    /// </summary>
    public class InspectableObject : InteractableBase, IExaminable
    {
        [SerializeField] private string displayName = "Objet";
        [TextArea(2, 6)] [SerializeField] private string description;
        [Tooltip("Preuve enregistrée dès l'examen.")]
        [SerializeField] private EvidenceData evidenceOnInspect;
        [Header("Indice caché")]
        [TextArea(2, 6)] [SerializeField] private string hiddenClueText;
        [Tooltip("Normale locale de la face qui porte l'indice (ex. (0,0,1) = verso).")]
        [SerializeField] private Vector3 hiddenFaceLocalNormal = Vector3.forward;
        [SerializeField] private EvidenceData evidenceOnClue;
        [Tooltip("Taille maximale de l'objet pendant l'examen (mètres).")]
        [SerializeField] private float inspectSize = 0.22f;

        private bool clueFound;

        public string DisplayName => displayName;
        public string Description => description;
        public string HiddenClueText => hiddenClueText;
        public bool HasHiddenClue => !string.IsNullOrEmpty(hiddenClueText);
        public Vector3 HiddenFaceLocalNormal => hiddenFaceLocalNormal;
        public float InspectSize => inspectSize;
        public bool ClueFound => clueFound;

        public void Configure(string name, string desc, EvidenceData onInspect, string clue, Vector3 clueNormal,
            EvidenceData onClue, float size = 0.22f)
        {
            displayName = name; description = desc; evidenceOnInspect = onInspect; hiddenClueText = clue;
            hiddenFaceLocalNormal = clueNormal; evidenceOnClue = onClue; inspectSize = size;
        }

        protected override string GetAvailablePrompt() => $"Examiner : {displayName}";

        protected override void OnInteract(PlayerRig player) => Examine(player);

        public void Examine(PlayerRig player)
        {
            if (!IsAvailable) return;
            ObjectInspectionManager.Instance?.Begin(this, player);
        }

        public void OnInspected()
        {
            if (evidenceOnInspect != null) EvidenceManager.Instance?.Discover(evidenceOnInspect);
        }

        public void OnClueRevealed()
        {
            if (clueFound) return;
            clueFound = true;
            if (evidenceOnClue != null) EvidenceManager.Instance?.Discover(evidenceOnClue);
            else GameEvents.RaiseNotification($"Indice découvert sur : {displayName}");
        }
    }
}
