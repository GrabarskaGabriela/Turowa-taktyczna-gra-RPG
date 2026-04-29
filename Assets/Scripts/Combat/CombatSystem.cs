using Grid;
using UI;
using Units;
using UnityEngine;

namespace Combat
{
    public class CombatSystem : MonoBehaviour
    {
        public LogManager logManager;

        public void Attack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            if (!attacker.IsAlive || !target.IsAlive) return;

            int dist = GridManager.Manhattan(attacker.gridPos, target.gridPos);
            int baseDamage = attacker.GetBestDamageAtDistance(dist);
            if (baseDamage <= 0) return;

            int dealt = Mathf.Max(0, Mathf.RoundToInt(baseDamage * attacker.DamageDealtMultiplier));
            target.ApplyDamage(dealt);

            string attackerName = attacker.classData != null ? attacker.classData.className : $"Unit {attacker.id}";
            string targetName = target.classData != null ? target.classData.className : $"Unit {target.id}";
            string side = attacker.isPlayer ? "[Gracz]" : "[Wróg]";

            logManager?.AddLog(
                $"{side} {attackerName} → {targetName}: -{dealt} HP ({target.hp}/{target.classData.maxHp})");

            if (!target.IsAlive)
                logManager?.AddLog($"✖ {targetName} zostaje pokonany!");
        }
    }
}