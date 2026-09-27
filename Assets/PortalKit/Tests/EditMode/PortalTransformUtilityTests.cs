using NUnit.Framework;
using PortalKit.Core;
using UnityEngine;

namespace PortalKit.Tests
{
    public class PortalTransformUtilityTests
    {
        const float AngleTolerance = 0.5f;
        const float DistanceTolerance = 1e-4f;
        static readonly Vector2 WideLimits = new Vector2(-89f, 89f);

        Transform portalA;
        Transform portalB;

        [SetUp]
        public void SetUp()
        {
            portalA = new GameObject("Portal A").transform;
            portalB = new GameObject("Portal B").transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(portalA.gameObject);
            Object.DestroyImmediate(portalB.gameObject);
        }

        // Floor portals face world up: Euler(-90, yaw, 0). Wall portals face horizontally: Euler(0, yaw, 0).
        void SetPortals(Vector3 eulerA, Vector3 eulerB)
        {
            portalA.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(eulerA));
            portalB.SetPositionAndRotation(new Vector3(10f, 0f, 0f), Quaternion.Euler(eulerB));
        }

        [Test]
        public void TravelMatrix_MapsPortalCentreToLinkedCentre()
        {
            SetPortals(new Vector3(0f, 30f, 0f), new Vector3(-90f, 120f, 0f));

            Vector3 mapped = PortalTransformUtility.TransformPoint(portalA, portalB, portalA.position);

            Assert.That(Vector3.Distance(mapped, portalB.position), Is.LessThan(DistanceTolerance));
        }

        [Test]
        public void TravelMatrix_RoundTripIsIdentity()
        {
            SetPortals(new Vector3(0f, 30f, 0f), new Vector3(-90f, 120f, 0f));
            Vector3 point = new Vector3(1.2f, 0.4f, -0.3f);

            Vector3 there = PortalTransformUtility.TransformPoint(portalA, portalB, point);
            Vector3 back = PortalTransformUtility.TransformPoint(portalB, portalA, there);

            Assert.That(Vector3.Distance(point, back), Is.LessThan(DistanceTolerance));
        }

        [Test]
        public void WalkingIntoWallPortal_ExitsOutOfLinkedPortal()
        {
            SetPortals(Vector3.zero, new Vector3(0f, 90f, 0f));

            // Portals face out of their wall, so walking into A means moving against A.forward.
            Vector3 exitVelocity = PortalTransformUtility.TransformDirection(portalA, portalB, -portalA.forward * 3f);

            Assert.That(Vector3.Distance(exitVelocity, portalB.forward * 3f), Is.LessThan(DistanceTolerance));
        }

        [Test]
        public void FallingIntoFloorPortal_ExitsMovingUp()
        {
            SetPortals(new Vector3(-90f, 0f, 0f), new Vector3(-90f, 90f, 0f));

            Vector3 exitVelocity = PortalTransformUtility.TransformDirection(portalA, portalB, new Vector3(0f, -5f, 0f));

            Assert.That(Vector3.Distance(exitVelocity, new Vector3(0f, 5f, 0f)), Is.LessThan(DistanceTolerance));
        }

        [TestCase(0f, 0f)]
        [TestCase(35f, 20f)]
        [TestCase(200f, -30f)]
        public void WallToWall_HasNoResidual(float yaw, float pitch)
        {
            SetPortals(Vector3.zero, new Vector3(0f, 90f, 0f));
            Quaternion camera = PortalTransformUtility.UprightRotation(yaw, pitch);

            Quaternion mapped = PortalTransformUtility.TransformRotation(portalA, portalB, camera);
            UprightLook look = PortalTransformUtility.ClosestUprightLook(mapped, WideLimits);

            Assert.That(Quaternion.Angle(look.residual, Quaternion.identity), Is.LessThan(AngleTolerance));
            Assert.That(look.pitch, Is.EqualTo(pitch).Within(AngleTolerance));
        }

        [TestCase(85f, 10f)]
        [TestCase(60f, 60f)]
        [TestCase(30f, 120f)]
        public void FloorToFloor_FlipsPitchWithExpectedResidual(float pitchIn, float expectedResidual)
        {
            foreach (float portalYawB in new[] { 0f, 90f, 200f })
            {
                foreach (float cameraYaw in new[] { 0f, 45f, 90f, 180f })
                {
                    SetPortals(new Vector3(-90f, 30f, 0f), new Vector3(-90f, portalYawB, 0f));
                    Quaternion camera = PortalTransformUtility.UprightRotation(cameraYaw, pitchIn);

                    Quaternion mapped = PortalTransformUtility.TransformRotation(portalA, portalB, camera);
                    UprightLook look = PortalTransformUtility.ClosestUprightLook(mapped, WideLimits);

                    string context = $"portalYawB={portalYawB} cameraYaw={cameraYaw}";
                    Assert.That(look.pitch, Is.EqualTo(-pitchIn).Within(AngleTolerance), context);
                    Assert.That(Quaternion.Angle(look.residual, Quaternion.identity), Is.EqualTo(expectedResidual).Within(AngleTolerance), context);
                }
            }
        }

        [Test]
        public void ClosestUprightLook_ClampsPitchAndResidualRebuildsTarget()
        {
            SetPortals(new Vector3(-90f, 0f, 0f), new Vector3(-90f, 0f, 0f));
            Quaternion camera = PortalTransformUtility.UprightRotation(0f, 85f);
            Quaternion mapped = PortalTransformUtility.TransformRotation(portalA, portalB, camera);

            UprightLook look = PortalTransformUtility.ClosestUprightLook(mapped, new Vector2(-40f, 85f));
            Quaternion rebuilt = PortalTransformUtility.UprightRotation(look.yaw, look.pitch) * look.residual;

            Assert.That(look.pitch, Is.EqualTo(-40f).Within(0.01f));
            Assert.That(Quaternion.Angle(rebuilt, mapped), Is.LessThan(AngleTolerance));
        }
    }
}
