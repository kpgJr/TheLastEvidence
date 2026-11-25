using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Rig joueur clavier/souris : marcher, courir, s'accroupir, gravité, collisions (CharacterController).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : PlayerRig
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerCamera cameraLook;
        [SerializeField] private Transform inspectionAnchor;
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float runSpeed = 4.2f;
        [SerializeField] private float crouchSpeed = 1.2f;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;
        [SerializeField] private float gravity = -15f;

        private CharacterController controller;
        private float verticalVelocity;
        private float currentHeight;
        private bool crouching;

        public override Camera ViewCamera => playerCamera;
        public override Transform InteractionOrigin => playerCamera.transform;
        public override Transform InspectionAnchor => inspectionAnchor;
        public override float HorizontalSpeed { get { var v = controller.velocity; v.y = 0; return v.magnitude; } }
        public override bool IsGrounded => controller.isGrounded;
        public bool IsCrouching => crouching;

        public void Init(Camera cam, PlayerCamera look, Transform anchor)
        {
            playerCamera = cam; cameraLook = look; inspectionAnchor = anchor;
        }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            currentHeight = standingHeight;
        }

        private void Update()
        {
            UpdateCrouch();
            Vector3 move = Vector3.zero;
            if (GameplayActive)
            {
                Vector2 input = GameInput.Move.ReadValue<Vector2>();
                input = Vector2.ClampMagnitude(input, 1f);
                float speed = crouching ? crouchSpeed : GameInput.Run.IsPressed() ? runSpeed : walkSpeed;
                move = (transform.right * input.x + transform.forward * input.y) * speed;
            }

            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        private void UpdateCrouch()
        {
            bool wantsCrouch = GameplayActive && GameInput.Crouch.IsPressed();
            // On ne se relève pas si un plafond bloque.
            if (!wantsCrouch && crouching)
            {
                Vector3 top = transform.position + Vector3.up * (crouchHeight - controller.radius);
                if (Physics.SphereCast(top, controller.radius * 0.9f, Vector3.up, out _,
                        standingHeight - crouchHeight, ~0, QueryTriggerInteraction.Ignore))
                    wantsCrouch = true;
            }
            crouching = wantsCrouch;

            float target = crouching ? crouchHeight : standingHeight;
            currentHeight = Mathf.MoveTowards(currentHeight, target, crouchTransitionSpeed * Time.deltaTime);
            controller.height = currentHeight;
            controller.center = new Vector3(0, currentHeight / 2f, 0);
            if (playerCamera != null)
                playerCamera.transform.localPosition = new Vector3(0, currentHeight - 0.12f, 0);
        }

        public override void GetPose(out Vector3 position, out float yaw, out float pitch)
        {
            position = transform.position;
            yaw = transform.eulerAngles.y;
            pitch = cameraLook != null ? cameraLook.Pitch : 0f;
        }

        public override void SetPose(Vector3 position, float yaw, float pitch)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            controller.enabled = true;
            verticalVelocity = 0;
            cameraLook?.SetPitch(pitch);
        }
    }
}
