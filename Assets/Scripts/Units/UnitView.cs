using System.Collections;
using Grid;
using UnityEngine;

namespace Units
{
    [RequireComponent(typeof(Unit))]
    public class UnitView : MonoBehaviour
    {
        public GridCoordinateSystem coords;
        private SpriteRenderer _sr;
        private Unit _unit;
        private Color _baseColor = Color.white;
        private Coroutine _damageRoutine;

        [Header("Movement")] public bool smoothMove = true;
        public float moveSpeed = 8f;

        [Header("Damage Feedback")] [SerializeField] private Color damageFlashColor = new Color(1f, 0.32f, 0.25f, 1f);
        [SerializeField] private float damageFlashDuration = 0.18f;
        [SerializeField] private float damageShakeDistance = 0.08f;
        [SerializeField] private int damageShakeSteps = 4;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            _unit = GetComponent<Unit>();
            _sr = GetComponent<SpriteRenderer>();

            if (_sr != null)
            {
                _baseColor = Color.white;
                _sr.color = _baseColor;
                if (!_unit.isPlayer)
                {
                    _sr.flipX = true;
                }
            }

            SnapToGrid();
        }

        private void Update()
        {
            if (coords == null) return;

            Vector3 targetPos = coords.GridToWorld(_unit.gridPos);

            if (smoothMove)
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            else
                transform.position = targetPos;
            if (_sr != null)
                _sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }

        public void SnapToGrid()
        {
            if (coords == null) return;
            transform.position = coords.GridToWorld(_unit.gridPos);
        }

        public void PlayDamageFeedback(bool hideAfter)
        {
            if (_damageRoutine != null)
                StopCoroutine(_damageRoutine);

            _damageRoutine = StartCoroutine(DamageFeedbackRoutine(hideAfter));
        }

        private IEnumerator DamageFeedbackRoutine(bool hideAfter)
        {
            if (_sr == null)
            {
                if (hideAfter)
                    gameObject.SetActive(false);
                yield break;
            }

            Vector3 startLocalPosition = transform.localPosition;
            float stepDuration = damageFlashDuration / Mathf.Max(1, damageShakeSteps);

            _sr.color = damageFlashColor;
            for (int i = 0; i < damageShakeSteps; i++)
            {
                float direction = i % 2 == 0 ? 1f : -1f;
                transform.localPosition = startLocalPosition + new Vector3(direction * damageShakeDistance, 0f, 0f);
                yield return new WaitForSeconds(stepDuration);
            }

            transform.localPosition = startLocalPosition;
            _sr.color = _baseColor;
            _damageRoutine = null;

            if (hideAfter)
                gameObject.SetActive(false);
        }
    }
}
