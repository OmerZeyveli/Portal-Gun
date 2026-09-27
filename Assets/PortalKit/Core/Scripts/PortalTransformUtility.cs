using UnityEngine;

namespace PortalKit.Core
{
    /// <summary>
    /// Maps points, directions and rotations from one portal to its linked portal.
    /// Portals face out of their surface, so travelling through one turns 180° around the portal's local up axis.
    /// This class is the single source of that convention; rendering, teleporting and travellers all use it.
    /// </summary>
    public static class PortalTransformUtility
    {
        static readonly Quaternion Flip = Quaternion.Euler(0f, 180f, 0f);
        static readonly Matrix4x4 FlipMatrix = Matrix4x4.Rotate(Flip);

        /// <summary>World-space matrix that maps anything near <paramref name="from"/> to the matching spot at <paramref name="to"/>.</summary>
        public static Matrix4x4 TravelMatrix(Transform from, Transform to)
        {
            return to.localToWorldMatrix * FlipMatrix * from.worldToLocalMatrix;
        }

        /// <summary>World-space rotation applied to anything travelling from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static Quaternion TravelRotation(Transform from, Transform to)
        {
            return to.rotation * Flip * Quaternion.Inverse(from.rotation);
        }

        public static Vector3 TransformPoint(Transform from, Transform to, Vector3 point)
        {
            return TravelMatrix(from, to).MultiplyPoint3x4(point);
        }

        /// <summary>Maps a direction or velocity through the portal (rotation only, portal scale is ignored).</summary>
        public static Vector3 TransformDirection(Transform from, Transform to, Vector3 direction)
        {
            return TravelRotation(from, to) * direction;
        }

        public static Quaternion TransformRotation(Transform from, Transform to, Quaternion rotation)
        {
            return TravelRotation(from, to) * rotation;
        }

        /// <summary>Rotation of an upright first-person camera: yaw around world up, then pitch (positive looks down).</summary>
        public static Quaternion UprightRotation(float yaw, float pitch)
        {
            return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>
        /// Finds the upright camera rotation closest to <paramref name="target"/>.
        /// Travelling between floor portals leaves a mapped camera upside down; snapping it upright directly
        /// would spin the view 180°. Controllers should apply <see cref="UprightLook.residual"/> on top of the
        /// upright rotation and ease it to identity over a few frames.
        /// </summary>
        public static UprightLook ClosestUprightLook(Quaternion target, Vector2 pitchLimits)
        {
            Vector3 forward = target * Vector3.forward;
            Vector3 up = target * Vector3.up;

            float pitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

            // Heading can follow where the view points, or where the top of the screen points.
            // The second wins when looking steeply up or down (e.g. floor to floor).
            Vector3 headingFromForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            Vector3 headingFromUp = Vector3.ProjectOnPlane(up, Vector3.up) * (forward.y > 0f ? -1f : 1f);

            float bestYaw = 0f;
            float bestAngle = float.MaxValue;
            ConsiderHeading(headingFromForward, pitch, target, ref bestYaw, ref bestAngle);
            ConsiderHeading(headingFromUp, pitch, target, ref bestYaw, ref bestAngle);

            return new UprightLook
            {
                yaw = bestYaw,
                pitch = pitch,
                residual = Quaternion.Inverse(UprightRotation(bestYaw, pitch)) * target
            };
        }

        static void ConsiderHeading(Vector3 heading, float pitch, Quaternion target, ref float bestYaw, ref float bestAngle)
        {
            const float minFlatSqrMagnitude = 1e-6f;
            if (heading.sqrMagnitude < minFlatSqrMagnitude)
            {
                return;
            }

            float yaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            float angle = Quaternion.Angle(UprightRotation(yaw, pitch), target);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestYaw = yaw;
            }
        }
    }

    /// <summary>Result of <see cref="PortalTransformUtility.ClosestUprightLook"/>.</summary>
    public struct UprightLook
    {
        public float yaw;
        public float pitch;
        /// <summary>Leftover rotation: target == UprightRotation(yaw, pitch) * residual.</summary>
        public Quaternion residual;
    }
}
