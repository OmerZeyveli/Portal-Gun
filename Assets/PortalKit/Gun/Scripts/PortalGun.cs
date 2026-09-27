using PortalKit.Core;
using System;
using UnityEngine;

namespace PortalKit.Gun
{
    /// <summary>
    /// Aims from the centre of the screen and places two-tile portals on <see cref="PortalTile"/> surfaces,
    /// using <see cref="PortalGrid"/> to find the paired tile and <see cref="PortalOccupancy"/> to reserve them.
    /// </summary>
    public class PortalGun : MonoBehaviour
    {
        /// <summary>Camera used to aim from the center of the screen. Defaults to Camera.main if empty.</summary>
        [Header("References")]
        [Tooltip("Camera used to aim from the center of the screen. Defaults to Camera.main if empty.")]
        public Camera cam;

        /// <summary>Layers that are allowed to receive portals. Placement still also requires a PortalTile.</summary>
        [Tooltip("Layers that are allowed to receive portals. Placement still also requires a PortalTile.")]
        public LayerMask portalableMask;

        /// <summary>Blue portal instance that gets moved when left-click places successfully.</summary>
        [Tooltip("Blue portal instance that gets moved when left-click places successfully.")]
        public Portal bluePortal;

        /// <summary>Orange portal instance that gets moved when right-click places successfully.</summary>
        [Tooltip("Orange portal instance that gets moved when right-click places successfully.")]
        public Portal orangePortal;

        /// <summary>Maximum distance for both placement and shot feedback raycasts.</summary>
        [Header("Placement")]
        [Tooltip("Maximum distance for both placement and shot feedback raycasts.")]
        public float maxDistance = 200f;

        /// <summary>Layers the shot beam can hit for feedback. Use this to show lasers on normal walls too.</summary>
        [Tooltip("Layers the shot beam can hit for feedback. Use this to show lasers on normal walls too.")]
        public LayerMask shotMask = Physics.DefaultRaycastLayers;

        /// <summary>How far the portal should sit in front of the hit surface (meters).</summary>
        [Tooltip("How far the portal should sit in front of the hit surface (meters).")]
        public float forwardOffset = 0.02f;

        /// <summary>If the portal prefab pivot is at its base, shift the portal down along portalUp by this many tile sizes.</summary>
        [Tooltip("If the portal prefab pivot is at its base, shift the portal down along portalUp by this many tile sizes.")]
        public float pivotDownTiles = 1f;

        /// <summary>Color reported with blue shots. Effects use it for beams, gun accents and the crosshair.</summary>
        [Header("Colors")]
        [Tooltip("Color reported with blue shots. Effects use it for beams, gun accents and the crosshair.")]
        public Color bluePortalColor = new Color(0.15f, 0.55f, 1f, 1f);

        /// <summary>Color reported with orange shots. Effects use it for beams, gun accents and the crosshair.</summary>
        [Tooltip("Color reported with orange shots. Effects use it for beams, gun accents and the crosshair.")]
        public Color orangePortalColor = new Color(1f, 0.35f, 0.05f, 1f);

        /// <summary>When true, the script reads left/right mouse buttons itself. Disable to drive firing from your own input system via <see cref="FireBlue"/> / <see cref="FireOrange"/>.</summary>
        [Header("Input")]
        [Tooltip("When true, the script reads left/right mouse buttons itself. Disable to drive firing from your own input system via FireBlue() / FireOrange().")]
        public bool inputEnabled = true;

        PortalGrid grid;

        struct PortalPlacementResult
        {
            // hitPoint is always populated from the broad shot raycast, even when placement fails.
            public bool success;
            public Vector3 hitPoint;
            public Vector3 hitNormal;
            public Vector3 portalPosition;
            public Quaternion portalRotation;
            public PortalTile bottomTile;
            public PortalTile topTile;
        }

        /// <summary>Raised after every shot, whether or not a portal was placed.</summary>
        public event Action<PortalShot> Fired;

        void Awake()
        {
            if (!cam) cam = Camera.main;
            grid = PortalGrid.Instance;

            EnsureOccupancy(bluePortal);
            EnsureOccupancy(orangePortal);
        }

        void Start()
        {
            // Script execution order can make Instance null in Awake; retry on Start.
            EnsureGrid();
        }

        void EnsureOccupancy(Portal p)
        {
            if (!p) return;

            // Ensure each portal has an occupancy component, used to disable the two tiles it occupies.
            if (!p.GetComponent<PortalOccupancy>())
                p.gameObject.AddComponent<PortalOccupancy>();
        }

