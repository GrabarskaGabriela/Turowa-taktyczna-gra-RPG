using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Wizualizacja scian")] public Tilemap wallsTilemap;

        [Header("Wizualizacja planszy")] public Tilemap visualTilemap;
        public TileBase lightTile;
        public TileBase darkTile;

        [Header("Kafelki zasiegu (podmiana sprite)")]
        public TileBase moveRangeTile;

        public TileBase attackRangeTile;

        public const int Width = 12;
        public const int Height = 12;

        public int[,] Map = new int[Width, Height];

        private void Start()
        {
            GenerateVisualGrid();
        }

        public void GenerateVisualGrid()
        {
            if (visualTilemap == null) return;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    TileBase t = (x + y) % 2 == 0 ? lightTile : darkTile;
                    visualTilemap.SetTile(new Vector3Int(x, y, 0), t);
                }
            }
        }

        public void ShowRange(IEnumerable<Vector2Int> tiles, bool isAttack)
        {
            ClearRange();
            TileBase tileToUse = isAttack ? attackRangeTile : moveRangeTile;
            foreach (var pos in tiles)
                if (InBounds(pos))
                    visualTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tileToUse);
        }

        public void ClearRange()
        {
            GenerateVisualGrid();
        }

        public static bool InBounds(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;

        public bool IsBlockedByWall(Vector2Int p)
        {
            if (wallsTilemap != null && wallsTilemap.HasTile(new Vector3Int(p.x, p.y, 0)))
                return true;
            return Map[p.x, p.y] == 1;
        }

        public static IEnumerable<Vector2Int> GetNeighbors4(Vector2Int p)
        {
            yield return new Vector2Int(p.x + 1, p.y);
            yield return new Vector2Int(p.x - 1, p.y);
            yield return new Vector2Int(p.x, p.y + 1);
            yield return new Vector2Int(p.x, p.y - 1);
        }

        public List<Vector2Int> GetReachableTiles(Vector2Int start, int range, HashSet<Vector2Int> occupied)
        {
            var reachable = new List<Vector2Int>();
            var dist = new Dictionary<Vector2Int, int>();
            var q = new Queue<Vector2Int>();

            dist[start] = 0;
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                int d = dist[cur];
                reachable.Add(cur);
                if (d == range) continue;

                foreach (var n in GetNeighbors4(cur))
                {
                    if (!IsPassable(n, start, occupied, dist))
                        continue;
                    dist[n] = d + 1;
                    q.Enqueue(n);
                }
            }

            return reachable;
        }

        private bool IsPassable(Vector2Int n, Vector2Int start, HashSet<Vector2Int> occupied,
            Dictionary<Vector2Int, int> dist)
        {
            if (!InBounds(n) || IsBlockedByWall(n)) return false;
            if (n != start && occupied.Contains(n)) return false;
            if (dist.ContainsKey(n)) return false;
            return true;
        }

        public static int Manhattan(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}