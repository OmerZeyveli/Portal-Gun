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

        Vector3 positionBeforeMove = transform.position;
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

        // Catch fast moves that skip the portal trigger.
        Portal.TryTeleportTravellerAcrossAnyPortal (this, positionBeforeMove, transform.position, controller.radius);

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
        // Ease out leftover roll/pitch from a portal exit so the view settles upright without snapping.
        camOffset = Quaternion.Slerp (camOffset, Quaternion.identity, 1f - Mathf.Exp (-cameraRealignSharpness * Time.deltaTime));
        cam.transform.localRotation = Quaternion.Euler (smoothPitch, 0f, 0f) * camOffset;

    }

    public override void Teleport (Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;

        // Same mapping the portal uses to render its view. Between floor portals this leaves the camera
        // upside down, so snap to the closest upright look and ease out the rest instead of flipping the view.
        Quaternion portalDelta = toPortal.rotation * PortalFlip * Quaternion.Inverse (fromPortal.rotation);
        Quaternion mappedCamRotation = portalDelta * cam.transform.rotation;
        SetClosestUprightLook (mappedCamRotation);
        camOffset = Quaternion.Inverse (UprightCamRotation (smoothYaw, smoothPitch)) * mappedCamRotation;
        transform.eulerAngles = Vector3.up * smoothYaw;
        cam.transform.localRotation = Quaternion.Euler (smoothPitch, 0f, 0f) * camOffset;

        Vector3 transformedVelocity = TransformPortalVelocity (fromPortal, toPortal, GetTotalVelocity ());
        Vector3 transformedControlledVelocity = TransformPortalVelocity (fromPortal, toPortal, controlledPlanarVelocity);
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

    Vector3 TransformPortalVelocity (Transform fromPortal, Transform toPortal, Vector3 sourceVelocity) {
        // Convert velocity to from-portal local space, apply 180° Y flip, then convert to to-portal world space.
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

    void SetClosestUprightLook (Quaternion target) {
        const float minFlatSqrMagnitude = 1e-6f;

        Vector3 forward = target * Vector3.forward;
        Vector3 up = target * Vector3.up;

        float targetPitch = -Mathf.Asin (Mathf.Clamp (forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp (targetPitch, pitchMinMax.x, pitchMinMax.y);

        // Heading can follow where the view points, or where the top of the screen points.
        // The second wins when looking steeply up/down (e.g. floor-to-floor), avoiding a 180° spin.
        Vector3 headingFromForward = Vector3.ProjectOnPlane (forward, Vector3.up);
        Vector3 headingFromUp = Vector3.ProjectOnPlane (up, Vector3.up) * ((forward.y > 0f) ? -1f : 1f);

        float bestYaw = smoothYaw;
        float bestAngle = float.MaxValue;
        foreach (Vector3 heading in new[] { headingFromForward, headingFromUp }) {
            if (heading.sqrMagnitude < minFlatSqrMagnitude) {
                continue;
            }
            float candidateYaw = Mathf.Atan2 (heading.x, heading.z) * Mathf.Rad2Deg;
            float angle = Quaternion.Angle (UprightCamRotation (candidateYaw, targetPitch), target);
            if (angle < bestAngle) {
                bestAngle = angle;
                bestYaw = candidateYaw;
            }
        }

        smoothYaw += Mathf.DeltaAngle (smoothYaw, bestYaw);
        yaw = smoothYaw;
        pitch = targetPitch;
        smoothPitch = targetPitch;
        yawSmoothV = 0f;
        pitchSmoothV = 0f;
    }

    static Quaternion UprightCamRotation (float yaw, float pitch) {
        return Quaternion.Euler (0f, yaw, 0f) * Quaternion.Euler (pitch, 0f, 0f);
    }

}
