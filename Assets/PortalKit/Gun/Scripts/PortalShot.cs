using UnityEngine;

namespace PortalKit.Gun
{
    /// <summary>Which of the gun's two portals a shot is for.</summary>
    public enum PortalSlot
    {
        Blue,
        Orange
    }

    /// <summary>Everything effects need to know about one shot. Raised by <see cref="PortalGun.Fired"/>.</summary>
    public struct PortalShot
    {
        public PortalSlot slot;
        public Color color;
        /// <summary>Aim origin (the gun's camera). Effects with a muzzle use their own muzzle instead.</summary>
        public Vector3 origin;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        /// <summary>True when a portal was actually placed.</summary>
        public bool placed;
    }
}
