using System.Collections.Generic;
using System.Linq;
using Grid;
using UI;
using Units;
using UnityEngine;

namespace AI
{
    public class FuzzyAiController : MonoBehaviour
    {
        [Header("Ustawienia Zagrożenia")] public int threatRadius = 6;

        [Header("Defuzyfikacja (Progi)")] public float attackYesThreshold = 0.35f;

        private static void LogFuzzy(string message)
        {
            BattleLog.Add(message);
            Debug.Log($"[FuzzyDecision] {message}");
        }

        public AiDecision Evaluate(Unit activeEnemy, List<Unit> allUnits, GridManager grid)
        {
            var decision = new AiDecision
            {
                stance = (int)Stance.Balanced,
                sequence = ActionSequence.MoveOnly,
                moveX = activeEnemy.gridPos.x,
                moveY = activeEnemy.gridPos.y,
                targetId = -1,
                attack = false
            };

            var players = allUnits.Where(u => u.IsAlive && u.isPlayer).ToList();
            if (players.Count == 0) return decision;

            var occupied = new HashSet<Vector2Int>(allUnits.Where(u => u.IsAlive).Select(u => u.gridPos));
            var reachable = grid.GetReachableTiles(activeEnemy.gridPos, activeEnemy.classData.moveRange, occupied);

            Unit target = ChooseTarget(activeEnemy, players, reachable);
            if (target == null)
            {
                decision.stance = (int)Stance.Defensive;
                LogFuzzy($"AI {activeEnemy.id}: Brak celu w zasięgu. Postawa obronna.");
                return decision;
            }

            decision.targetId = target.id;

            int maxIncomingDmg = EstimateMaxIncomingDamage(activeEnemy, players);
            float survivalRatio = (maxIncomingDmg <= 0) ? 4f : (float)activeEnemy.hp / maxIncomingDmg;

            float muHpVeryLow = FuzzyMembership.Trap(survivalRatio, 0f, 0f, 0.5f, 0.7f);
            float muHpHigh = FuzzyMembership.Trap(survivalRatio, 1.8f, 2.5f, 10f, 10f);

            int dist = GridManager.Manhattan(activeEnemy.gridPos, target.gridPos);
            int rangedR = Mathf.Max(activeEnemy.classData.rangedRange, activeEnemy.classData.meleeRange);
            int moveR = activeEnemy.classData.moveRange;

            float muDistClose = FuzzyMembership.Trap(dist, 0f, 0f, rangedR, rangedR + 1f);
            float muDistFar = FuzzyMembership.Trap(dist, rangedR + (float)moveR - 1f, rangedR + (float)moveR, 99f, 99f);

            float threatRaw =
                players.Count(p => GridManager.Manhattan(p.gridPos, activeEnemy.gridPos) <= threatRadius) / 4f;
            float muThreatHigh = FuzzyMembership.Trap(threatRaw, 0.9f, 1.0f, 10f, 10f);

            LogFuzzy(
                $"<color=orange>Analiza AI #{activeEnemy.id}:</color> HP_Ratio={survivalRatio:F1}, MuDistClose={muDistClose:F2}");

            float muDef = FuzzyMembership.Or(muHpVeryLow, FuzzyMembership.And(muHpVeryLow, muThreatHigh));
            float muAgg = FuzzyMembership.And(muHpHigh, muDistClose);
            float muBal = Mathf.Max(0f, 1f - Mathf.Max(muDef, muAgg));

            decision.stance = CentroidDefuzz3(muDef, muBal, muAgg);

            float muMoveToward = Mathf.Min(1f, muDistFar + 0.3f);
            int moveIntent = CentroidDefuzz3(0f, 0.2f, muMoveToward);

            Vector2Int dest = PickMoveTile(activeEnemy, target, reachable, moveIntent);
            decision.moveX = dest.x;
            decision.moveY = dest.y;

            bool canAttackNow = CanAttackFrom(activeEnemy.gridPos, activeEnemy, target);
            bool canAttackFromDest = CanAttackFrom(dest, activeEnemy, target);

            float muWantAttack = FuzzyMembership.And(muDistClose, 1f - muHpVeryLow);
            bool attackWants = (canAttackNow || canAttackFromDest) && (muWantAttack >= attackYesThreshold);
            decision.attack = attackWants;

            decision.sequence = PickSequence(canAttackNow, canAttackFromDest, attackWants, moveIntent,
                muHpVeryLow > 0.5f, muThreatHigh > 0.5f);

            LogFuzzy($"AI decyduje: <b>{decision.sequence}</b> (Cel: Kot #{target.id})");

            return decision;
        }

