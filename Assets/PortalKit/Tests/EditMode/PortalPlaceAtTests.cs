using NUnit.Framework;
using PortalKit.Core;
using UnityEngine;

namespace PortalKit.Tests
{
    public class PortalPlaceAtTests
    {
        [Test]
        public void PlaceAt_MovesPortalAndRaisesOpened()
        {
            var portal = new GameObject("Portal").AddComponent<Portal>();
            Portal opened = null;
            portal.Opened += p => opened = p;

            portal.PlaceAt(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 90f, 0f));

            Assert.That(portal.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(Quaternion.Angle(portal.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
            Assert.That(opened, Is.SameAs(portal));

            Object.DestroyImmediate(portal.gameObject);
        }
    }
}
