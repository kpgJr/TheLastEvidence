using UnityEngine;

namespace LastEvidence
{
    /// <summary>Point d'accès à une énigme dans le monde (clavier du coffre, ordinateur, boîte à fusibles...).</summary>
    public class PuzzleTerminal : InteractableBase
    {
        [SerializeField] private PuzzleBase puzzle;
        [SerializeField] private string prompt = "Utiliser";
        [SerializeField] private string solvedPrompt;

        public void Configure(PuzzleBase target, string usePrompt, string promptWhenSolved = null)
        {
            puzzle = target; prompt = usePrompt; solvedPrompt = promptWhenSolved;
        }

        protected override string GetAvailablePrompt() =>
            puzzle != null && puzzle.IsSolved && !string.IsNullOrEmpty(solvedPrompt) ? solvedPrompt : prompt;

        protected override void OnInteract(PlayerRig player)
        {
            if (puzzle != null) puzzle.Open();
        }
    }
}
