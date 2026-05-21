using TMPro;
using Turns;
using Units;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
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

        [Header("Buttons")]
        [FormerlySerializedAs("skipMoveButton")] public Button moveButton;
        [FormerlySerializedAs("skipAttackButton")] public Button attackButton;
        public Button endTurnButton;

        [Header("Optional")] public Sprite defaultPortrait;
        public TMP_Text turnOrderText;
        public Button pauseButton;
        public GameObject pauseOverlay;
        public Button resumeButton;
        public Button mainMenuButton;

        private void Start()
        {
            EnsureRuntimeUi();
            HideStanceDisplay();

            SetButtonLabel(moveButton, "Move");
            SetButtonLabel(attackButton, "Attack");
            SetButtonLabel(endTurnButton, "End Turn");

            if (moveButton != null) moveButton.onClick.AddListener(() => turnManager.PlayerChooseMove());
            if (attackButton != null) attackButton.onClick.AddListener(() => turnManager.PlayerChooseAttack());
            if (endTurnButton != null) endTurnButton.onClick.AddListener(() => turnManager.PlayerEndTurn());
            if (pauseButton != null) pauseButton.onClick.AddListener(PauseGame);
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
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
            UpdateTurnOrderText();
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
                BattleState.PlayerChooseAction => "Phase: Choose Action",
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
            if (unitHpSlider != null)
                unitHpSlider.value = u.classData.maxHp <= 0 ? 0f : (float)u.hp / u.classData.maxHp;
            if (unitPortrait != null)
                unitPortrait.sprite = u.classData?.portrait != null ? u.classData.portrait : defaultPortrait;
        }

        private void SetEmptyPanel()
        {
            if (unitNameText != null) unitNameText.text = "–";
            if (unitHpText != null) unitHpText.text = "HP: –";
            if (unitHpSlider != null) unitHpSlider.value = 0f;
            if (unitPortrait != null) unitPortrait.sprite = defaultPortrait;
        }

        private void UpdateButtons(Unit u, BattleState st)
        {
            bool isPlayer = u != null && u.isPlayer;
            bool choosingAction = st == BattleState.PlayerChooseAction;
            bool choosingMove = st == BattleState.PlayerChooseMove;
            bool choosingAttack = st == BattleState.PlayerChooseAttackTarget;

            if (moveButton != null) moveButton.interactable = isPlayer && choosingAction && turnManager.CanPlayerChooseMove;
            if (attackButton != null) attackButton.interactable = isPlayer && choosingAction && turnManager.CanPlayerChooseAttack;
            if (endTurnButton != null) endTurnButton.interactable = isPlayer && (choosingAction || choosingMove || choosingAttack);
        }

        private void UpdateTurnOrderText()
        {
            if (turnOrderText == null || turnManager == null) return;

            var order = turnManager.CurrentTurnOrder;
            if (order == null || order.Count == 0)
            {
                turnOrderText.text = "Turn Order:\n-";
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder("Turn Order:");
            for (int i = 0; i < order.Count; i++)
            {
                Unit unit = order[i];
                string side = unit.isPlayer ? "P" : "AI";
                string className = unit.classData != null ? unit.classData.className : "Unit";
                string marker = i == 0 ? ">" : "-";
                builder.AppendLine();
                builder.Append($"{marker} {side} #{unit.id} {className}");
            }

            turnOrderText.text = builder.ToString();
        }

        private void EnsureRuntimeUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            if (turnOrderText == null)
                turnOrderText = CreateText(parent, "TurnOrderText", "Turn Order:", new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -72f), new Vector2(290f, 190f), 18,
                    TextAlignmentOptions.TopRight);

            if (pauseButton == null)
                pauseButton = CreateButton(parent, "PauseButton", "Pause", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(120f, 42f));

            CreatePauseOverlay(parent);
        }

        private void HideStanceDisplay()
        {
            if (stanceText != null)
                stanceText.gameObject.SetActive(false);
        }

        private void CreatePauseOverlay(Transform parent)
        {
            if (pauseOverlay != null) return;

            pauseOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pauseOverlay.transform.SetParent(parent, false);
            RectTransform overlayRect = pauseOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            pauseOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            CreateText(pauseOverlay.transform, "PauseTitle", "Paused", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(300f, 54f), 36,
                TextAlignmentOptions.Center);
            resumeButton = CreateButton(pauseOverlay.transform, "ResumeButton", "Resume", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(210f, 46f));
            mainMenuButton = CreateButton(pauseOverlay.transform, "MainMenuButton", "Main Menu", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(210f, 46f));

            pauseOverlay.SetActive(false);
        }

        private void PauseGame()
        {
            Time.timeScale = 0f;
            if (pauseOverlay != null)
                pauseOverlay.SetActive(true);
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            if (pauseOverlay != null)
                pauseOverlay.SetActive(false);
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = label;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.15f, 0.12f, 0.92f);

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.24f, 0.18f, 1f);
            colors.pressedColor = new Color(0.10f, 0.08f, 0.06f, 1f);
            colors.disabledColor = new Color(0.12f, 0.11f, 0.10f, 0.55f);
            button.colors = colors;

            TMP_Text text = CreateText(go.transform, "Text", label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            return button;
        }
    }
}
