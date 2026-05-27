using UnityEngine;

namespace Units
{
    [RequireComponent(typeof(Unit))]
    public class UnitHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.72f, 0f);
        [SerializeField] private Vector2 size = new Vector2(0.72f, 0.08f);
        [SerializeField] private Color playerFillColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color enemyFillColor = new Color(0.9f, 0.2f, 0.18f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        private static Sprite _pixelSprite;

        private Unit _unit;
        private SpriteRenderer _unitRenderer;
        private SpriteRenderer _backgroundRenderer;
        private SpriteRenderer _fillRenderer;
        private int _lastHp = -1;
        private int _lastMaxHp = -1;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _unitRenderer = GetComponent<SpriteRenderer>();
            EnsureSprite();
            CreateRenderers();
        }

        private void LateUpdate()
        {
            if (_unit == null || _unit.classData == null)
                return;

            SyncSorting();

            int maxHp = Mathf.Max(1, _unit.classData.maxHp);
            if (_lastHp == _unit.hp && _lastMaxHp == maxHp)
                return;

            _lastHp = _unit.hp;
            _lastMaxHp = maxHp;
            UpdateFill(maxHp);
        }

        private void CreateRenderers()
        {
            _backgroundRenderer = CreatePart("HealthBar_Background", backgroundColor, 100);
            _backgroundRenderer.transform.localPosition = offset;
            _backgroundRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);

            _fillRenderer = CreatePart("HealthBar_Fill", Color.white, 101);
            _fillRenderer.transform.localPosition = offset;
            _fillRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private SpriteRenderer CreatePart(string objectName, Color color, int sortingOrder)
        {
            var part = new GameObject(objectName);
            part.transform.SetParent(transform, false);

            var spriteRenderer = part.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _pixelSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            if (_unitRenderer != null)
                spriteRenderer.sortingLayerID = _unitRenderer.sortingLayerID;

            return spriteRenderer;
        }

        private void UpdateFill(int maxHp)
        {
            float ratio = Mathf.Clamp01((float)_unit.hp / maxHp);
            _fillRenderer.color = _unit.isPlayer ? playerFillColor : enemyFillColor;
            _fillRenderer.transform.localScale = new Vector3(size.x * ratio, size.y, 1f);
            _fillRenderer.transform.localPosition =
                offset + new Vector3((-size.x + size.x * ratio) * 0.5f, 0f, 0f);
        }

        private void SyncSorting()
        {
            if (_unitRenderer == null || _backgroundRenderer == null || _fillRenderer == null)
                return;

            _backgroundRenderer.sortingLayerID = _unitRenderer.sortingLayerID;
            _fillRenderer.sortingLayerID = _unitRenderer.sortingLayerID;
            _backgroundRenderer.sortingOrder = _unitRenderer.sortingOrder + 100;
            _fillRenderer.sortingOrder = _unitRenderer.sortingOrder + 101;
        }

        private static void EnsureSprite()
        {
            if (_pixelSprite != null)
                return;

            var texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