        private static int CentroidDefuzz3(float mu0, float mu1, float mu2)
        {
            float sum = mu0 + mu1 + mu2;
            if (sum < Mathf.Epsilon) return 1;
            float centroid = (0f * mu0 + 1f * mu1 + 2f * mu2) / sum;
            return Mathf.RoundToInt(Mathf.Clamp(centroid, 0f, 2f));
        }

        private static ActionSequence PickSequence(bool canAttackNow, bool canAttackFromDest, bool attackWants,
            int moveIntent,
            bool hpVeryLow, bool threatHigh)
        {
            if (!attackWants) return ActionSequence.MoveOnly;

            if (canAttackNow && moveIntent == 0 && (hpVeryLow || threatHigh))
                return ActionSequence.AttackThenMove;

            if (!canAttackNow && canAttackFromDest)
                return ActionSequence.MoveThenAttack;

            return ActionSequence.AttackOnly;
        }

        private static Unit ChooseTarget(Unit enemy, List<Unit> players, List<Vector2Int> reachable)
        {
            var attackable = players.Where(p => CanAttackAfterMove(enemy, p, reachable)).ToList();
            if (attackable.Count > 0)
                return attackable.OrderBy(p => p.hp).ThenBy(p => GridManager.Manhattan(enemy.gridPos, p.gridPos))
                    .First();

            return players.OrderBy(p => GridManager.Manhattan(enemy.gridPos, p.gridPos)).FirstOrDefault();
        }

        private static int EstimateMaxIncomingDamage(Unit enemy, List<Unit> players)
        {
            int total = 0;
            foreach (var p in players)
            {
                int distToEnemy = GridManager.Manhattan(p.gridPos, enemy.gridPos);
                int effectiveDist = Mathf.Max(0, distToEnemy - p.classData.moveRange);
                int dmg = p.GetBestDamageAtDistance(effectiveDist);
                total += Mathf.RoundToInt(dmg * p.DamageDealtMultiplier);
            }

            return total;
        }

        private static bool CanAttackFrom(Vector2Int from, Unit attacker, Unit target)
        {
            int dist = GridManager.Manhattan(from, target.gridPos);
            return attacker.GetBestDamageAtDistance(dist) > 0;
        }

        private static bool CanAttackAfterMove(Unit attacker, Unit target, List<Vector2Int> reachable)
        {
            return reachable.Any(tile => CanAttackFrom(tile, attacker, target));
        }

        private static Vector2Int PickMoveTile(Unit enemy, Unit target, List<Vector2Int> reachable, int moveIntent)
        {
            if (reachable.Count == 0) return enemy.gridPos;

            switch (moveIntent)
            {
                case 0:
                    return reachable.OrderByDescending(tile => GridManager.Manhattan(tile, target.gridPos)).First();
                case 2:
                    var attackTiles = reachable.Where(t => CanAttackFrom(t, enemy, target)).ToList();
                    if (attackTiles.Count > 0)
                        return attackTiles.OrderBy(t => GridManager.Manhattan(t, target.gridPos)).First();
                    return reachable.OrderBy(tile => GridManager.Manhattan(tile, target.gridPos)).First();
                default:
                    return enemy.gridPos;
            }
        }
    }
}
