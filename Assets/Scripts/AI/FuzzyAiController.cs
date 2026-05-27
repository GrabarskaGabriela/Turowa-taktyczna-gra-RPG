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
        [Header("Ustawienia zagrozenia")] public int threatRadius = 6;

        [Header("Defuzyfikacja")] public float attackYesThreshold = 0.35f;

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
                LogFuzzy($"{UnitDisplayNames.UnitName(activeEnemy)}: brak celu. Postawa defensywna.");
                return decision;
            }

            decision.targetId = target.id;

            int maxIncomingDmg = EstimateMaxIncomingDamage(activeEnemy, players);
            float survivalRatio = maxIncomingDmg <= 0 ? 4f : (float)activeEnemy.hp / maxIncomingDmg;
            float threatRatio = activeEnemy.hp <= 0 ? 1f : (float)maxIncomingDmg / activeEnemy.hp;

            float muHpVeryLow = FuzzyMembership.Trap(survivalRatio, 0f, 0f, 0.5f, 0.75f);
            float muHpLow = FuzzyMembership.Tri(survivalRatio, 0.5f, 1.4f, 2.3f);
            float muHpMedium = FuzzyMembership.Tri(survivalRatio, 1.8f, 3.0f, 4.2f);
            float muHpHigh = FuzzyMembership.Trap(survivalRatio, 3.5f, 4.5f, 10f, 10f);

            int dist = GridManager.Manhattan(activeEnemy.gridPos, target.gridPos);
            int attackRange = GetEffectiveAttackRange(activeEnemy);
            int moveRange = activeEnemy.classData.moveRange;
            int attackAfterMoveRange = attackRange + moveRange;

            float muDistClose = FuzzyMembership.Trap(dist, 0f, 0f, attackRange, attackRange + 1f);
            float muDistMedium = FuzzyMembership.Tri(dist, attackRange, attackRange + (moveRange * 0.5f), attackAfterMoveRange + 1f);
            float muDistFar = FuzzyMembership.Trap(dist, attackAfterMoveRange, attackAfterMoveRange + 1f, 99f, 99f);

            float nearbyEnemies = players.Count(p => GridManager.Manhattan(p.gridPos, activeEnemy.gridPos) <= threatRadius) / 4f;
            float muThreatHigh = Max(
                FuzzyMembership.Trap(nearbyEnemies, 0.45f, 0.75f, 1f, 1f),
                FuzzyMembership.Trap(threatRatio, 0.6f, 0.9f, 3f, 3f));
            float muThreatLow = 1f - muThreatHigh;

            LogFuzzy(
                $"<color=orange>{UnitDisplayNames.UnitName(activeEnemy)}</color> HP={survivalRatio:F1}, " +
                $"dist={muDistClose:F2}/{muDistMedium:F2}/{muDistFar:F2}, threat={muThreatHigh:F2}");

            float muDefensive = Max(
                muHpVeryLow,
                FuzzyMembership.And(muHpLow, muThreatHigh),
                Min(muDistClose, muThreatHigh, Max(muHpVeryLow, muHpLow)));
            float muAggressive = Max(
                FuzzyMembership.And(muHpHigh, muDistClose),
                Min(muHpMedium, muDistClose, muThreatLow),
                FuzzyMembership.And(muHpHigh, muDistMedium));
            float muBalanced = Max(
                muHpMedium,
                FuzzyMembership.And(muHpLow, muDistMedium),
                FuzzyMembership.And(muHpHigh, muDistFar));

            decision.stance = CentroidDefuzz3(muDefensive, muBalanced, muAggressive);

            float muMoveAway = Max(
                FuzzyMembership.And(muHpVeryLow, muThreatHigh),
                Min(muHpLow, muDistClose, muThreatHigh));
            float muStay = Max(
                Min(muDistClose, muHpMedium, muThreatLow),
                FuzzyMembership.And(muDistClose, muHpHigh));
            float muMoveToward = Max(
                muDistFar,
                FuzzyMembership.And(muDistMedium, 1f - muHpVeryLow));
            int moveIntent = CentroidDefuzz3(muMoveAway, muStay, muMoveToward);

            Vector2Int dest = PickMoveTile(activeEnemy, target, reachable, moveIntent);
            decision.moveX = dest.x;
            decision.moveY = dest.y;

            bool canAttackNow = CanAttackFrom(activeEnemy.gridPos, activeEnemy, target);
            bool canAttackFromDest = CanAttackFrom(dest, activeEnemy, target);
            float muWantAttack = Max(
                Min(muDistClose, 1f - muHpVeryLow, 1f),
                Min(muDistMedium, muHpHigh, canAttackFromDest ? 1f : 0f),
                Min(muThreatLow, muHpMedium, canAttackNow ? 1f : 0f));

            bool attackWants = (canAttackNow || canAttackFromDest) && muWantAttack >= attackYesThreshold;
            decision.attack = attackWants;
            decision.sequence = PickSequence(canAttackNow, canAttackFromDest, attackWants, moveIntent,
                muHpVeryLow > 0.5f, muThreatHigh > 0.5f);

            LogFuzzy($"AI decyduje: <b>{decision.sequence}</b>, cel: {UnitDisplayNames.UnitName(target)}, postawa: {UnitDisplayNames.StanceName((Stance)decision.stance)}");
            return decision;
        }

        private static int CentroidDefuzz3(float mu0, float mu1, float mu2)
        {
            float sum = mu0 + mu1 + mu2;
            if (sum < Mathf.Epsilon) return 1;
            float centroid = ((0f * mu0) + (1f * mu1) + (2f * mu2)) / sum;
            return Mathf.RoundToInt(Mathf.Clamp(centroid, 0f, 2f));
        }

        private static float Min(params float[] values)
        {
            float result = 1f;
            foreach (float value in values)
                result = Mathf.Min(result, value);
            return result;
        }

        private static float Max(params float[] values)
        {
            float result = 0f;
            foreach (float value in values)
                result = Mathf.Max(result, value);
            return result;
        }

        private static ActionSequence PickSequence(bool canAttackNow, bool canAttackFromDest, bool attackWants,
            int moveIntent, bool hpVeryLow, bool threatHigh)
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
            return players
                .OrderByDescending(p => ScoreTarget(enemy, p, reachable))
                .ThenBy(p => GridManager.Manhattan(enemy.gridPos, p.gridPos))
                .FirstOrDefault();
        }

        private static float ScoreTarget(Unit enemy, Unit player, List<Vector2Int> reachable)
        {
            float hpPressure = player.classData == null || player.classData.maxHp <= 0
                ? 0f
                : 1f - ((float)player.hp / player.classData.maxHp);
            float attackOpportunity = CanAttackAfterMove(enemy, player, reachable) ? 1f : 0f;
            float distancePressure = 1f / (1f + GridManager.Manhattan(enemy.gridPos, player.gridPos));

            return (attackOpportunity * 100f) + (hpPressure * 30f) + (distancePressure * 10f);
        }

        private static int EstimateMaxIncomingDamage(Unit enemy, List<Unit> players)
        {
            int total = 0;
            foreach (var player in players)
            {
                int distToEnemy = GridManager.Manhattan(player.gridPos, enemy.gridPos);
                int effectiveDist = Mathf.Max(0, distToEnemy - player.classData.moveRange);
                int dmg = player.GetBestDamageAtDistance(effectiveDist);
                total += Mathf.RoundToInt(dmg * player.DamageDealtMultiplier);
            }

            return total;
        }

        private static bool CanAttackFrom(Vector2Int from, Unit attacker, Unit target)
        {
            return attacker.GetBestDamageFromPosition(from, target.gridPos) > 0;
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
                case 1:
                    if (CanAttackFrom(enemy.gridPos, enemy, target))
                        return enemy.gridPos;
                    var stableAttackTiles = reachable.Where(t => CanAttackFrom(t, enemy, target)).ToList();
                    if (stableAttackTiles.Count > 0)
                        return stableAttackTiles.OrderBy(t => GridManager.Manhattan(t, enemy.gridPos)).First();
                    return enemy.gridPos;
                case 2:
                    var attackTiles = reachable.Where(t => CanAttackFrom(t, enemy, target)).ToList();
                    if (attackTiles.Count > 0)
                        return attackTiles.OrderBy(t => GridManager.Manhattan(t, target.gridPos)).First();
                    return reachable.OrderBy(tile => GridManager.Manhattan(tile, target.gridPos)).First();
                default:
                    return enemy.gridPos;
            }
        }

        private static int GetEffectiveAttackRange(Unit unit)
        {
            if (unit.classData == null) return 0;

            int range = unit.classData.meleeDamage > 0 ? unit.classData.meleeRange : 0;
            if (unit.HasRangedAttackResource)
                range = Mathf.Max(range, unit.classData.rangedRange);
            return range;
        }
    }
}
