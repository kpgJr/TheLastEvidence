using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastEvidence
{
    /// <summary>
    /// Ajuste le rendu selon la plateforme : en VR (Meta Quest), MSAA 4x, pas de HDR ni d'ombres
    /// pour tenir 72 images/seconde ; en bureau, réglages complets (post-traitement, ombres).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class RenderQualityAdapter : MonoBehaviour
    {
        private void Awake()
        {
            GameSettings.ApplyAll();
            if (!PlatformMode.IsVR) return;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.msaaSampleCount = 4;
                urp.supportsHDR = false;
                urp.renderScale = 1f;
                urp.shadowDistance = 0f;
            }
            Application.targetFrameRate = -1;
        }
    }
}
