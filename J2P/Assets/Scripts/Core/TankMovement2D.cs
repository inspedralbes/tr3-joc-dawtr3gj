using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TankMovement2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
        [SerializeField, Min(1f)] private float acceleration = 30f;
        [SerializeField, Min(1f)] private float deceleration = 35f;
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField, Min(0f)] private float clampPadding = 0.45f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private float externalSpeedMultiplier = 1f;

        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public Vector2 DesiredMove => moveInput;
        public float MoveSpeed => moveSpeed;
        public float EffectiveMoveSpeed => moveSpeed * externalSpeedMultiplier;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void Configure(float speed, float accel, float decel, ArenaBounds bounds, float padding)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
            acceleration = Mathf.Max(1f, accel);
            deceleration = Mathf.Max(1f, decel);
            arenaBounds = bounds;
            clampPadding = Mathf.Max(0f, padding);
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void StopImmediate()
        {
            moveInput = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void SetExternalSpeedMultiplier(float multiplier)
        {
            externalSpeedMultiplier = Mathf.Max(0.01f, multiplier);
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }

            float rate = moveInput.sqrMagnitude > 0.001f ? acceleration : deceleration;
            Vector2 targetVelocity = moveInput * EffectiveMoveSpeed;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            if (arenaBounds != null)
            {
                rb.position = arenaBounds.ClampInside(rb.position, clampPadding);
            }
        }
    }
}
