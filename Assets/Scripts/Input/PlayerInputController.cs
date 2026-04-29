using Grid;
using Turns;
using UnityEngine;

namespace Input
{
    public class PlayerInputController : MonoBehaviour
    {
        [Header("Refs")] public Camera cam;
        public GridManager grid;
        public GridCoordinateSystem coords;
        public TurnManager turnManager;

        private void Reset()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null || turnManager == null || grid == null || coords == null) return;

            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                Debug.Log("[INPUT] PPM -> skip");
                turnManager.PlayerSkipCurrentStep();
                return;
            }

            if (!UnityEngine.Input.GetMouseButtonDown(0)) return;

            Vector3 world = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            world.z = 0f;

            Vector2Int gridPos = coords.WorldToGrid(world);

            Debug.Log($"[INPUT] LPM world={world} gridPos={gridPos}");

            bool inBounds = GridManager.InBounds(gridPos);
            Debug.Log($"[INPUT] inBounds={inBounds}");

            if (!inBounds) return;

            Debug.Log("[INPUT] calling TurnManager.PlayerClickGrid");
            turnManager.PlayerClickGrid(gridPos);
        }
    }
}