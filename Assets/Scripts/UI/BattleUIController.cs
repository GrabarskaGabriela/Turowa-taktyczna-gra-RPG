using TMPro;
using Turns;
using Units;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Refs")] public TurnManager turnManager;

        [Header("Top Bar")] public TMP_Text turnText;
        public TMP_Text phaseText;

        [Header("Active Unit Panel")] public Image unitPortrait;
        public TMP_Text unitNameText;
        public TMP_Text unitHpText;
        public Slider unitHpSlider;
        public TMP_Text stanceText;

        [Header("Buttons")] public Button skipMoveButton;
        public Button skipAttackButton;
        public Button endTurnButton;

        [Header("Optional")] public Sprite defaultPortrait;

        private void Start()
        {
            if (skipMoveButton != null) skipMoveButton.onClick.AddListener(() => turnManager.PlayerSkipMove());
            if (skipAttackButton != null) skipAttackButton.onClick.AddListener(() => turnManager.PlayerSkipAttack());
            if (endTurnButton != null) endTurnButton.onClick.AddListener(() => turnManager.PlayerEndTurn());
        }

        private void Update()
        {
            if (turnManager == null) return;

            var u = turnManager.ActiveUnit;
            var st = turnManager.State;

            UpdateTurnText(u);
            UpdatePhaseText(st);
            UpdateUnitPanel(u);
            UpdateButtons(u, st);
        }

        private void UpdateTurnText(Unit u)
        {
            if (turnText == null) return;

            if (u == null)
            {
                turnText.text = "–";
                return;
            }

            turnText.text = u.isPlayer ? "Player Turn" : "AI Turn";
        }

        private void UpdatePhaseText(BattleState st)
        {
            if (phaseText == null) return;
            phaseText.text = st switch
            {
                BattleState.PlayerChooseMove => "Phase: Choose Move",
                BattleState.PlayerChooseAttackTarget => "Phase: Choose Attack",
                BattleState.ExecutingAiTurn => "Phase: AI Executing",
                _ => "Phase: –"
            };
        }

        private void UpdateUnitPanel(Unit u)
        {
            if (u == null)
            {
                SetEmptyPanel();
                return;
            }

            if (unitNameText != null) unitNameText.text = $"{u.classData.className} (ID {u.id})";
            if (unitHpText != null) unitHpText.text = $"HP: {u.hp}/{u.classData.maxHp}";
            if (stanceText != null) stanceText.text = $"Stance: {u.stance}";
            if (unitHpSlider != null)
                unitHpSlider.value = u.classData.maxHp <= 0 ? 0f : (float)u.hp / u.classData.maxHp;
            if (unitPortrait != null)
                unitPortrait.sprite = u.classData?.portrait != null ? u.classData.portrait : defaultPortrait;
        }

        private void SetEmptyPanel()
        {
            if (unitNameText != null) unitNameText.text = "–";
            if (unitHpText != null) unitHpText.text = "HP: –";
            if (stanceText != null) stanceText.text = "Stance: –";
            if (unitHpSlider != null) unitHpSlider.value = 0f;
            if (unitPortrait != null) unitPortrait.sprite = defaultPortrait;
        }

        private void UpdateButtons(Unit u, BattleState st)
        {
            bool isPlayer = u != null && u.isPlayer;
            bool choosingMove = st == BattleState.PlayerChooseMove;
            bool choosingAttack = st == BattleState.PlayerChooseAttackTarget;

            if (skipMoveButton != null) skipMoveButton.interactable = isPlayer && choosingMove;
            if (skipAttackButton != null) skipAttackButton.interactable = isPlayer && choosingAttack;
            if (endTurnButton != null) endTurnButton.interactable = isPlayer && (choosingMove || choosingAttack);
        }
    }
}