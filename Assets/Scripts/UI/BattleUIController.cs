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

        private static readonly Color ButtonBackgroundColor = new Color(0.18f, 0.15f, 0.12f, 0.92f);
        private static readonly Color UnitPanelFrameColor = new Color(0.25f, 0.18f, 0.15f, 0.95f);
        private static readonly Color UnitPanelBorderColor = new Color(0.25f, 0.18f, 0.15f, 1f);
        private static readonly Vector2 StatusTextPadding = new Vector2(10f, 6f);
        private static readonly Vector2 UnitPanelFramePadding = new Vector2(8f, 5f);

        private void Start()
        {
            EnsureRuntimeUi();
            ApplyStatusTextStyle(turnText, TextAlignmentOptions.Center);
            ApplyStatusTextStyle(phaseText, TextAlignmentOptions.Center);
            ApplyStatusTextStyle(turnOrderText, TextAlignmentOptions.TopRight);
            HideStanceDisplay();
            EnsureActiveUnitFrames();

            SetButtonLabel(moveButton, "Ruch");
            SetButtonLabel(attackButton, "Atak");
            SetButtonLabel(endTurnButton, "Koniec tury");
            SetButtonLabel(pauseButton, "Pauza");
            SetButtonLabel(resumeButton, "Wznów");
            SetButtonLabel(mainMenuButton, "Menu główne");
            ApplyPauseOverlayLabels();

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

            UpdateUnitPanel(u);
            UpdateButtons(u, st);
            UpdateTurnStatusText(u, st);
            UpdateTurnOrderText();
        }
        
        private void UpdateUnitPanel(Unit u)
        {
            if (u == null)
            {
                SetEmptyPanel();
                return;
            }

            if (unitNameText != null) unitNameText.text = UnitDisplayNames.UnitName(u);
            if (unitHpText != null) unitHpText.text = $"HP: {u.hp}/{u.classData.maxHp}";
            if (unitHpSlider != null)
                unitHpSlider.value = u.classData.maxHp <= 0 ? 0f : (float)u.hp / u.classData.maxHp;
            if (unitPortrait != null)
                unitPortrait.sprite = u.classData?.portrait != null ? u.classData.portrait : defaultPortrait;
        }

        private void SetEmptyPanel()
        {
            if (unitNameText != null) unitNameText.text = "-";
            if (unitHpText != null) unitHpText.text = "HP: -";
            if (unitHpSlider != null) unitHpSlider.value = 0f;
            if (unitPortrait != null) unitPortrait.sprite = defaultPortrait;
        }

        private void UpdateButtons(Unit u, BattleState st)
        {
            bool isPlayer = u != null && u.isPlayer;
            bool choosingAction = st == BattleState.PlayerChooseAction;
            bool choosingMove = st == BattleState.PlayerChooseMove;
            bool choosingAttack = st == BattleState.PlayerChooseAttackTarget;
            bool playerCanAct = isPlayer && (choosingAction || choosingMove || choosingAttack);

            if (moveButton != null) moveButton.interactable = turnManager.CanPlayerChooseMove;
            if (attackButton != null) attackButton.interactable = turnManager.CanPlayerChooseAttack;
            if (endTurnButton != null) endTurnButton.interactable = playerCanAct;
        }

        private void UpdateTurnOrderText()
        {
            if (turnOrderText == null || turnManager == null) return;

            var order = turnManager.CurrentTurnOrder;
            if (order == null || order.Count == 0)
            {
                turnOrderText.text = "Kolejność tur:\n-";
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder("Kolejność tur:");
            for (int i = 0; i < order.Count; i++)
            {
                Unit unit = order[i];
                string marker = i == 0 ? ">" : "-";
                builder.AppendLine();
                builder.Append($"{marker} {UnitDisplayNames.UnitName(unit)}");
            }

            turnOrderText.text = builder.ToString();
        }

        private void UpdateTurnStatusText(Unit activeUnit, BattleState state)
        {
            if (turnText != null)
                turnText.text = BuildTurnText(activeUnit, state);

            if (phaseText != null)
                phaseText.text = BuildInstructionText(activeUnit, state);
        }

        private static string BuildTurnText(Unit activeUnit, BattleState state)
        {
            if (activeUnit == null)
                return "Tura: przygotowanie";


            string owner = activeUnit.isPlayer ? "Gracz" : "Przeciwnik";
            return $"Tura: {owner} - {UnitDisplayNames.UnitName(activeUnit)}";
        }

        private static string BuildInstructionText(Unit activeUnit, BattleState state)
        {
            if (activeUnit == null)
                return "Przygotowanie następnej tury";

            if (!activeUnit.isPlayer)
                return "Ruch przeciwnika";

            switch (state)
            {
                case BattleState.PlayerChooseAction:
                    return "Wybierz akcję";
                case BattleState.PlayerChooseMove:
                    return "Wybierz pole do ruchu";
                case BattleState.PlayerChooseAttackTarget:
                    return "Wybierz przeciwnika, którego chcesz zaatakować";
                default:
                    return "Przygotowanie następnej tury";
            }
        }

        private void EnsureRuntimeUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            if (turnText == null)
                turnText = CreateText(
                    parent,
                    "CurrentTurnText",
                    "Tura: przygotowanie",
                    new RectLayout(
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -18f),
                        new Vector2(420f, 34f)),
                    20,
                    TextAlignmentOptions.Center);

            if (phaseText == null)
                phaseText = CreateText(
                    parent,
                    "TurnInstructionText",
                    "Przygotowanie następnej tury",
                    new RectLayout(
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -58f),
                        new Vector2(560f, 34f)),
                    18,
                    TextAlignmentOptions.Center);

            if (turnOrderText == null)
                turnOrderText = CreateText(
                    parent,
                    "TurnOrderText",
                    "Kolejność tur:",
                    new RectLayout(
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-18f, -72f),
                        new Vector2(290f, 190f)),
                    18,
                    TextAlignmentOptions.TopRight);

            if (pauseButton == null)
                pauseButton = CreateButton(
                    parent,
                    "PauseButton",
                    "Pauza",
                    new RectLayout(
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(18f, -18f),
                        new Vector2(120f, 42f)));

            CreatePauseOverlay(parent);
        }

        private static void ApplyStatusTextStyle(TMP_Text label, TextAlignmentOptions alignment)
        {
            if (label == null) return;

            label.gameObject.SetActive(true);
            label.color = Color.white;
            label.fontSize = 18;
            label.alignment = alignment;
            label.margin = new Vector4(StatusTextPadding.x, StatusTextPadding.y, StatusTextPadding.x, StatusTextPadding.y);
            label.raycastTarget = false;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect == null || labelRect.parent == null) return;

            string backgroundName = $"{label.name}_Background";
            Transform existingBackground = labelRect.parent.Find(backgroundName);
            GameObject backgroundObject = existingBackground != null
                ? existingBackground.gameObject
                : new GameObject(backgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            backgroundObject.transform.SetParent(labelRect.parent, false);
            backgroundObject.transform.SetSiblingIndex(labelRect.GetSiblingIndex());

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = labelRect.anchorMin;
            backgroundRect.anchorMax = labelRect.anchorMax;
            backgroundRect.pivot = labelRect.pivot;
            backgroundRect.anchoredPosition = labelRect.anchoredPosition;
            backgroundRect.sizeDelta = labelRect.sizeDelta + (StatusTextPadding * 2f);
            backgroundRect.localScale = labelRect.localScale;
            backgroundRect.localRotation = labelRect.localRotation;

            Image background = backgroundObject.GetComponent<Image>();
            backgroundObject.SetActive(true);
            background.color = ButtonBackgroundColor;
            background.raycastTarget = false;
        }

        private void HideStanceDisplay()
        {
            if (stanceText != null)
                stanceText.gameObject.SetActive(false);
        }

        private void EnsureActiveUnitFrames()
        {
            EnsureFrame(unitPortrait != null ? unitPortrait.rectTransform : null, "UnitPortraitFrame", UnitPanelFramePadding);
            RemoveFrame(unitNameText != null ? unitNameText.transform.parent : null, "UnitNameTextFrame");
            RemoveFrame(unitHpText != null ? unitHpText.transform.parent : null, "UnitHpTextFrame");
            RemoveFrame(unitHpSlider != null ? unitHpSlider.transform.parent : null, "UnitHpSliderFrame");
        }

        private static void EnsureFrame(RectTransform target, string frameName, Vector2 padding)
        {
            if (target == null || target.parent == null) return;

            Transform existingFrame = target.parent.Find(frameName);
            GameObject frameObject = existingFrame != null
                ? existingFrame.gameObject
                : new GameObject(frameName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));

            frameObject.transform.SetParent(target.parent, false);
            frameObject.transform.SetSiblingIndex(target.GetSiblingIndex());

            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = target.anchorMin;
            frameRect.anchorMax = target.anchorMax;
            frameRect.pivot = target.pivot;
            frameRect.anchoredPosition = target.anchoredPosition;
            frameRect.sizeDelta = target.sizeDelta + (padding * 2f);
            frameRect.localScale = target.localScale;
            frameRect.localRotation = target.localRotation;

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = UnitPanelFrameColor;
            frameImage.raycastTarget = false;

            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectColor = UnitPanelBorderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        private static void RemoveFrame(Transform parent, string frameName)
        {
            if (parent == null) return;

            Transform frame = parent.Find(frameName);
            if (frame != null)
                Destroy(frame.gameObject);
        }

        private void CreatePauseOverlay(Transform parent)
        {
            if (pauseOverlay != null)
            {
                ApplyPauseOverlayLabels();
                return;
            }

            pauseOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pauseOverlay.transform.SetParent(parent, false);
            RectTransform overlayRect = pauseOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            pauseOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            CreateText(
                pauseOverlay.transform,
                "PauseTitle",
                "Pauza",
                new RectLayout(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 82f),
                    new Vector2(300f, 54f)),
                36,
                TextAlignmentOptions.Center);
            resumeButton = CreateButton(
                pauseOverlay.transform,
                "ResumeButton",
                "Wznów",
                new RectLayout(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 10f),
                    new Vector2(210f, 46f)));
            mainMenuButton = CreateButton(
                pauseOverlay.transform,
                "MainMenuButton",
                "Menu główne",
                new RectLayout(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -48f),
                    new Vector2(210f, 46f)));

            pauseOverlay.SetActive(false);
        }

        private void ApplyPauseOverlayLabels()
        {
            SetButtonLabel(resumeButton, "Wznów");
            SetButtonLabel(mainMenuButton, "Menu główne");

            if (pauseOverlay == null) return;

            TMP_Text[] labels = pauseOverlay.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                if (label.name == "PauseTitle")
                    label.text = "Pauza";
            }
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

        private static void ReturnToMainMenu()
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

        private static TMP_Text CreateText(Transform parent, string name, string text, RectLayout layout,
            int fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.pivot = layout.Pivot;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.Size;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, RectLayout layout)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.pivot = layout.Pivot;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.Size;

            Image image = go.GetComponent<Image>();
            image.color = ButtonBackgroundColor;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.24f, 0.18f, 1f);
            colors.pressedColor = new Color(0.10f, 0.08f, 0.06f, 1f);
            colors.disabledColor = new Color(0.12f, 0.11f, 0.10f, 0.55f);
            button.colors = colors;

            TMP_Text text = CreateText(
                go.transform,
                "Text",
                label,
                new RectLayout(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
                18,
                TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            return button;
        }

        private sealed class RectLayout
        {
            public RectLayout(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition,
                Vector2 size)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                Pivot = pivot;
                AnchoredPosition = anchoredPosition;
                Size = size;
            }

            public Vector2 AnchorMin { get; }
            public Vector2 AnchorMax { get; }
            public Vector2 Pivot { get; }
            public Vector2 AnchoredPosition { get; }
            public Vector2 Size { get; }
        }
    }
}
