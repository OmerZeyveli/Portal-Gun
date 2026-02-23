using UnityEngine;

[DisallowMultipleComponent]
public class PortalOccupancy : MonoBehaviour
{
    PortalTile a, b;

    public void Clear()
    {
        if (a) a.ClearOccupied(this);
        if (b) b.ClearOccupied(this);
        a = b = null;
    }

    public bool Place(PortalTile t0, PortalTile t1)
    {
        if (!t0 || !t1) return false;

        // başka portal işgal etmiş mi?
        if (!t0.CanOccupy(this) || !t1.CanOccupy(this))
            return false;

        // önce eskileri aç
        Clear();

        a = t0;
        b = t1;

        a.SetOccupied(this);
        b.SetOccupied(this);
        return true;
    }
}