using System.Collections.Generic;
using Grid;
using Turns;
using Units;
using UnityEngine;

namespace Spawning
{
    public class BattleSpawner : MonoBehaviour
    {
        [Header("Refs")] public GridManager grid;
        public TurnManager turnManager;
        public GridCoordinateSystem coords;

        [Header("Prefabs")] public Unit unitPrefabPlayer;
        public Unit unitPrefabEnemy;

        [Header("Class data (4 units per side)")]
        public List<UnitClassData> playerTeamClasses = new List<UnitClassData>(4);

        public List<UnitClassData> enemyTeamClasses = new List<UnitClassData>(4);

        [Header("Spawn settings")] public int playerMinX;
        public int playerMaxX = 2;
        public int enemyMinX = 9;
        public int enemyMaxX = 11;

        private int _nextId;

        private void Start() => SpawnTeams4V4();

        private void SpawnTeams4V4()
        {
            turnManager.units.Clear();
            _nextId = 0;
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            List<UnitClassData> playerClasses = ShuffledClasses(playerTeamClasses);
            for (int i = 0; i < 4; i++)
            {
                var cls = i < playerClasses.Count ? playerClasses[i] : null;
                var pos = FindRandomSpawnPos(playerMinX, playerMaxX, occupied);
                var u = SpawnUnit(unitPrefabPlayer, true, cls, pos);
                if (u == null) continue;
                turnManager.units.Add(u);
                occupied.Add(pos);
            }

            List<UnitClassData> enemyClasses = ShuffledClasses(enemyTeamClasses);
            for (int i = 0; i < 4; i++)
            {
                var cls = i < enemyClasses.Count ? enemyClasses[i] : null;
                var pos = FindRandomSpawnPos(enemyMinX, enemyMaxX, occupied);
                var u = SpawnUnit(unitPrefabEnemy, false, cls, pos);
                if (u == null) continue;
                turnManager.units.Add(u);
                occupied.Add(pos);
            }

            turnManager.BeginBattle();
        }

        private static List<UnitClassData> ShuffledClasses(List<UnitClassData> source)
        {
            var list = new List<UnitClassData>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        private Vector2Int FindRandomSpawnPos(int minX, int maxX, HashSet<Vector2Int> occupied)
        {
            var candidates = new List<Vector2Int>();

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = 0; y < GridManager.Height; y++)
                {
                    var p = new Vector2Int(x, y);
                    if (GridManager.InBounds(p) && !grid.IsBlockedByWall(p) && !occupied.Contains(p))
                        candidates.Add(p);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"BattleSpawner: brak wolnych pol w strefie x={minX}-{maxX}!");
                return new Vector2Int(minX, 0);
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private Unit SpawnUnit(Unit prefab, bool isPlayer, UnitClassData cls, Vector2Int gridPos)
        {
            if (prefab == null || cls == null) return null;

            var u = Instantiate(prefab);
            u.id = _nextId++;
            u.isPlayer = isPlayer;
            u.classData = cls;
            u.hp = cls.maxHp;
            u.gridPos = gridPos;

            SpriteRenderer sr = u.GetComponent<SpriteRenderer>();
            if (sr != null && cls.portrait != null)
            {
                sr.sprite = cls.portrait;
                sr.color = Color.white;
            }

            UnitView uv = u.GetComponent<UnitView>();
            if (uv != null) uv.coords = this.coords;

            u.transform.position = coords.GridToWorld(gridPos);
            return u;
        }
    }
}