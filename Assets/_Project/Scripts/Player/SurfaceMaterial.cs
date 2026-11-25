using UnityEngine;

namespace LastEvidence
{
    public enum SurfaceType { Wood, Tile, Carpet, Concrete }

    /// <summary>Indique le type de sol pour les bruits de pas.</summary>
    public class SurfaceMaterial : MonoBehaviour
    {
        public SurfaceType surface = SurfaceType.Wood;
    }
}