        bool EnsureGrid()
        {
            if (grid != null) return true;
            grid = PortalGrid.Instance;
            if (grid != null) return true;
            grid = FindObjectOfType<PortalGrid>(true);
            return grid != null;
        }

        void Update()
        {
            if (!inputEnabled) return;
            if (Input.GetMouseButtonDown(0)) Fire(PortalSlot.Blue);
            if (Input.GetMouseButtonDown(1)) Fire(PortalSlot.Orange);
        }

        /// <summary>Fires the blue portal. Call this from your own input handler when inputEnabled is false.</summary>
        public bool FireBlue() => Fire(PortalSlot.Blue);

        /// <summary>Fires the orange portal. Call this from your own input handler when inputEnabled is false.</summary>
        public bool FireOrange() => Fire(PortalSlot.Orange);

        /// <summary>Shoots from the centre of the screen, places the slot's portal if possible and raises <see cref="Fired"/>.</summary>
        /// <returns>True when a portal was placed.</returns>
        public bool Fire(PortalSlot slot)
        {
            bool placed = TryPlace(GetPortal(slot), out PortalPlacementResult result);

            Fired?.Invoke(new PortalShot
            {
                slot = slot,
                color = GetColor(slot),
                origin = cam ? cam.transform.position : transform.position,
                hitPoint = result.hitPoint,
                hitNormal = result.hitNormal,
                placed = placed
            });

            return placed;
        }

        /// <summary>True when firing <paramref name="slot"/> right now would place a portal.</summary>
        public bool CanPlace(PortalSlot slot) => TryEvaluatePlacement(GetPortal(slot), false, out _);

        /// <summary>Color associated with the given slot (<see cref="bluePortalColor"/> or <see cref="orangePortalColor"/>).</summary>
        public Color GetColor(PortalSlot slot) => slot == PortalSlot.Blue ? bluePortalColor : orangePortalColor;

        Portal GetPortal(PortalSlot slot) => slot == PortalSlot.Blue ? bluePortal : orangePortal;

        PortalPlacementResult CreateDefaultResult()
        {
            Ray ray = cam ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : new Ray(transform.position, transform.forward);
            return new PortalPlacementResult
            {
                success = false,
                hitPoint = ray.origin + ray.direction * maxDistance,
                hitNormal = -ray.direction,
                portalPosition = Vector3.zero,
                portalRotation = Quaternion.identity
            };
        }

        bool TryPlace(Portal portal, out PortalPlacementResult result)
        {
            if (!TryEvaluatePlacement(portal, true, out result))
                return false;

            var occ = portal.GetComponent<PortalOccupancy>();
            if (!occ.Place(result.bottomTile, result.topTile))
            {
                Debug.LogError($"[PortalGun] TryPlace failed: tiles are occupied or cannot be used. bottom='{result.bottomTile.name}' top='{result.topTile.name}'");
                result.success = false;
                return false;
            }

            portal.PlaceAt(result.portalPosition, result.portalRotation);
            return true;
        }

