using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace LastEvidence
{
    /// <summary>
    /// Détermine si le jeu tourne en mode VR (casque Meta Quest via OpenXR) ou en mode
    /// bureau (clavier/souris). Toute la logique de jeu est partagée : seuls le rig du joueur,
    /// les entrées et le placement des interfaces changent.
    /// </summary>
    public static class PlatformMode
    {
        private static bool? cachedIsVR;

        public static bool IsVR
        {
            get
            {
                if (!cachedIsVR.HasValue) cachedIsVR = Detect();
                return cachedIsVR.Value;
            }
        }

        /// <summary>Force un mode (tests, débogage). Passer null pour revenir à la détection automatique.</summary>
        public static void Override(bool? isVR) => cachedIsVR = isVR;

        private static bool Detect()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Le build Android est destiné au Meta Quest : toujours en VR.
            return true;
#else
            var settings = XRGeneralSettings.Instance;
            if (settings != null && settings.Manager != null && settings.Manager.activeLoader != null)
                return true;
            return XRSettings.isDeviceActive;
#endif
        }
    }
}
