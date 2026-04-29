using System.Collections.Generic;
using System.Linq;
using Units;
using UnityEngine;

namespace Turns
{
    public static class UnitQuery
    {
        public static Unit GetUnitAt(List<Unit> units, Vector2Int pos)
            => units.FirstOrDefault(u => u != null && u.IsAlive && u.gridPos == pos);

        public static Unit GetById(List<Unit> units, int id)
            => units.FirstOrDefault(u => u != null && u.IsAlive && u.id == id);

        public static HashSet<Vector2Int> GetOccupiedTiles(List<Unit> units)
            => new HashSet<Vector2Int>(units.Where(u => u != null && u.IsAlive).Select(u => u.gridPos));
    }
}