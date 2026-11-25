using UnityEngine;

namespace LastEvidence
{
    /// <summary>Base des objets interactifs : condition de disponibilité et message de verrouillage.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] protected ProgressCondition availableWhen = new ProgressCondition();
        [Tooltip("Indication affichée lorsque la condition n'est pas remplie.")]
        [SerializeField] protected string unavailablePrompt = "Inutilisable pour le moment";
        [Tooltip("Message affiché si le joueur interagit alors que la condition n'est pas remplie.")]
        [SerializeField] protected string unavailableMessage;

        public ProgressCondition AvailableWhen => availableWhen;

        public void SetAvailability(string[] requiredFlags, string prompt, string message)
        {
            availableWhen = new ProgressCondition { requiredFlags = requiredFlags };
            unavailablePrompt = prompt;
            unavailableMessage = message;
        }

        protected bool IsAvailable => availableWhen == null || availableWhen.IsMet();

        public string GetPrompt() => IsAvailable ? GetAvailablePrompt() : unavailablePrompt;

        public virtual bool CanInteract() => true;

        public void Interact(PlayerRig player)
        {
            if (!IsAvailable)
            {
                AudioManager.Instance?.PlaySfx(SfxType.Locked);
                if (!string.IsNullOrEmpty(unavailableMessage)) GameEvents.RaiseNotification(unavailableMessage);
                return;
            }
            OnInteract(player);
        }

        protected abstract string GetAvailablePrompt();
        protected abstract void OnInteract(PlayerRig player);
    }
}
