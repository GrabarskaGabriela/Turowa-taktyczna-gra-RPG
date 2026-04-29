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

        [Header("Movement")] public bool smoothMove = true;
        public float moveSpeed = 8f;

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
                _sr.color = Color.white;
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
    }
}