using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Registre des énigmes de la scène (comptage, recherche par identifiant).</summary>
    public class PuzzleManager : MonoBehaviour
    {
        private static readonly List<PuzzleBase> puzzles = new List<PuzzleBase>();

        public static IReadOnlyList<PuzzleBase> All => puzzles;

        public static void Register(PuzzleBase puzzle)
        {
            if (puzzle != null && !puzzles.Contains(puzzle)) puzzles.Add(puzzle);
        }

        public static void Unregister(PuzzleBase puzzle) => puzzles.Remove(puzzle);

        public static PuzzleBase Find(string id) => puzzles.Find(p => p != null && p.PuzzleId == id);

        public static int SolvedCount
        {
            get
            {
                int n = 0;
                foreach (var p in puzzles) if (p != null && p.IsSolved) n++;
                return n;
            }
        }

        public static int TotalCount => puzzles.Count;

        private void OnDestroy() => puzzles.RemoveAll(p => p == null);
    }
}
