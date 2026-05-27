using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CombatLogUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private TMP_Text logEntryPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int maxEntries = 80;
        [SerializeField] private bool createFreshScrollViewOnAwake = true;
        [SerializeField] private int logFontSize = 15;

        private static readonly Vector2 LogWindowSize = new Vector2(460f, 240f);

        public static CombatLogUI EnsureInScene()
        {
            CombatLogUI existing = FindAnyObjectByType<CombatLogUI>();
            if (existing != null)
                return existing;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            GameObject logObject = new GameObject(
                "Battle Logs",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CombatLogUI));
            logObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = logObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 24f);
            rect.sizeDelta = LogWindowSize;

            return logObject.GetComponent<CombatLogUI>();
        }

        private void Awake()
        {
            if (createFreshScrollViewOnAwake)
            {
                EnsureReadableRootRect();
                CreateFreshScrollView();
            }
        }

        public void AddLog(string message)
        {
            if (content == null || logEntryPrefab == null)
            {
                EnsureReadableRootRect();
                CreateFreshScrollView();
            }

            if (content == null || logEntryPrefab == null)
            {
                Debug.LogWarning($"CombatLogUI is missing references. Message: {message}");
                return;
            }

            TMP_Text entry = Instantiate(logEntryPrefab, content);
            entry.gameObject.SetActive(true);
            entry.text = message;

            if (content.childCount > maxEntries)
            {
                Destroy(content.GetChild(0).gameObject);
            }

            Canvas.ForceUpdateCanvases();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        public void AddLog(string attacker, string target, int damage)
        {
            AddLog($"{attacker} zadal {damage} obrazen: {target}");
        }

        private void CreateFreshScrollView()
        {
            HideOldDirectTextChildren();

            Transform existing = transform.Find("CombatLogScrollView");
            if (existing != null)
            {
                scrollRect = existing.GetComponent<ScrollRect>();
                content = existing.Find("Viewport/Content");
                Transform template = existing.Find("LogEntryPrefab");
                logEntryPrefab = template != null ? template.GetComponent<TMP_Text>() : null;
                if (scrollRect != null && content != null && logEntryPrefab != null)
                    return;
            }

            GameObject scrollView = new GameObject(
                "CombatLogScrollView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ScrollRect));
            scrollView.transform.SetParent(transform, false);
            Stretch(scrollView.GetComponent<RectTransform>());

            Image background = scrollView.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D));
            viewport.transform.SetParent(scrollView.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 6f);

            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;
            layout.padding = new RectOffset(6, 6, 6, 6);

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject entryPrefabObject = new GameObject(
                "LogEntryPrefab",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            entryPrefabObject.transform.SetParent(scrollView.transform, false);

            RectTransform entryRect = entryPrefabObject.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0f, 1f);
            entryRect.anchorMax = new Vector2(1f, 1f);
            entryRect.pivot = new Vector2(0f, 1f);
            entryRect.sizeDelta = new Vector2(0f, 22f);

            TextMeshProUGUI entryText = entryPrefabObject.GetComponent<TextMeshProUGUI>();
            entryText.text = string.Empty;
            entryText.fontSize = logFontSize;
            entryText.color = Color.white;
            entryText.alignment = TextAlignmentOptions.TopLeft;
            entryText.textWrappingMode = TextWrappingModes.Normal;
            entryText.raycastTarget = false;

            LayoutElement layoutElement = entryPrefabObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 20f;

            entryPrefabObject.SetActive(false);

            scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;

            content = contentObject.transform;
            logEntryPrefab = entryText;
        }

        private void EnsureReadableRootRect()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;

            bool tooSmall = root.rect.width < LogWindowSize.x || root.rect.height < LogWindowSize.y;
            if (!tooSmall) return;

            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = new Vector2(-24f, 24f);
            root.sizeDelta = LogWindowSize;
        }

        private void HideOldDirectTextChildren()
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<TMP_Text>() != null)
                    child.gameObject.SetActive(false);
            }
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
