using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace LastEvidence
{
    /// <summary>
    /// Rig joueur VR (Meta Quest) :
    ///  - Joystick gauche : déplacement continu relatif au regard (clic = courir).
    ///  - Joystick droit : rotation par paliers (confort).
    ///  - Le déplacement physique dans la pièce est supporté : la capsule suit la tête et
    ///    la vue est repoussée si le joueur tente de passer la tête à travers un mur.
    ///  - Gâchette droite : interagir / cliquer dans les menus (rayon de la manette droite).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class VRPlayerController : PlayerRig
    {
        [SerializeField] private Camera headCamera;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Transform rightController;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform inspectionAnchor;
        [SerializeField] private XRRayInteractor uiRayInteractor;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float runSpeed = 3.0f;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float gravity = -15f;

        /// <summary>Faux dans les menus : le joueur peut regarder et pointer mais pas se déplacer.</summary>
        public bool LocomotionEnabled { get; set; } = true;

        private CharacterController controller;
        private float verticalVelocity;
        private bool turnReady = true;
        private bool running;

        public override Camera ViewCamera => headCamera;
        public override Transform InteractionOrigin => rightController;
        public override Transform InspectionAnchor => inspectionAnchor;
        public Transform LeftController => leftController;
        public XRRayInteractor UIRayInteractor => uiRayInteractor;
        public override float HorizontalSpeed { get { var v = controller.velocity; v.y = 0; return v.magnitude; } }
        public override bool IsGrounded => controller.isGrounded;

        public void Init(Camera cam, Transform offset, Transform left, Transform right, Transform anchor, XRRayInteractor ray)
        {
            headCamera = cam; cameraOffset = offset; leftController = left; rightController = right;
            inspectionAnchor = anchor; uiRayInteractor = ray;
        }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            UnityEngine.InputSystem.InputSystem.onDeviceChange += OnDeviceChange;
            LogXRDevices("démarrage");
        }

        private void OnDisable() => UnityEngine.InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;

        private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
        {
            if (device is UnityEngine.InputSystem.XR.XRController) LogXRDevices($"{change} {device.name}");
        }

        /// <summary>Diagnostic (visible avec « adb logcat -s Unity ») : manettes détectées et leur main.</summary>
        private static void LogXRDevices(string reason)
        {
            var sb = new System.Text.StringBuilder($"[LastEvidence] Manettes XR ({reason}) :");
            foreach (var d in UnityEngine.InputSystem.InputSystem.devices)
                if (d is UnityEngine.InputSystem.XR.XRController)
                    sb.Append($" | {d.layout} usages=[{string.Join(",", d.usages)}] enabled={d.enabled}");
            Debug.Log(sb.ToString());
        }

        private void Update()
        {
            FeedUIPress();
            FollowHead();

            Vector3 move = Vector3.zero;
            if (GameplayActive && LocomotionEnabled)
            {
                if (GameInput.Run.WasPressedThisFrame()) running = !running;
                Vector2 input = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);
                if (input.sqrMagnitude < 0.01f) running = false;
                Vector3 forward = headCamera.transform.forward; forward.y = 0; forward.Normalize();
                Vector3 right = headCamera.transform.right; right.y = 0; right.Normalize();
                move = (right * input.x + forward * input.y) * (running ? runSpeed : moveSpeed);
                HandleSnapTurn();
            }

            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        /// <summary>Transmet l'état de la gâchette droite au rayon UI de l'XR Interaction Toolkit.</summary>
        private void FeedUIPress()
        {
            if (uiRayInteractor == null) return;
            bool pressed = GameInput.RightTrigger.IsPressed();
            uiRayInteractor.uiPressInput.QueueManualState(pressed, pressed ? 1f : 0f);
        }

        /// <summary>
        /// Garde la capsule sous la tête : on déplace le corps vers la tête (avec collisions),
        /// puis on décale l'origine de la caméra en sens inverse.
        /// </summary>
        private void FollowHead()
        {
            if (headCamera == null || cameraOffset == null) return;
            Vector3 headLocal = transform.InverseTransformPoint(headCamera.transform.position);
            float height = Mathf.Clamp(headLocal.y + 0.1f, 0.8f, 2.1f);
            controller.height = height;
            controller.center = new Vector3(0, height / 2f + controller.skinWidth, 0);

            Vector3 delta = headCamera.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude < 0.0001f) return;
            controller.Move(delta);
            cameraOffset.position -= delta;
        }

        private void HandleSnapTurn()
        {
            float x = GameInput.Turn.ReadValue<Vector2>().x;
            if (Mathf.Abs(x) < 0.3f) { turnReady = true; return; }
            if (!turnReady || Mathf.Abs(x) < 0.75f) return;
            turnReady = false;
            transform.Rotate(0, Mathf.Sign(x) * snapTurnAngle, 0);
        }

        public override void GetPose(out Vector3 position, out float yaw, out float pitch)
        {
            position = transform.position;
            Vector3 f = headCamera.transform.forward; f.y = 0;
            yaw = f.sqrMagnitude > 0.001f ? Quaternion.LookRotation(f).eulerAngles.y : transform.eulerAngles.y;
            pitch = 0;
        }

        public override void SetPose(Vector3 position, float yaw, float pitch)
        {
            controller.enabled = false;
            // On oriente le rig pour que le regard actuel corresponde au lacet demandé.
            float headYawLocal = headCamera != null ? headCamera.transform.localEulerAngles.y : 0f;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw - headYawLocal, 0));
            if (cameraOffset != null && headCamera != null)
            {
                Vector3 delta = headCamera.transform.position - transform.position;
                delta.y = 0;
                cameraOffset.position -= delta;
            }
            controller.enabled = true;
            verticalVelocity = 0;
        }
    }
}
