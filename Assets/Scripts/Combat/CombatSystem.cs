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

            var attack = attacker.GetBestAttackFromPosition(attacker.gridPos, target.gridPos);
            int baseDamage = attack.damage;
            if (baseDamage <= 0) return;

            attacker.ConsumeAttackResource(attack.kind);

            int rawDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * attacker.DamageDealtMultiplier));
            int actualDamage = target.ApplyDamage(rawDamage, false);

            string attackerName = UnitDisplayNames.UnitName(attacker);
            string targetName = UnitDisplayNames.UnitName(target);
            int maxHp = target.classData != null ? target.classData.maxHp : 0;
            string attackType = attack.kind == UnitAttackKind.Ranged ? "atakiem dystansowym" : "atakiem wręcz";

            AddLog(
                $"<b>{attackerName}</b> zadał <b>{actualDamage}</b> obrażeń jednostce <b>{targetName}</b> {attackType}. ({target.hp}/{maxHp} HP)");

            if (!target.IsAlive)
                AddLog($"<b>{targetName}</b> zostaje pokonany!");
        }

        private void AddLog(string message)
        {
            if (logManager != null)
                logManager.AddLog(message);
            else
                BattleLog.Add(message);

            Debug.Log($"[Combat] {message}");
        }
    }
}
