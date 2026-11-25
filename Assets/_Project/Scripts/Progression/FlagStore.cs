using System.Collections.Generic;
using System.Linq;

namespace LastEvidence
{
    /// <summary>Ensemble de drapeaux de progression (logique pure, testable en Edit Mode).</summary>
    public class FlagStore
    {
        private readonly HashSet<string> flags = new HashSet<string>();

        public int Count => flags.Count;

        /// <summary>Ajoute le drapeau. Retourne vrai s'il n'existait pas encore.</summary>
        public bool Set(string flag) => !string.IsNullOrEmpty(flag) && flags.Add(flag);

        public bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public bool HasAll(IEnumerable<string> required)
        {
            if (required == null) return true;
            foreach (var f in required)
                if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) return false;
            return true;
        }

        public void Clear() => flags.Clear();

        public List<string> ToList() => flags.OrderBy(f => f).ToList();
    }
}
