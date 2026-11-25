using UnityEngine;

namespace LastEvidence
{
    /// <summary>Zone de sortie derrière la porte principale : termine la partie si l'affaire est close.</summary>
    [RequireComponent(typeof(Collider))]
    public class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private string requiredFlag = "case.closed";

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerRig>() == null) return;
            if (ProgressionManager.Instance == null || !ProgressionManager.Instance.HasFlag(requiredFlag)) return;
            AccusationManager.Instance?.FinishGame();
        }
    }
}
