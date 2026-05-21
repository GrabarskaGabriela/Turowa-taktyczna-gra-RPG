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

            int rawDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * attacker.DamageDealtMultiplier));
            int actualDamage = target.ApplyDamage(rawDamage, false);

            string attackerName = GetAttackerPhrase(attacker);
            string targetName = GetTargetPhrase(target);
            int maxHp = target.classData != null ? target.classData.maxHp : 0;

            AddLog(
                $"<b>{attackerName}</b> zada\u0142 <b>{actualDamage}</b> obra\u017ce\u0144 {targetName}. ({target.hp}/{maxHp} HP)");

            if (!target.IsAlive)
                AddLog($"<b>{GetUnitPhrase(target, false)}</b> zostaje pokonany!");
        }

        private void AddLog(string message)
        {
            if (logManager != null)
                logManager.AddLog(message);
            else
                BattleLog.Add(message);

            Debug.Log($"[Combat] {message}");
        }

        private static string GetAttackerPhrase(Unit unit)
        {
            string owner = unit.isPlayer ? "gracza" : "wroga";
            return $"{GetUnitPhrase(unit, false)} {owner}";
        }

        private static string GetTargetPhrase(Unit unit)
        {
            if (unit.isPlayer)
                return $"{GetUnitPhrase(unit, true)} gracza";

            return $"wrogiemu {GetUnitPhrase(unit, true)}";
        }

        private static string GetUnitPhrase(Unit unit, bool dative)
        {
            string className = unit.classData != null ? unit.classData.className : string.Empty;

            return className switch
            {
                "Light Archer" => dative ? "lekkiemu \u0142ucznikowi" : "Lekki \u0142ucznik",
                "Heavy Archer" => dative ? "ci\u0119\u017ckiemu \u0142ucznikowi" : "Ci\u0119\u017cki \u0142ucznik",
                "Light Warrior" => dative ? "lekkiemu wojownikowi" : "Lekki wojownik",
                "Heavy Warrior" => dative ? "ci\u0119\u017ckiemu wojownikowi" : "Ci\u0119\u017cki wojownik",
                _ => dative ? $"unitowi #{unit.id}" : $"Unit #{unit.id}"
            };
        }
    }
}
