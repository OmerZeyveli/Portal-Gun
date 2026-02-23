using UnityEngine;

public class PortalGun : MonoBehaviour
{
    public Camera cam;
    public LayerMask portalableMask;

    public Portal bluePortal;
    public Portal orangePortal;

    [Header("Placement")]
    public float maxDistance = 200f;

    [Tooltip("How far the portal should sit in front of the hit surface (meters).")]
    public float forwardOffset = 0.02f;

    [Tooltip("If the portal prefab pivot is at its base, shift the portal down along portalUp by this many tile sizes.")]
    public float pivotDownTiles = 1f;

    PortalGrid grid;

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
        if (grid == null)
        {
            grid = PortalGrid.Instance;
            if (grid == null)
                grid = FindObjectOfType<PortalGrid>(true);
        }
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
        if (Input.GetMouseButtonDown(0)) TryPlace(bluePortal);
        if (Input.GetMouseButtonDown(1)) TryPlace(orangePortal);
    }

    void TryPlace(Portal portal)
    {
        if (!portal)
        {
            Debug.LogError("[PortalGun] TryPlace failed: portal reference is null.");
            return;
        }
        if (!cam)
        {
            Debug.LogError("[PortalGun] TryPlace failed: camera reference is null.");
            return;
        }
        if (!EnsureGrid())
        {
            Debug.LogError("[PortalGun] TryPlace failed: PortalGrid not found.");
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, portalableMask))
        {
            // Debug.LogError("[PortalGun] TryPlace failed: raycast did not hit anything in portalableMask. Check layers/mask/colliders.");
            return;
        }

        var tile = hit.collider.GetComponentInParent<PortalTile>();
        if (!tile)
        {
            Debug.LogError($"[PortalGun] TryPlace failed: hit '{hit.collider.name}' but no PortalTile found in parent hierarchy.");
            return;
        }
        if (!tile.portalable)
        {
            Debug.LogError($"[PortalGun] TryPlace failed: tile '{tile.name}' is marked non-portalable.");
            return;
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
                Debug.LogError($"[PortalGun] TryPlace failed: no adjacent tile found for 2-tile portal. tile='{tile.name}' coord={tile.Coord} preferredDir={preferredDir}");
                return;
            }
        }

        bool neighborIsPositive = Vector3.Dot(neighbor.transform.position - tile.transform.position, portalUp) > 0f;
        PortalTile bottom = neighborIsPositive ? tile : neighbor;
        PortalTile top = neighborIsPositive ? neighbor : tile;

        var occ = portal.GetComponent<PortalOccupancy>();
        if (!occ)
        {
            Debug.LogError($"[PortalGun] TryPlace failed: portal '{portal.name}' is missing PortalOccupancy component.");
            return;
        }
        if (!occ.Place(bottom, top))
        {
            Debug.LogError($"[PortalGun] TryPlace failed: tiles are occupied or cannot be used. bottom='{bottom.name}' top='{top.name}'");
            return;
        }

        Vector3 center = (bottom.transform.position + top.transform.position) * 0.5f;

        float downMeters = (grid != null ? grid.cellSize : 1f) * pivotDownTiles;
        center -= portalUp * downMeters;

        Quaternion rot = Quaternion.LookRotation(normal, portalUp);

        // Ensure the portal's forward points out of the surface.
        // If the portal ends up facing into the surface, flip it 180° around its up axis.
        if (Vector3.Dot(rot * Vector3.forward, normal) < 0f)
            rot = Quaternion.AngleAxis(180f, normal) * rot;

        portal.transform.SetPositionAndRotation(center + normal * forwardOffset, rot);
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
