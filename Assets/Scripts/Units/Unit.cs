using UI;
using UnityEngine;

namespace Units
{
    public enum Stance { Defensive, Balanced, Aggressive }
    public enum ActionSequence { MoveOnly, AttackOnly, MoveThenAttack, AttackThenMove }
    public enum UnitAttackKind { None, Melee, Ranged }

    public static class UnitDisplayNames
    {
        public static string ClassName(Unit unit)
        {
            return ClassName(unit != null ? unit.classData : null);
        }

        public static string ClassName(UnitClassData classData)
        {
            string className = classData != null ? classData.className : string.Empty;

            return className switch
            {
                "Light Archer" => "Lekki łucznik",
                "Heavy Archer" => "Ciężki łucznik",
                "Light Warrior" => "Lekki wojownik",
                "Heavy Warrior" => "Ciężki wojownik",
                _ => string.IsNullOrWhiteSpace(className) ? "Jednostka" : className
            };
        }

        public static string UnitName(Unit unit)
        {
            if (unit == null)
                return "Jednostka";

            string side = unit.isPlayer ? "Kot" : "Wróg";
            return $"{side} - {ClassName(unit)}";
        }

        public static string StanceName(Stance stance)
        {
            return stance switch
            {
                Stance.Defensive => "Defensywna",
                Stance.Aggressive => "Agresywna",
                _ => "Zbalansowana"
            };
        }
    }

    [System.Serializable]
    public class PlayerActionState
    {
        public bool moveUsed;
        public bool attackUsed;
        public void Reset()
        {
            moveUsed = false;
            attackUsed = false;
        }
    }

    [System.Serializable]
    public class AiDecision
    {
        public int stance;
        public ActionSequence sequence;
        public int moveX, moveY;
        public int targetId;
        public bool attack;
    }

    public class Unit : MonoBehaviour
    {
        public int id;
        public bool isPlayer;

        public UnitClassData classData;
        public int hp;
        public Stance stance = Stance.Balanced;
        public Vector2Int gridPos;
        public int remainingRangedAttacks = -1;

        public bool IsAlive => hp > 0;
        private bool _defeatVisualsHidden;

        private static void LogToBattleLog(string message)
        {
            BattleLog.Add(message);
            Debug.Log(message);
        }

        public void InitializeCombatResources()
        {
            remainingRangedAttacks = classData != null ? classData.rangedAttackUses : 0;
        }

        public bool HasRangedAttackResource =>
            classData != null && classData.rangedDamage > 0 && classData.rangedRange > 0 &&
            (classData.rangedAttackUses < 0 || remainingRangedAttacks > 0);

        public int GetBestDamageAtDistance(int manhattanDist)
        {
            return GetBestDamageAtDistance(manhattanDist, true);
        }

        public int GetBestDamageAtDistance(int manhattanDist, bool respectRangedResource)
        {
            if (classData == null) return 0;

            int bestDamage = 0;
            if (classData.meleeDamage > 0 && manhattanDist <= classData.meleeRange)
                bestDamage = Mathf.Max(bestDamage, classData.meleeDamage);

            bool canUseRanged = !respectRangedResource || HasRangedAttackResource;
            if (canUseRanged && manhattanDist > classData.meleeRange &&
                classData.rangedDamage > 0 && classData.rangedRange > 0 && manhattanDist <= classData.rangedRange)
                bestDamage = Mathf.Max(bestDamage, classData.rangedDamage);

            return bestDamage;
        }

        public int GetBestDamageFromPosition(Vector2Int attackerPos, Vector2Int targetPos)
        {
            return GetBestAttackFromPosition(attackerPos, targetPos).damage;
        }

        public (UnitAttackKind kind, int damage) GetBestAttackFromPosition(Vector2Int attackerPos, Vector2Int targetPos)
        {
            if (classData == null) return (UnitAttackKind.None, 0);

            int manhattanDist = Mathf.Abs(attackerPos.x - targetPos.x) + Mathf.Abs(attackerPos.y - targetPos.y);
            UnitAttackKind bestKind = UnitAttackKind.None;
            int bestDamage = 0;

            if (classData.meleeDamage > 0 && manhattanDist <= classData.meleeRange)
            {
                bestKind = UnitAttackKind.Melee;
                bestDamage = classData.meleeDamage;
            }

            if (HasRangedAttackResource && manhattanDist > classData.meleeRange &&
                classData.rangedDamage > bestDamage && manhattanDist <= classData.rangedRange)
            {
                bestKind = UnitAttackKind.Ranged;
                bestDamage = classData.rangedDamage;
            }

            return (bestKind, bestDamage);
        }

        public void ConsumeAttackResource(UnitAttackKind attackKind)
        {
            if (attackKind != UnitAttackKind.Ranged || classData == null || classData.rangedAttackUses < 0)
                return;

            remainingRangedAttacks = Mathf.Max(0, remainingRangedAttacks - 1);
        }

        public float DamageDealtMultiplier => stance switch
        {
            Stance.Defensive => 0.85f,
            Stance.Aggressive => 1.15f,
            _ => 1.0f
        };

        private float DamageTakenMultiplier => stance switch
        {
            Stance.Defensive => 0.85f,
            Stance.Aggressive => 1.15f,
            _ => 1.0f
        };

        public int ApplyDamage(int raw, bool logDamage = true)
        {
            int hpBefore = hp;
            int dmg = Mathf.Max(0, Mathf.RoundToInt(raw * DamageTakenMultiplier));
            hp -= dmg;
            if (hp < 0) hp = 0;

            int actualDamage = hpBefore - hp;
            bool defeated = hp == 0;

            if (actualDamage > 0 && !defeated)
            {
                UnitView view = GetComponent<UnitView>();
                if (view != null)
                    view.PlayDamageFeedback(false);
            }

            if (!logDamage)
            {
                if (defeated)
                    HideDefeatedUnit();
                return actualDamage;
            }

            string color = isPlayer ? "blue" : "red";
            string unitName = $"<color={color}>{UnitDisplayNames.UnitName(this)}</color>";
            string logMessage = $"{unitName} otrzymuje <b>{actualDamage}</b> obrazen. (HP: {hp})";

            LogToBattleLog(logMessage);

            if (defeated)
            {
                LogToBattleLog($"{unitName} <color=black>zostal pokonany!</color>");
                HideDefeatedUnit();
            }

            return actualDamage;
        }

        private void HideDefeatedUnit()
        {
            if (_defeatVisualsHidden) return;

            _defeatVisualsHidden = true;
            UnitView view = GetComponent<UnitView>();
            if (view != null)
                view.PlayDamageFeedback(true);
            else
                gameObject.SetActive(false);
        }

        public void ChangeStance(Stance newStance)
        {
            stance = newStance;
            LogToBattleLog($"{UnitDisplayNames.UnitName(this)} zmienia postawe na <b>{UnitDisplayNames.StanceName(newStance)}</b>.");
        }
    }
}