        bool TryEvaluatePlacement(Portal portal, bool logErrors, out PortalPlacementResult result)
        {
            result = CreateDefaultResult();

            if (!portal)
            {
                LogPlacementError(logErrors, "[PortalGun] TryPlace failed: portal reference is null.");
                return false;
            }
            if (!cam)
            {
                LogPlacementError(logErrors, "[PortalGun] TryPlace failed: camera reference is null.");
                return false;
            }
            if (!EnsureGrid())
            {
                LogPlacementError(logErrors, "[PortalGun] TryPlace failed: PortalGrid not found.");
                return false;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            // First raycast against shotMask so non-portalable walls still get a responsive beam.
            // Actual placement is gated below by portalableMask, PortalTile, and occupancy checks.
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, shotMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            result.hitPoint = hit.point;
            result.hitNormal = hit.normal.normalized;

            if (!LayerInMask(hit.collider.gameObject.layer, portalableMask))
            {
                return false;
            }

            var tile = hit.collider.GetComponentInParent<PortalTile>();
            if (!tile)
            {
                LogPlacementError(logErrors, $"[PortalGun] TryPlace failed: hit '{hit.collider.name}' but no PortalTile found in parent hierarchy.");
                return false;
            }
            if (!tile.portalable)
            {
                LogPlacementError(logErrors, $"[PortalGun] TryPlace failed: tile '{tile.name}' is marked non-portalable.");
                return false;
            }

            Vector3 normal = hit.normal.normalized;

            bool isFloorish = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.75f;
            bool wallForceVertical = !isFloorish;

            Vector3 portalUp = GetSnappedPortalUp(tile, normal, cam.transform.forward, wallForceVertical).normalized;

            float rel = Vector3.Dot(hit.point - tile.transform.position, portalUp);
            Vector3 preferredDir = (rel >= 0f) ? portalUp : -portalUp;

            if (!grid.TryGetNeighbor(tile, preferredDir, out PortalTile neighbor))
            {
                if (!grid.TryGetNeighbor(tile, -preferredDir, out neighbor))
                {
                    LogPlacementError(logErrors, $"[PortalGun] TryPlace failed: no adjacent tile found for 2-tile portal. tile='{tile.name}' coord={tile.Coord} preferredDir={preferredDir}");
                    return false;
                }
            }

            bool neighborIsPositive = Vector3.Dot(neighbor.transform.position - tile.transform.position, portalUp) > 0f;
            PortalTile bottom = neighborIsPositive ? tile : neighbor;
            PortalTile top = neighborIsPositive ? neighbor : tile;

            var occ = portal.GetComponent<PortalOccupancy>();
            if (!occ)
            {
                LogPlacementError(logErrors, $"[PortalGun] TryPlace failed: portal '{portal.name}' is missing PortalOccupancy component.");
                return false;
            }
            if (!bottom.CanOccupy(occ) || !top.CanOccupy(occ))
            {
                LogPlacementError(logErrors, $"[PortalGun] TryPlace failed: tiles are occupied or cannot be used. bottom='{bottom.name}' top='{top.name}'");
                return false;
            }

            Vector3 center = (bottom.transform.position + top.transform.position) * 0.5f;

            float downMeters = (grid != null ? grid.cellSize : 1f) * pivotDownTiles;
            center -= portalUp * downMeters;

            Quaternion rot = Quaternion.LookRotation(normal, portalUp);

            // Ensure the portal's forward points out of the surface.
            // If the portal ends up facing into the surface, flip it 180° around its up axis.
            if (Vector3.Dot(rot * Vector3.forward, normal) < 0f)
                rot = Quaternion.AngleAxis(180f, normal) * rot;

            result.success = true;
            result.portalPosition = center + normal * forwardOffset;
            result.portalRotation = rot;
            result.bottomTile = bottom;
            result.topTile = top;
            return true;
        }

        static void LogPlacementError(bool shouldLog, string message)
        {
            if (shouldLog)
                Debug.LogError(message);
        }

        static bool LayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// Returns a portalUp vector snapped to the closest 90° axis on the hit surface.
        /// On walls, we force the portal to remain vertical.
        /// On floors/ceilings, we choose between the tile's up/right axes based on the camera view direction.
        /// </summary>
        Vector3 GetSnappedPortalUp(PortalTile tile, Vector3 normal, Vector3 viewForward, bool wallForceVertical)
        {
            // Project tile axes onto the surface plane.
            Vector3 upAxis = Vector3.ProjectOnPlane(tile.Up, normal);
            Vector3 rightAxis = Vector3.ProjectOnPlane(tile.Right, normal);

            // Fallback if axes are invalid.
            if (upAxis.sqrMagnitude < 1e-6f || rightAxis.sqrMagnitude < 1e-6f)
            {
                Vector3 fallback = Vector3.ProjectOnPlane(Vector3.forward, normal);
                if (fallback.sqrMagnitude < 1e-6f) fallback = Vector3.ProjectOnPlane(Vector3.right, normal);
                upAxis = fallback.normalized;
                rightAxis = Vector3.Cross(normal, upAxis).normalized;
            }
            else
            {
                upAxis.Normalize();
                rightAxis.Normalize();
            }

            if (wallForceVertical)
            {
                // Ensure it points upward in world space.
                if (Vector3.Dot(upAxis, Vector3.up) < 0f) upAxis = -upAxis;
                return upAxis;
            }

            // Floor/ceiling: pick the axis closest to the view direction, snapped to 90°.
            Vector3 v = Vector3.ProjectOnPlane(viewForward, normal);
            if (v.sqrMagnitude < 1e-6f) v = Vector3.ProjectOnPlane(Vector3.forward, normal);
            v.Normalize();

            float du = Vector3.Dot(v, upAxis);
            float dr = Vector3.Dot(v, rightAxis);

            if (Mathf.Abs(du) >= Mathf.Abs(dr))
                return (du >= 0f) ? upAxis : -upAxis;

            return (dr >= 0f) ? rightAxis : -rightAxis;
        }
    }
}
