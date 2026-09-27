using NUnit.Framework;
using PortalKit.Core;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace PortalKit.Tests
{
    public class PortalApertureTests
    {
        [Test]
        public void OvalMesh_HasUnitDepthSoPortalCanThickenIt()
        {
            Mesh mesh = PortalAperture.BuildMesh(PortalAperture.Shape.Oval, new Vector2(0.96f, 1.92f), 1f, 64);

            Bounds bounds = mesh.bounds;
            Assert.That(bounds.size.z, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(bounds.center.z, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(bounds.size.x, Is.EqualTo(0.96f).Within(1e-3f));
            Assert.That(bounds.size.y, Is.EqualTo(1.92f).Within(1e-3f));
            Assert.That(bounds.center.y, Is.EqualTo(1f).Within(1e-4f));

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void RectangleMesh_IsABoxPrism()
        {
            Mesh mesh = PortalAperture.BuildMesh(PortalAperture.Shape.Rectangle, new Vector2(2f, 3f), 1.5f, 64);

            Assert.That(mesh.vertexCount, Is.EqualTo(2 + 4 * 2));
            Assert.That(mesh.bounds.size, Is.EqualTo(new Vector3(2f, 3f, 1f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(mesh);
        }
    }
}
