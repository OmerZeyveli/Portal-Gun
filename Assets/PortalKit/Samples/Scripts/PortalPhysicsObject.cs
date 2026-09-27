using System.Collections.Generic;
using System.Collections;
using PortalKit.Core;
using UnityEngine;

namespace PortalKit.Samples
{
    [RequireComponent (typeof (Rigidbody))]
    public class PortalPhysicsObject : PortalTraveller {

        public float force = 10;
        new Rigidbody rigidbody;
        public Color[] colors;
        static int i;

        void Awake () {
            rigidbody = GetComponent<Rigidbody> ();
            graphicsObject.GetComponent<MeshRenderer> ().material.color = colors[i];
            i++;
            if (i > colors.Length - 1) {
                i = 0;
            }
        }

        public override void Teleport (Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
            base.Teleport (fromPortal, toPortal, pos, rot);
            rigidbody.velocity = PortalTransformUtility.TransformDirection (fromPortal, toPortal, rigidbody.velocity);
            rigidbody.angularVelocity = PortalTransformUtility.TransformDirection (fromPortal, toPortal, rigidbody.angularVelocity);
        }
    }
}
