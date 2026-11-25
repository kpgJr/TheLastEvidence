using System;

namespace LastEvidence
{
    /// <summary>Condition configurable dans l'Inspector : tous les drapeaux listés doivent être actifs.</summary>
    [Serializable]
    public class ProgressCondition
    {
        public string[] requiredFlags = Array.Empty<string>();

        public bool IsEmpty => requiredFlags == null || requiredFlags.Length == 0;

        public bool IsMet()
        {
            if (IsEmpty) return true;
            var pm = ProgressionManager.Instance;
            return pm != null && pm.Flags.HasAll(requiredFlags);
        }
    }
}
