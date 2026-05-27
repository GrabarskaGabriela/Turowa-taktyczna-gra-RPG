using TMPro;
using Turns;
using Units;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TargetPanelUI : MonoBehaviour
    {
        public TurnManager turnManager;

        public Image portrait;
        public TMP_Text nameText;
        public TMP_Text hpText;
        public Slider hpSlider;

        public Sprite defaultPortrait;

        private void Update()
        {
            if (turnManager == null) return;

            var t = turnManager.LastSelectedTarget;
            bool show = t != null && t.IsAlive;

            gameObject.SetActive(show);
            if (!show) return;

            if (portrait != null)
            {
                var p = t.classData != null ? t.classData.portrait : null;
                portrait.sprite = (p != null) ? p : defaultPortrait;
            }

            if (nameText != null) nameText.text = UnitDisplayNames.UnitName(t);
            if (hpText != null) hpText.text = $"HP: {t.hp}/{t.classData.maxHp}";
            if (hpSlider != null) hpSlider.value = (float)t.hp / t.classData.maxHp;
        }
    }
}
