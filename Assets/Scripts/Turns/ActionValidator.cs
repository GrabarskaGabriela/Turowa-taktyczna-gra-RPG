using System.Collections.Generic;
using Units;
using UnityEngine;

namespace Turns
{
    public static class ActionValidator
    {
        public static bool CanMoveTo(Unit unit, Vector2Int dest, List<Vector2Int> reachable)
        {
            if (unit == null || !unit.IsAlive) return false;
            return reachable.Contains(dest);
        }

        public static bool CanAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return false;
            if (!attacker.IsAlive || !target.IsAlive) return false;
            if (attacker.isPlayer == target.isPlayer) return false;

            return attacker.GetBestDamageFromPosition(attacker.gridPos, target.gridPos) > 0;
        }
    }
}
