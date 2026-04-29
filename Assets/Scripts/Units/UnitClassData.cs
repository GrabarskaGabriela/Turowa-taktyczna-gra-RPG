using UnityEngine;
using UnityEngine.Serialization;

namespace Units
{
    [CreateAssetMenu(menuName = "RPG/Unit Class Data")]
    public class UnitClassData : ScriptableObject
    {
        public string className;

        public Sprite portrait;

        [FormerlySerializedAs("maxHP")] public int maxHp = 20;
        public int moveRange = 4;

        public int meleeRange = 1;
        public int rangedRange;

        public int meleeDamage = 5;
        public int rangedDamage = 3;
    }
}