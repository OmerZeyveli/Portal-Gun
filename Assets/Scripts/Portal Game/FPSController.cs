using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : PortalTraveller {

    public float walkSpeed = 3;
    public float runSpeed = 6;
    public float smoothMoveTime = 0.1f;
    public float jumpForce = 8;
    public float gravity = 18;

    [Header ("Portal Momentum")]
    public float airMomentumDamping = 0.15f;
    public float groundedMomentumDamping = 8f;
    public float minMomentumSpeed = 0.05f;

    public bool lockCursor;
    public float mouseSensitivity = 10;
    public Vector2 pitchMinMax = new Vector2 (-40, 85);
    public float rotationSmoothTime = 0.1f;

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

    bool jumping;
    bool grounded;
    float lastGroundedTime;
    bool disabled;

    static readonly Quaternion PortalFlip = Quaternion.Euler(0f, 180f, 0f);

    void Start () {
        cam = Camera.main;
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        controller = GetComponent<CharacterController> ();

        yaw = transform.eulerAngles.y;
        pitch = cam.transform.localEulerAngles.x;
        smoothYaw = yaw;
        smoothPitch = pitch;
    }

    void Update () {
        if (Input.GetKeyDown (KeyCode.P)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Break ();
        }
        if (Input.GetKeyDown (KeyCode.O)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            disabled = !disabled;
        }

        if (disabled) {
            return;
        }

        Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));

        Vector3 inputDir = new Vector3 (input.x, 0, input.y).normalized;
        Vector3 worldInputDir = transform.TransformDirection (inputDir);

        float currentSpeed = (Input.GetKey (KeyCode.LeftShift)) ? runSpeed : walkSpeed;
        // Keep input movement separate so smoothing cannot erase portal fling speed.
        Vector3 targetVelocity = Vector3.ProjectOnPlane (worldInputDir, Vector3.up).normalized * currentSpeed;
        controlledPlanarVelocity = Vector3.SmoothDamp (controlledPlanarVelocity, targetVelocity, ref smoothV, smoothMoveTime);
        smoothV = Vector3.ProjectOnPlane (smoothV, Vector3.up);

        verticalVelocity -= gravity * Time.deltaTime;
        ApplyMomentumDamping ();

        velocity = GetTotalVelocity ();

        var flags = controller.Move (velocity * Time.deltaTime);
        grounded = (flags & CollisionFlags.Below) != 0;
        if (grounded) {
            jumping = false;
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) {
                verticalVelocity = 0f;
            }
        }
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) {
            verticalVelocity = 0f;
        }
        velocity = GetTotalVelocity ();

        if (Input.GetKeyDown (KeyCode.Space)) {
            float timeSinceLastTouchedGround = Time.time - lastGroundedTime;
            if (grounded || (!jumping && timeSinceLastTouchedGround < 0.15f)) {
                jumping = true;
                verticalVelocity = jumpForce;
                velocity = GetTotalVelocity ();
            }
        }

        float mX = Input.GetAxisRaw ("Mouse X");
        float mY = Input.GetAxisRaw ("Mouse Y");

        // Ignore the large first mouse delta from cursor lock.
        float mMag = Mathf.Sqrt (mX * mX + mY * mY);
        if (mMag > 5) {
            mX = 0;
            mY = 0;
        }

        yaw += mX * mouseSensitivity;
        pitch -= mY * mouseSensitivity;
        pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
        smoothPitch = Mathf.SmoothDampAngle (smoothPitch, pitch, ref pitchSmoothV, rotationSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle (smoothYaw, yaw, ref yawSmoothV, rotationSmoothTime);

        transform.eulerAngles = Vector3.up * smoothYaw;
        cam.transform.localEulerAngles = Vector3.right * smoothPitch;

    }

    public override void Teleport (Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;

        Quaternion portalDelta = toPortal.rotation * PortalFlip * Quaternion.Inverse (fromPortal.rotation);
        // Same-facing floor/ceiling portals need upright yaw, not mirrored 3D rotation.
        bool sameFacingHorizontalPortals = IsHorizontalPortal (fromPortal) && IsHorizontalPortal (toPortal) && Vector3.Dot (fromPortal.forward, toPortal.forward) > 0.75f;
        Quaternion horizontalDelta = Quaternion.identity;
        bool useHorizontalDelta = sameFacingHorizontalPortals && TryGetHorizontalPortalDelta (fromPortal, toPortal, out horizontalDelta);

        if (useHorizontalDelta) {
            SetUprightLookRotation (horizontalDelta * transform.forward, Vector3.up, smoothPitch);
        } else {
            Vector3 transformedForward = portalDelta * cam.transform.forward;
            Vector3 transformedUp = portalDelta * cam.transform.up;
            SetUprightLookRotation (transformedForward, transformedUp);
        }

        Vector3 transformedVelocity = TransformPortalVelocity (fromPortal, toPortal, GetTotalVelocity (), useHorizontalDelta, horizontalDelta);
        Vector3 transformedControlledVelocity = TransformPortalVelocity (fromPortal, toPortal, controlledPlanarVelocity, useHorizontalDelta, horizontalDelta);
        SetPortalVelocity (transformedVelocity, transformedControlledVelocity);

        Physics.SyncTransforms ();
    }

    Vector3 GetTotalVelocity () {
        return controlledPlanarVelocity + momentumVelocity + Vector3.up * verticalVelocity;
    }

    void ApplyMomentumDamping () {
        if (momentumVelocity.sqrMagnitude <= 0f) {
            return;
        }

        // Air damping is low for fling; grounded damping removes sliding after landing.
        float damping = grounded ? groundedMomentumDamping : airMomentumDamping;
        if (damping > 0f) {
            float blend = 1f - Mathf.Exp (-damping * Time.deltaTime);
            momentumVelocity = Vector3.Lerp (momentumVelocity, Vector3.zero, blend);
        }

        if (momentumVelocity.sqrMagnitude < minMomentumSpeed * minMomentumSpeed) {
            momentumVelocity = Vector3.zero;
        }
    }

    Vector3 TransformPortalVelocity (Transform fromPortal, Transform toPortal, Vector3 sourceVelocity, bool useHorizontalDelta, Quaternion horizontalDelta) {
        if (useHorizontalDelta) {
            Vector3 planarVelocity = Vector3.ProjectOnPlane (sourceVelocity, Vector3.up);
            float sourceVerticalVelocity = Vector3.Dot (sourceVelocity, Vector3.up);
            return (horizontalDelta * planarVelocity) - Vector3.up * sourceVerticalVelocity;
        }

        Vector3 vLocal = fromPortal.InverseTransformVector (sourceVelocity);
        vLocal = PortalFlip * vLocal;
        return toPortal.TransformVector (vLocal);
    }

    void SetPortalVelocity (Vector3 transformedVelocity, Vector3 transformedControlledVelocity) {
        verticalVelocity = Vector3.Dot (transformedVelocity, Vector3.up);

        // Only the transformed input part stays controlled; extra speed becomes portal momentum.
        controlledPlanarVelocity = Vector3.ProjectOnPlane (transformedControlledVelocity, Vector3.up);
        momentumVelocity = Vector3.ProjectOnPlane (transformedVelocity, Vector3.up) - controlledPlanarVelocity;

        if (momentumVelocity.sqrMagnitude < minMomentumSpeed * minMomentumSpeed) {
            momentumVelocity = Vector3.zero;
        }

        smoothV = Vector3.zero;
        grounded = false;
        velocity = GetTotalVelocity ();
    }

    void SetUprightLookRotation (Vector3 forward, Vector3 up) {
        float targetPitch = -Mathf.Asin (Mathf.Clamp (Vector3.Dot (forward.normalized, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
        SetUprightLookRotation (forward, up, targetPitch);
    }

    void SetUprightLookRotation (Vector3 forward, Vector3 up, float targetPitch) {
        const float minLookSqrMagnitude = 1e-6f;
        const float minFlatLookSqrMagnitude = 1e-4f;

        if (forward.sqrMagnitude < minLookSqrMagnitude) {
            return;
        }

        forward.Normalize ();

        pitch = Mathf.Clamp (targetPitch, pitchMinMax.x, pitchMinMax.y);
        smoothPitch = pitch;

        Vector3 flatForward = Vector3.ProjectOnPlane (forward, Vector3.up);
        if (flatForward.sqrMagnitude < minFlatLookSqrMagnitude) {
            // When looking nearly vertical, use camera up to keep yaw stable.
            Vector3 flatUp = Vector3.ProjectOnPlane (up, Vector3.up);
            if (flatUp.sqrMagnitude > minFlatLookSqrMagnitude) {
                flatForward = (Vector3.Dot (forward, Vector3.up) > 0f) ? -flatUp : flatUp;
            }
        }

        if (flatForward.sqrMagnitude > minLookSqrMagnitude) {
            flatForward.Normalize ();
            float targetYaw = Mathf.Atan2 (flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.DeltaAngle (smoothYaw, targetYaw);
            smoothYaw += yawDelta;
        }

        yaw = smoothYaw;
        yawSmoothV = 0f;
        pitchSmoothV = 0f;

        transform.eulerAngles = Vector3.up * smoothYaw;
        cam.transform.localEulerAngles = Vector3.right * smoothPitch;
    }

    bool IsHorizontalPortal (Transform portal) {
        return Mathf.Abs (Vector3.Dot (portal.forward, Vector3.up)) > 0.75f;
    }

    bool TryGetHorizontalPortalDelta (Transform fromPortal, Transform toPortal, out Quaternion delta) {
        // Portal roll on the floor maps to player yaw around world up.
        Vector3 fromUp = Vector3.ProjectOnPlane (fromPortal.up, Vector3.up);
        Vector3 toUp = Vector3.ProjectOnPlane (toPortal.up, Vector3.up);

        if (fromUp.sqrMagnitude < 1e-6f || toUp.sqrMagnitude < 1e-6f) {
            delta = Quaternion.identity;
            return false;
        }

        float yawDelta = Vector3.SignedAngle (fromUp.normalized, toUp.normalized, Vector3.up);
        delta = Quaternion.AngleAxis (yawDelta, Vector3.up);
        return true;
    }

}
