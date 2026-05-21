using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class FitSpriteToCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private SpriteRenderer _spriteRenderer;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private float _lastCameraSize;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Start()
        {
            FitToCamera();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || _spriteRenderer == null)
                return;

            if (_lastScreenWidth == Screen.width &&
                _lastScreenHeight == Screen.height &&
                Mathf.Approximately(_lastCameraSize, targetCamera.orthographicSize))
                return;

            FitToCamera();
        }

        private void FitToCamera()
        {
            if (targetCamera == null || _spriteRenderer == null || _spriteRenderer.sprite == null)
                return;

            float cameraHeight = targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * targetCamera.aspect;
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 cameraPosition = targetCamera.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastCameraSize = targetCamera.orthographicSize;
        }
    }
}
