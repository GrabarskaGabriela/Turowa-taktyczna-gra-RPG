using UnityEngine;
using UnityEngine.Tilemaps;

namespace Grid
{
    public class GridCoordinateSystem : MonoBehaviour
    {
        public Tilemap targetTilemap;

        public Vector3 GridToWorld(Vector2Int p)
        {
            Vector3Int cellPos = new Vector3Int(p.x, p.y, 0);
            return targetTilemap.GetCellCenterWorld(cellPos);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3Int cellPos = targetTilemap.WorldToCell(worldPos);
            return new Vector2Int(cellPos.x, cellPos.y);
        }
    }
}