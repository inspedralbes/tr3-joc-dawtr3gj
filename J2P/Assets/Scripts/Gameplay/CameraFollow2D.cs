using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.14f;

        private Camera cachedCamera;
        private Vector3 velocity;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            cachedCamera.orthographic = true;
        }

        public void Configure(Transform followTarget, ArenaBounds bounds, float cameraSize, float followSmoothTime)
        {
            target = followTarget;
            arenaBounds = bounds;
            smoothTime = Mathf.Max(0f, followSmoothTime);

            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            cachedCamera.orthographic = true;
            cachedCamera.orthographicSize = cameraSize;
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            desiredPosition.z = offset.z;
            desiredPosition = ClampToArena(desiredPosition);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }

        private Vector3 ClampToArena(Vector3 position)
        {
            if (cachedCamera == null || arenaBounds == null)
            {
                return position;
            }

            Rect rect = arenaBounds.InnerRect;
            float verticalExtent = cachedCamera.orthographicSize;
            float horizontalExtent = verticalExtent * cachedCamera.aspect;

            float minX = rect.xMin + horizontalExtent;
            float maxX = rect.xMax - horizontalExtent;
            float minY = rect.yMin + verticalExtent;
            float maxY = rect.yMax - verticalExtent;

            if (minX > maxX)
            {
                minX = maxX = arenaBounds.Center.x;
            }

            if (minY > maxY)
            {
                minY = maxY = arenaBounds.Center.y;
            }

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }
    }
}
