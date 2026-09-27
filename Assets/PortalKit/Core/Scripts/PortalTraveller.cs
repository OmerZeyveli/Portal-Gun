using System.Collections.Generic;
using UnityEngine;

namespace PortalKit.Core
{
    /// <summary>
    /// Base class for anything that can pass through a <see cref="Portal"/>. A portal clones
    /// <see cref="graphicsObject"/> while the traveller straddles it, then discards the clone on teleport.
    /// </summary>
    public class PortalTraveller : MonoBehaviour
    {

        /// <summary>Object whose renderers get sliced and cloned while crossing a portal.</summary>
        public GameObject graphicsObject;
        public GameObject graphicsClone { get; set; }
        public Vector3 previousOffsetFromPortal { get; set; }

        public Material[] originalMaterials { get; set; }
        public Material[] cloneMaterials { get; set; }

        /// <summary>Called by the portal to move the traveller to the other side. Override to also remap velocity, camera look, etc.</summary>
        public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
        }

        /// <summary>Called when the traveller first touches a portal.</summary>
        // Called when first touches portal
        public virtual void EnterPortalThreshold()
        {
            if (graphicsClone == null)
            {
                graphicsClone = Instantiate(graphicsObject);
                graphicsClone.transform.parent = graphicsObject.transform.parent;
                graphicsClone.transform.localScale = graphicsObject.transform.localScale;
                originalMaterials = GetMaterials(graphicsObject);
                cloneMaterials = GetMaterials(graphicsClone);
            }
            else
            {
                graphicsClone.SetActive(true);
            }
        }

        /// <summary>Called once the traveller is no longer touching a portal (excluding when teleporting).</summary>
        // Called once no longer touching portal (excluding when teleporting)
        public virtual void ExitPortalThreshold()
        {
            graphicsClone.SetActive(false);
            // Disable slicing
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                originalMaterials[i].SetVector("sliceNormal", Vector3.zero);
            }
        }

        public void SetSliceOffsetDst(float dst, bool clone)
        {
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                if (clone)
                {
                    cloneMaterials[i].SetFloat("sliceOffsetDst", dst);
                }
                else
                {
                    originalMaterials[i].SetFloat("sliceOffsetDst", dst);
                }

            }
        }

        Material[] GetMaterials(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<MeshRenderer>();
            var matList = new List<Material>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    matList.Add(mat);
                }
            }
            return matList.ToArray();
        }
    }
}
