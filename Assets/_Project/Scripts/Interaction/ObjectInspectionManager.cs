using TMPro;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Mode examen : l'objet est placé devant les yeux, rotation (souris glissée / joystick droit),
    /// zoom (molette / joystick gauche), affichage du nom, de la description et de l'indice caché.
    /// Les déplacements sont désactivés pendant l'examen.
    /// </summary>
    public class ObjectInspectionManager : MonoBehaviour
    {
        public static ObjectInspectionManager Instance { get; private set; }

        [SerializeField] private float rotateSpeedMouse = 0.35f;
        [SerializeField] private float rotateSpeedStick = 140f;
        [SerializeField] private float minDistance = 0.25f;
        [SerializeField] private float maxDistance = 0.8f;
        [SerializeField] private float clueFacingThreshold = 0.7f;

        private InspectableObject target;
        private PlayerRig rig;
        private Transform originalParent;
        private Vector3 originalLocalPos;
        private Quaternion originalLocalRot;
        private Vector3 originalLocalScale;
        private Collider[] disabledColliders;
        private int beganFrame;
        private float distance;

        private GameObject uiRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI clueText;

        public bool IsInspecting => target != null;

        private void Awake() => Instance = this;

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => BuildUI();

        private void BuildUI()
        {
            if (UIRoot.Instance == null) return;
            var bg = UIFactory.CreateImage("InspectionUI", UIRoot.Instance.HudRoot, new Color(0, 0, 0, 0.55f));
            bg.raycastTarget = false;
            bg.rectTransform.Anchor(0.15f, 0.02f, 0.85f, 0.3f);
            uiRoot = bg.gameObject;
            var layout = UIFactory.AddVerticalLayout(uiRoot, 6, 16);
            layout.childForceExpandHeight = false;
            titleText = UIFactory.CreateText(bg.transform, "", 34, TextAlignmentOptions.Center, UIStyle.Amber);
            bodyText = UIFactory.CreateText(bg.transform, "", 24, TextAlignmentOptions.Center);
            clueText = UIFactory.CreateText(bg.transform, "", 26, TextAlignmentOptions.Center, UIStyle.OldBeige);
            var help = UIFactory.CreateText(bg.transform, HelpText(), 20, TextAlignmentOptions.Center, UIStyle.TextMuted);
            help.fontStyle = FontStyles.Italic;
            uiRoot.SetActive(false);
        }

        private static string HelpText() => PlatformMode.IsVR
            ? "Joystick droit : tourner • Joystick gauche : zoom • B / gâchette : reposer"
            : "Souris (clic maintenu) : tourner • Molette : zoom • E / Échap / R : reposer";

        public void Begin(InspectableObject obj, PlayerRig player)
        {
            if (obj == null || player == null || player.InspectionAnchor == null || IsInspecting) return;
            target = obj;
            rig = player;
            beganFrame = Time.frameCount;

            var t = obj.transform;
            originalParent = t.parent;
            originalLocalPos = t.localPosition;
            originalLocalRot = t.localRotation;
            originalLocalScale = t.localScale;
            disabledColliders = obj.GetComponentsInChildren<Collider>();
            foreach (var c in disabledColliders) c.enabled = false;

            // Mise à l'échelle pour que l'objet tienne dans le champ de vision.
            Bounds bounds = ComputeBounds(obj.gameObject);
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = maxSize > 0.0001f ? obj.InspectSize / maxSize : 1f;

            distance = PlatformMode.IsVR ? 0.4f : 0.55f;
            t.SetParent(player.InspectionAnchor, true);
            t.localScale = originalLocalScale * scale;
            t.localPosition = Vector3.zero;
            // Convention : la face avant d'un objet est sa face -Z (tournée vers le joueur), le verso est +Z.
            t.localRotation = Quaternion.Euler(8, 0, 0);
            player.InspectionAnchor.localPosition = new Vector3(0, PlatformMode.IsVR ? -0.05f : 0, distance);

            GameManager.Instance?.SetState(GameState.Inspecting);
            AudioManager.Instance?.PlaySfx(SfxType.Paper, 0.6f);
            obj.OnInspected();
            if (uiRoot != null)
            {
                uiRoot.SetActive(true);
                titleText.text = obj.DisplayName;
                bodyText.text = obj.Description;
                clueText.text = obj.ClueFound ? obj.HiddenClueText : (obj.HasHiddenClue ? "<i>Tournez l'objet pour l'observer sous tous les angles…</i>" : "");
            }
            GameEvents.RaiseInteractionPrompt(null);
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void Update()
        {
            if (!IsInspecting) return;
            if (target == null) { End(); return; }

            Transform t = target.transform;
            Transform cam = rig.ViewCamera.transform;
            if (PlatformMode.IsVR)
            {
                Vector2 stick = GameInput.Turn.ReadValue<Vector2>();
                t.Rotate(cam.up, -stick.x * rotateSpeedStick * Time.deltaTime, Space.World);
                t.Rotate(cam.right, stick.y * rotateSpeedStick * Time.deltaTime, Space.World);
                distance = Mathf.Clamp(distance - GameInput.Move.ReadValue<Vector2>().y * 0.4f * Time.deltaTime, minDistance, maxDistance);
            }
            else
            {
                if (GameInput.InspectDrag.IsPressed())
                {
                    Vector2 delta = GameInput.Look.ReadValue<Vector2>();
                    t.Rotate(cam.up, -delta.x * rotateSpeedMouse, Space.World);
                    t.Rotate(cam.right, delta.y * rotateSpeedMouse, Space.World);
                }
                float scroll = GameInput.Zoom.ReadValue<float>();
                if (Mathf.Abs(scroll) > 0.01f) distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 0.05f, minDistance, maxDistance);
            }
            rig.InspectionAnchor.localPosition = new Vector3(0, PlatformMode.IsVR ? -0.05f : 0, distance);

            CheckHiddenClue(t, cam);

            if (Time.frameCount > beganFrame + 1 &&
                (GameInput.Cancel.WasPressedThisFrame() || GameInput.Pause.WasPressedThisFrame() ||
                 GameInput.Interact.WasPressedThisFrame() || GameInput.Examine.WasPressedThisFrame()))
                End();
        }

        private void CheckHiddenClue(Transform t, Transform cam)
        {
            if (!target.HasHiddenClue || target.ClueFound) return;
            Vector3 worldNormal = t.TransformDirection(target.HiddenFaceLocalNormal).normalized;
            Vector3 toCamera = (cam.position - t.position).normalized;
            if (Vector3.Dot(worldNormal, toCamera) > clueFacingThreshold)
            {
                target.OnClueRevealed();
                if (clueText != null) clueText.text = target.HiddenClueText;
                AudioManager.Instance?.PlaySfx(SfxType.Evidence, 0.5f);
            }
        }

        public void End()
        {
            if (target != null)
            {
                var t = target.transform;
                t.SetParent(originalParent, false);
                t.localPosition = originalLocalPos;
                t.localRotation = originalLocalRot;
                t.localScale = originalLocalScale;
                if (disabledColliders != null) foreach (var c in disabledColliders) if (c != null) c.enabled = true;
            }
            target = null;
            if (uiRoot != null) uiRoot.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Inspecting)
                GameManager.Instance.SetState(GameState.Playing);
        }
    }
}
