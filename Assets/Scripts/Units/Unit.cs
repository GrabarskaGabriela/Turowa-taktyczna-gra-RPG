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
        public void Reset() { moveUsed = false; attackUsed = false; }
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

        private static void LogToBattleLog(string message)
        {
            LogManager log = Object.FindAnyObjectByType<LogManager>();
            if (log != null)
            {
                log.AddLog(message);
            }
            Debug.Log(message);
        }

        public int GetBestDamageAtDistance(int manhattanDist)
        {
            if (classData == null) return 0;
            if (manhattanDist <= classData.meleeRange) return classData.meleeDamage;
            if (classData.rangedRange > 0 && manhattanDist <= classData.rangedRange) return classData.rangedDamage;
            return 0;
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

        public void ApplyDamage(int raw)
        {
            int dmg = Mathf.Max(0, Mathf.RoundToInt(raw * DamageTakenMultiplier));
            hp -= dmg;
            if (hp < 0) hp = 0;
            
            string unitName = isPlayer ? $"<color=blue>Kot #{id}</color>" : $"<color=red>Wróg #{id}</color>";
            string logMessage = $"{unitName} otrzymuje <b>{dmg}</b> obrażeń. (HP: {hp})";
        
            LogToBattleLog(logMessage);

            if (hp == 0)
            {
                LogToBattleLog($"{unitName} <color=black>został pokonany!</color>");
            }
        }
        
        public void ChangeStance(Stance newStance)
        {
            stance = newStance;
            string unitName = isPlayer ? "Twój kot" : "Wrogi kot";
            LogToBattleLog($"{unitName} zmienia postawę na <b>{newStance}</b>.");
        }
    }
}