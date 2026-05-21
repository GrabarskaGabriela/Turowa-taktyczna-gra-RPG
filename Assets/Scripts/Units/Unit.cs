using UI;
using UnityEngine;

namespace Units
{
    public enum Stance { Defensive, Balanced, Aggressive }
    public enum ActionSequence { MoveOnly, AttackOnly, MoveThenAttack, AttackThenMove }

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

        public bool IsAlive => hp > 0;
        private bool _defeatVisualsHidden;

        private static void LogToBattleLog(string message)
        {
            BattleLog.Add(message);
            Debug.Log(message);
        }

        public int GetBestDamageAtDistance(int manhattanDist)
        {
            if (classData == null) return 0;

            int bestDamage = 0;
            if (classData.meleeDamage > 0 && manhattanDist <= classData.meleeRange)
                bestDamage = Mathf.Max(bestDamage, classData.meleeDamage);

            if (classData.rangedDamage > 0 && classData.rangedRange > 0 && manhattanDist <= classData.rangedRange)
                bestDamage = Mathf.Max(bestDamage, classData.rangedDamage);

            return bestDamage;
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

            if (!logDamage)
            {
                if (defeated)
                    HideDefeatedUnit();
                return actualDamage;
            }

            string unitName = isPlayer ? $"<color=blue>Kot #{id}</color>" : $"<color=red>Wrog #{id}</color>";
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
            gameObject.SetActive(false);
        }

        public void ChangeStance(Stance newStance)
        {
            stance = newStance;
            string unitName = isPlayer ? "Twoj kot" : "Wrogi kot";
            LogToBattleLog($"{unitName} zmienia postawe na <b>{newStance}</b>.");
        }
    }
}
