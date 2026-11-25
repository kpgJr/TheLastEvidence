namespace LastEvidence
{
    /// <summary>Résultat transmis de la scène d'enquête à la scène de fin.</summary>
    public static class EndingResult
    {
        public static bool HasResult;
        public static AccusationOutcome Outcome;
        public static string AccusedName;
        public static string MotiveText;
        public static string OutcomeText;
        public static string TruthSummary;
        public static float PlayTime;
        public static int EvidenceFound;
        public static int EvidenceTotal;
        public static int PuzzlesSolved;
        public static int PuzzlesTotal;
        public static int HintsUsed;
        public static int KeyEvidenceLinked;
    }
}
