using UnityEngine;

public class PortalTile : MonoBehaviour
{
    public enum Axis
    {
        PosX, NegX,
        PosY, NegY,
        PosZ, NegZ
    }

    [Header("Orientation (local axis -> world)")]
    public Axis normalAxis = Axis.PosZ;
    public Axis upAxis = Axis.PosY;

    [Header("Colliders")]
    public Collider solidCollider; // yoksa Awake'te otomatik alır

    [Header("Rules")]
    public bool portalable = true;

    public Vector3Int Coord { get; set; }

    PortalOccupancy occupant;

    void Awake()
    {
        if (!solidCollider) solidCollider = GetComponent<Collider>();
        if (PortalGrid.Instance) PortalGrid.Instance.Register(this);
    }

    void OnDisable()
    {
        if (PortalGrid.Instance) PortalGrid.Instance.Unregister(this);
    }

    public Vector3 Normal => transform.TransformDirection(LocalAxis(normalAxis)).normalized;
    public Vector3 Up
    {
        get
        {
            Vector3 n = Normal;
            Vector3 u = transform.TransformDirection(LocalAxis(upAxis)).normalized;

            // u ile n ortogonal değilse düzelt
            Vector3 r = Vector3.Cross(n, u).normalized;
            u = Vector3.Cross(r, n).normalized;
            return u;
        }
    }
    public Vector3 Right => Vector3.Cross(Normal, Up).normalized;

    static Vector3 LocalAxis(Axis a) => a switch
    {
        Axis.PosX => Vector3.right,
        Axis.NegX => Vector3.left,
        Axis.PosY => Vector3.up,
        Axis.NegY => Vector3.down,
        Axis.PosZ => Vector3.forward,
        Axis.NegZ => Vector3.back,
        _ => Vector3.forward
    };

    public bool IsOccupied => occupant != null;

    public bool CanOccupy(PortalOccupancy who)
    {
        if (!portalable) return false;
        return occupant == null || occupant == who;
    }

    public void SetOccupied(PortalOccupancy who)
    {
        occupant = who;
        if (solidCollider) solidCollider.enabled = false;
    }

    public void ClearOccupied(PortalOccupancy who)
    {
        if (occupant != who) return;
        occupant = null;
        if (solidCollider) solidCollider.enabled = true;
    }
}