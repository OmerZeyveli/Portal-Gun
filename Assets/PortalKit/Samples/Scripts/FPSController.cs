using PortalKit.Core;
using UnityEngine;

namespace PortalKit.Samples
{
    /// <summary>Sample controller; shows how to use PortalTransformUtility.</summary>
    public class FPSController : PortalTraveller
    {

        public float walkSpeed = 3;
        public float runSpeed = 6;
        public float smoothMoveTime = 0.1f;
        public float jumpForce = 8;
        public float gravity = 18;

        [Header("Portal Momentum")]
        public float airMomentumDamping = 0.15f;
        public float groundedMomentumDamping = 8f;
        public float minMomentumSpeed = 0.05f;

        public bool lockCursor;
        public float mouseSensitivity = 10;
        public Vector2 pitchMinMax = new Vector2(-40, 85);
        public float rotationSmoothTime = 0.1f;
        public float cameraRealignSharpness = 8f;

        CharacterController controller;
        Camera cam;
        public float yaw;
        public float pitch;
        float smoothYaw;
        float smoothPitch;

        float yawSmoothV;
        float pitchSmoothV;
        float verticalVelocity;
        Vector3 controlledPlanarVelocity;
        Vector3 momentumVelocity;
        Vector3 velocity;
        Vector3 smoothV;
        Vector3 rotationSmoothVelocity;
        Vector3 currentRotation;
        Quaternion camOffset = Quaternion.identity;

        bool jumping;
        bool grounded;
        float lastGroundedTime;
        bool disabled;

        void Start()
        {
            cam = Camera.main;
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            controller = GetComponent<CharacterController>();

            yaw = transform.eulerAngles.y;
            pitch = cam.transform.localEulerAngles.x;
            smoothYaw = yaw;
            smoothPitch = pitch;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Break();
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                disabled = !disabled;
            }

            if (disabled)
            {
                return;
            }

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            Vector3 inputDir = new Vector3(input.x, 0, input.y).normalized;
            Vector3 worldInputDir = transform.TransformDirection(inputDir);

            float currentSpeed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;
            // Keep input movement separate so smoothing cannot erase portal fling speed.
            Vector3 targetVelocity = Vector3.ProjectOnPlane(worldInputDir, Vector3.up).normalized * currentSpeed;
            controlledPlanarVelocity = Vector3.SmoothDamp(controlledPlanarVelocity, targetVelocity, ref smoothV, smoothMoveTime);
            smoothV = Vector3.ProjectOnPlane(smoothV, Vector3.up);

            verticalVelocity -= gravity * Time.deltaTime;
            ApplyMomentumDamping();

            velocity = GetTotalVelocity();

            Vector3 positionBeforeMove = transform.position;
            var flags = controller.Move(velocity * Time.deltaTime);
            grounded = (flags & CollisionFlags.Below) != 0;
            if (grounded)
            {
                jumping = false;
                lastGroundedTime = Time.time;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = 0f;
                }
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
            velocity = GetTotalVelocity();

            // Catch fast moves that skip the portal trigger.
            Portal.TryTeleportTravellerAcrossAnyPortal(this, positionBeforeMove, transform.position, controller.radius);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                float timeSinceLastTouchedGround = Time.time - lastGroundedTime;
                if (grounded || (!jumping && timeSinceLastTouchedGround < 0.15f))
                {
                    jumping = true;
                    verticalVelocity = jumpForce;
                    velocity = GetTotalVelocity();
                }
            }

            float mX = Input.GetAxisRaw("Mouse X");
            float mY = Input.GetAxisRaw("Mouse Y");

            // Ignore the large first mouse delta from cursor lock.
            float mMag = Mathf.Sqrt(mX * mX + mY * mY);
            if (mMag > 5)
            {
                mX = 0;
                mY = 0;
            }

            yaw += mX * mouseSensitivity;
            pitch -= mY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
            smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, rotationSmoothTime);
            smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, rotationSmoothTime);

            transform.eulerAngles = Vector3.up * smoothYaw;
            // Ease out leftover roll/pitch from a portal exit so the view settles upright without snapping.
            camOffset = Quaternion.Slerp(camOffset, Quaternion.identity, 1f - Mathf.Exp(-cameraRealignSharpness * Time.deltaTime));
            cam.transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f) * camOffset;

        }

        public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
        {
            transform.position = pos;

            // Map the camera through the portal, snap to the closest upright look and ease out the rest in Update.
            Quaternion mappedCamRotation = PortalTransformUtility.TransformRotation(fromPortal, toPortal, cam.transform.rotation);
            UprightLook look = PortalTransformUtility.ClosestUprightLook(mappedCamRotation, pitchMinMax);
            smoothYaw += Mathf.DeltaAngle(smoothYaw, look.yaw);
            yaw = smoothYaw;
            pitch = look.pitch;
            smoothPitch = look.pitch;
            yawSmoothV = 0f;
            pitchSmoothV = 0f;
            camOffset = look.residual;
            transform.eulerAngles = Vector3.up * smoothYaw;
            cam.transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f) * camOffset;

            Vector3 transformedVelocity = PortalTransformUtility.TransformDirection(fromPortal, toPortal, GetTotalVelocity());
            Vector3 transformedControlledVelocity = PortalTransformUtility.TransformDirection(fromPortal, toPortal, controlledPlanarVelocity);
            SetPortalVelocity(transformedVelocity, transformedControlledVelocity);

            Physics.SyncTransforms();
        }

        Vector3 GetTotalVelocity()
        {
            return controlledPlanarVelocity + momentumVelocity + Vector3.up * verticalVelocity;
        }

        void ApplyMomentumDamping()
        {
            if (momentumVelocity.sqrMagnitude <= 0f)
            {
                return;
            }

            // Air damping is low for fling; grounded damping removes sliding after landing.
            float damping = grounded ? groundedMomentumDamping : airMomentumDamping;
            if (damping > 0f)
            {
                float blend = 1f - Mathf.Exp(-damping * Time.deltaTime);
                momentumVelocity = Vector3.Lerp(momentumVelocity, Vector3.zero, blend);
            }

            if (momentumVelocity.sqrMagnitude < minMomentumSpeed * minMomentumSpeed)
            {
                momentumVelocity = Vector3.zero;
            }
        }

        void SetPortalVelocity(Vector3 transformedVelocity, Vector3 transformedControlledVelocity)
        {
            verticalVelocity = Vector3.Dot(transformedVelocity, Vector3.up);

            // Only the transformed input part stays controlled; extra speed becomes portal momentum.
            controlledPlanarVelocity = Vector3.ProjectOnPlane(transformedControlledVelocity, Vector3.up);
            momentumVelocity = Vector3.ProjectOnPlane(transformedVelocity, Vector3.up) - controlledPlanarVelocity;

            if (momentumVelocity.sqrMagnitude < minMomentumSpeed * minMomentumSpeed)
            {
                momentumVelocity = Vector3.zero;
            }

            smoothV = Vector3.zero;
            grounded = false;
            velocity = GetTotalVelocity();
        }

    }
}
