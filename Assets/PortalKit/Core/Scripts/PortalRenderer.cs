using UnityEngine;

namespace PortalKit.Core
{
    public class PortalRenderer : MonoBehaviour {

        Portal[] portals;

        void Awake () {
            portals = FindObjectsOfType<Portal> ();
        }

        void OnPreCull () {

            for (int i = 0; i < portals.Length; i++) {
                portals[i].PrePortalRender ();
            }
            for (int i = 0; i < portals.Length; i++) {
                portals[i].Render ();
            }

            for (int i = 0; i < portals.Length; i++) {
                portals[i].PostPortalRender ();
            }

        }

    }
}
