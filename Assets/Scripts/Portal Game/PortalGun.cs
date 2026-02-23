using UnityEngine;

public class PortalGun : MonoBehaviour
{
    public Camera cam;
    public LayerMask portalableMask;

    public Portal bluePortal;
    public Portal orangePortal;

    [Header("Placement")]
    public float maxDistance = 200f;
    public float surfaceOffset = 0.003f; // 1-5mm
    public int portalHeightTiles = 2;    // şimdilik 2
    public bool debugDraw = false;

    PortalGrid grid;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        grid = PortalGrid.Instance;

        EnsureOccupancy(bluePortal);
        EnsureOccupancy(orangePortal);
    }

    void EnsureOccupancy(Portal p)
    {
        if (!p) return;
        if (!p.GetComponent<PortalOccupancy>())
            p.gameObject.AddComponent<PortalOccupancy>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryPlace(bluePortal);
        if (Input.GetMouseButtonDown(1)) TryPlace(orangePortal);
    }

    void TryPlace(Portal portal)
    {
        if (!portal || !cam || grid == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, portalableMask))
            return;

        var tile = hit.collider.GetComponentInParent<PortalTile>();
        if (!tile || !tile.portalable)
            return;

        Vector3 normal = tile.Normal;

        // Duvar mı zemin/tavan mı?
        bool isFloorish = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.75f;

        // Portalın "up" ekseni (1x2 yönü)
        Vector3 portalUp = isFloorish ? ChooseAxisFromView(tile, normal) : tile.Up;
        portalUp.Normalize();

        // 2. tile yönü: portalUp boyunca komşu
        // Hit noktasına göre hangi tarafa iki tile olacağını seç
        float rel = Vector3.Dot(hit.point - tile.transform.position, portalUp);
        Vector3 preferredDir = (rel >= 0f) ? portalUp : -portalUp;

        if (!grid.TryGetNeighbor(tile, preferredDir, out PortalTile neighbor))
        {
            // tersine de bak (kenarlarda)
            if (!grid.TryGetNeighbor(tile, -preferredDir, out neighbor))
                return;
        }

        // Aynı düzlem / aynı normal mi?
        if (Vector3.Dot(neighbor.Normal, normal) < 0.99f)
            return;

        // 2 tile hangisi "alt" olsun? (portalUp pozitif yönünde olan üst tile)
        bool neighborIsPositive = Vector3.Dot(neighbor.transform.position - tile.transform.position, portalUp) > 0f;
        PortalTile bottom = neighborIsPositive ? tile : neighbor;
        PortalTile top = neighborIsPositive ? neighbor : tile;

        // İşgal kontrolü + collider disable
        var occ = portal.GetComponent<PortalOccupancy>();
        if (!occ.Place(bottom, top))
            return;

        // Portal transform (merkez: iki tile ortası)
        Vector3 center = (bottom.transform.position + top.transform.position) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(normal, portalUp);
        portal.transform.SetPositionAndRotation(center + normal * surfaceOffset, rot);

        if (debugDraw)
        {
            Debug.DrawRay(hit.point, normal * 0.5f, Color.green, 0.5f);
            Debug.DrawRay(center, portalUp * 0.5f, Color.cyan, 0.5f);
        }
    }

    // Zemin/tavan için: kameranın baktığı yöne göre tile.Up veya tile.Right seç (grid’e snap)
    Vector3 ChooseAxisFromView(PortalTile tile, Vector3 normal)
    {
        Vector3 view = cam.transform.forward;
        Vector3 v = Vector3.ProjectOnPlane(view, normal);
        if (v.sqrMagnitude < 0.0001f) v = Vector3.ProjectOnPlane(cam.transform.up, normal);
        v.Normalize();

        float du = Mathf.Abs(Vector3.Dot(v, tile.Up));
        float dr = Mathf.Abs(Vector3.Dot(v, tile.Right));

        if (du >= dr)
            return Mathf.Sign(Vector3.Dot(v, tile.Up)) * tile.Up;
        else
            return Mathf.Sign(Vector3.Dot(v, tile.Right)) * tile.Right;
    }
}