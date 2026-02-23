using System.Collections.Generic;
using UnityEngine;

public class PortalGrid : MonoBehaviour
{
    public static PortalGrid Instance { get; private set; }

    [Tooltip("Tile merkezleri arasındaki mesafe (genelde 1)")]
    public float cellSize = 1f;

    readonly Dictionary<Vector3Int, PortalTile> tiles = new();

    void Awake()
    {
        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        tiles.Clear();
        var all = FindObjectsOfType<PortalTile>(true);
        foreach (var t in all)
            Register(t);
    }

    public void Register(PortalTile tile)
    {
        if (!tile) return;
        Vector3Int c = WorldToCoord(tile.transform.position);
        tiles[c] = tile;
        tile.Coord = c;
    }

    public void Unregister(PortalTile tile)
    {
        if (!tile) return;
        tiles.Remove(tile.Coord);
    }

    public Vector3Int WorldToCoord(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize)
        );
    }

    public bool TryGetTile(Vector3Int coord, out PortalTile tile) => tiles.TryGetValue(coord, out tile);

    // Dünya yönünü grid adımına çevir (±X, ±Y, ±Z)
    public static Vector3Int DirToStep(Vector3 dir)
    {
        dir = dir.normalized;
        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);
        float az = Mathf.Abs(dir.z);

        if (ay >= ax && ay >= az) return new Vector3Int(0, dir.y >= 0 ? 1 : -1, 0);
        if (ax >= ay && ax >= az) return new Vector3Int(dir.x >= 0 ? 1 : -1, 0, 0);
        return new Vector3Int(0, 0, dir.z >= 0 ? 1 : -1);
    }

    public bool TryGetNeighbor(PortalTile tile, Vector3 worldDir, out PortalTile neighbor)
    {
        neighbor = null;
        if (!tile) return false;

        Vector3Int step = DirToStep(worldDir);
        return TryGetTile(tile.Coord + step, out neighbor);
    }
}