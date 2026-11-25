using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LastEvidence
{
    /// <summary>
    /// Crée les canvas de la scène selon le mode :
    ///  - Bureau : canvas en superposition écran (1920×1080).
    ///  - VR : canvas en espace monde placé devant le joueur (menus) et attaché à la tête (HUD).
    /// Crée aussi l'EventSystem avec le bon module d'entrée.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        public static readonly Vector2 ModalSize = new Vector2(1600, 1000);
        private const float VRModalScale = 0.0011f;
        private const float VRModalDistance = 1.5f;
        private const float VRHudScale = 0.00058f;
        private const float VRHudMaxDistance = 0.9f;

        private float recenterUntil;
        private float lookAwayTime;

        public Canvas ModalCanvas { get; private set; }
        public Canvas HudCanvas { get; private set; }
        public RectTransform ModalRoot { get; private set; }
        public RectTransform HudRoot { get; private set; }

        private void Awake()
        {
            Instance = this;
            CreateEventSystem();
            ModalCanvas = CreateCanvas("ModalCanvas", 20, out var modalRoot);
            ModalRoot = modalRoot;
            HudCanvas = CreateCanvas("HudCanvas", 10, out var hudRoot);
            HudRoot = hudRoot;
            if (PlatformMode.IsVR)
            {
                HudCanvas.transform.localScale = Vector3.one * VRHudScale;
                ((RectTransform)HudCanvas.transform).sizeDelta = new Vector2(1600, 1000);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            AttachHudToCamera();
            RecenterModal();
            // Au lancement sur Quest, le suivi de la tête n'est pas encore actif : on recentre pendant quelques secondes.
            recenterUntil = Time.unscaledTime + 2.5f;
        }

        private void LateUpdate()
        {
            if (!PlatformMode.IsVR) return;
            var cam = PlayerRig.Current != null ? PlayerRig.Current.ViewCamera : Camera.main;
            if (cam == null) return;
            UpdateHudDistance(cam);

            bool panelOpen = UIManager.Instance == null || UIManager.Instance.AnyPanelOpen;
            if (Time.unscaledTime < recenterUntil) { RecenterModal(); return; }
            if (!panelOpen) return;
            // Si le joueur se détourne longtemps du menu, celui-ci revient devant lui.
            Vector3 toCanvas = (ModalCanvas.transform.position - cam.transform.position).normalized;
            float angle = Vector3.Angle(cam.transform.forward, toCanvas);
            lookAwayTime = angle > 70f ? lookAwayTime + Time.unscaledDeltaTime : 0f;
            if (lookAwayTime > 1.2f)
            {
                RecenterModal();
                lookAwayTime = 0f;
            }
        }

        /// <summary>Rapproche le HUD si un mur est plus proche que lui (sinon il serait masqué).</summary>
        private void UpdateHudDistance(Camera cam)
        {
            if (HudCanvas.transform.parent != cam.transform) return;
            float distance = VRHudMaxDistance;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, VRHudMaxDistance + 0.05f,
                    ~0, QueryTriggerInteraction.Ignore))
                distance = Mathf.Clamp(hit.distance - 0.05f, 0.25f, VRHudMaxDistance);
            float k = distance / VRHudMaxDistance;
            HudCanvas.transform.localPosition = new Vector3(0, -0.04f * k, distance);
            HudCanvas.transform.localScale = Vector3.one * VRHudScale * k;
        }

        private void CreateEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            if (PlatformMode.IsVR)
            {
                var module = go.AddComponent<XRUIInputModule>();
                module.enableXRInput = true;
                module.enableMouseInput = false;
                module.enableTouchInput = false;
                module.enableGamepadInput = false;
            }
            else
            {
                var module = go.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
        }

        private Canvas CreateCanvas(string name, int sortingOrder, out RectTransform root)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.sortingOrder = sortingOrder;

            if (PlatformMode.IsVR)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = ModalSize;
                rt.localScale = Vector3.one * VRModalScale;
                go.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
            }

            root = UIFactory.CreateRect("Root", go.transform).Stretch();
            return canvas;
        }

        private void AttachHudToCamera()
        {
            if (!PlatformMode.IsVR) return;
            var cam = PlayerRig.Current != null ? PlayerRig.Current.ViewCamera : Camera.main;
            if (cam == null) return;
            HudCanvas.worldCamera = cam;
            ModalCanvas.worldCamera = cam;
            var hud = HudCanvas.transform;
            hud.SetParent(cam.transform, false);
            hud.localPosition = new Vector3(0, -0.04f, VRHudMaxDistance);
            hud.localRotation = Quaternion.identity;
            // Le HUD ne doit pas intercepter le rayon de la manette.
            var raycaster = HudCanvas.GetComponent<BaseRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
        }

        /// <summary>En VR, replace le canvas des menus face au joueur (orientation horizontale uniquement).</summary>
        public void RecenterModal()
        {
            if (!PlatformMode.IsVR) return;
            var cam = PlayerRig.Current != null ? PlayerRig.Current.ViewCamera : Camera.main;
            if (cam == null) return;
            ModalCanvas.worldCamera = cam;
            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            // Si un mur est plus proche que la distance normale, le menu est rapproché (et réduit d'autant)
            // pour ne jamais apparaître derrière le mur.
            float distance = VRModalDistance;
            if (Physics.Raycast(cam.transform.position, forward, out RaycastHit hit, VRModalDistance, ~0, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0.45f, hit.distance - 0.1f);
            float k = distance / VRModalDistance;
            var t = ModalCanvas.transform;
            t.SetParent(null, true);
            t.position = cam.transform.position + forward * distance + Vector3.down * (0.1f * k);
            t.rotation = Quaternion.LookRotation(forward, Vector3.up);
            t.localScale = Vector3.one * VRModalScale * k;
        }
    }
}
