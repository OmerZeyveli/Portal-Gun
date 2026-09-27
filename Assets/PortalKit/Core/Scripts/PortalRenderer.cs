using UnityEngine;

namespace PortalKit.Core
{
    /// <summary>Put on the main camera; renders all portals before it culls.</summary>
    public class PortalRenderer : MonoBehaviour
    {

        Portal[] portals;

        void Awake()
        {
            portals = FindObjectsOfType<Portal>();
        }

        void OnPreCull()
        {

            for (int i = 0; i < portals.Length; i++)
            {
                portals[i].PrePortalRender();
            }
            for (int i = 0; i < portals.Length; i++)
            {
                portals[i].Render();
            }

            for (int i = 0; i < portals.Length; i++)
            {
                portals[i].PostPortalRender();
            }

        }

    }
}
