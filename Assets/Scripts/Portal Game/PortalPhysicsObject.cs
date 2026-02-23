using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PortalPhysicsObject : PortalTraveller {

    public float force = 10;
    new Rigidbody rigidbody;
    public Color[] colors;
    static int i;

    static readonly Quaternion PortalFlip = Quaternion.Euler(0f, 180f, 0f);

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

        Vector3 vLocal = fromPortal.InverseTransformVector(rigidbody.velocity);
        vLocal = PortalFlip * vLocal;
        rigidbody.velocity = toPortal.TransformVector(vLocal);

        Vector3 wLocal = fromPortal.InverseTransformVector(rigidbody.angularVelocity);
        wLocal = PortalFlip * wLocal;
        rigidbody.angularVelocity = toPortal.TransformVector(wLocal);
    }
}