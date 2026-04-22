using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankArena2D
{
    public enum PerceptionHitType
    {
        None,
        Target,
        Obstacle,
        Boundary,
        OtherActor
    }

    public sealed class TankPerception2D : MonoBehaviour
    {
        [Serializable]
        public struct RaySample
        {
            [SerializeField] private Vector2 direction;
            [SerializeField] private float normalizedDistance;
            [SerializeField] private PerceptionHitType hitType;
            [SerializeField] private Vector2 hitPoint;
            [SerializeField] private Collider2D hitCollider;

            public RaySample(Vector2 directionValue, float normalizedDistanceValue, PerceptionHitType hitTypeValue, Vector2 hitPointValue, Collider2D hitColliderValue)
            {
                direction = directionValue;
                normalizedDistance = normalizedDistanceValue;
                hitType = hitTypeValue;
                hitPoint = hitPointValue;
                hitCollider = hitColliderValue;
            }

            public Vector2 Direction => direction;
            public float NormalizedDistance => normalizedDistance;
            public PerceptionHitType HitType => hitType;
            public Vector2 HitPoint => hitPoint;
            public Collider2D HitCollider => hitCollider;
        }

        [SerializeField] private Transform rayOrigin;
        [SerializeField, Min(4)] private int rayCount = 16;
        [SerializeField, Min(1f)] private float rayDistance = 14f;
        [SerializeField, Min(1f)] private float targetDetectionRange = 28f;
        [SerializeField, Range(0f, 360f)] private float startingAngle = 0f;
        [SerializeField] private bool debugDraw = true;
        [SerializeField] private Color clearColor = new Color(0.3f, 0.9f, 1f, 0.55f);
        [SerializeField] private Color targetColor = new Color(0.2f, 1f, 0.45f, 0.95f);
        [SerializeField] private Color obstacleColor = new Color(1f, 0.42f, 0.25f, 0.95f);
        [SerializeField] private Color boundaryColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        [SerializeField] private Color actorColor = new Color(0.25f, 0.55f, 1f, 0.95f);

        private Collider2D[] selfColliders = Array.Empty<Collider2D>();
        private RaySample[] raySamples = Array.Empty<RaySample>();
        private float lastSeenTime = float.NegativeInfinity;

        public IReadOnlyList<RaySample> RaySamples => raySamples;
        public int RayCount => raySamples != null ? raySamples.Length : 0;
        public float RayDistance => rayDistance;
        public float DetectionRange => targetDetectionRange;
        public Vector2 Origin => rayOrigin != null ? rayOrigin.position : transform.position;
        public bool TargetDetected { get; private set; }
        public bool HasLineOfSight { get; private set; }
        public float TargetDistance { get; private set; }
        public Vector2 TargetDirection { get; private set; }
        public Vector2 LastKnownTargetPosition { get; private set; }
        public bool HasLastKnownTarget => lastSeenTime > float.NegativeInfinity;
        public float TimeSinceLastSeen => HasLastKnownTarget ? Time.time - lastSeenTime : Mathf.Infinity;

        private void Awake()
        {
            CacheSelfColliders();
            EnsureSampleBuffer();
        }

        public void Configure(Transform origin, int newRayCount, float newRayDistance, float newDetectionRange, bool showDebug)
        {
            rayOrigin = origin;
            rayCount = Mathf.Max(4, newRayCount);
            rayDistance = Mathf.Max(1f, newRayDistance);
            targetDetectionRange = Mathf.Max(1f, newDetectionRange);
            debugDraw = showDebug;
            EnsureSampleBuffer();
            CacheSelfColliders();
        }

        public void Scan(Transform target)
        {
            EnsureSampleBuffer();

            Vector2 origin = Origin;
            float stepAngle = 360f / rayCount;

            for (int index = 0; index < raySamples.Length; index++)
            {
                float angle = startingAngle + stepAngle * index;
                Vector2 direction = AngleToDirection(angle);
                raySamples[index] = BuildSample(origin, direction, target);
                DrawSample(origin, raySamples[index]);
            }

            UpdateTargetInfo(origin, target);
        }

        public Vector2 GetBestDirection(Vector2 desiredDirection)
        {
            EnsureSampleBuffer();

            if (raySamples.Length == 0)
            {
                return desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : Vector2.zero;
            }

            Vector2 desired = desiredDirection.sqrMagnitude > 0.0001f
                ? desiredDirection.normalized
                : Vector2.zero;

            if (desired.sqrMagnitude > 0.0001f && GetNormalizedClearance(desired) > 0.55f)
            {
                return desired;
            }

            Vector2 bestDirection = desired.sqrMagnitude > 0.0001f ? desired : raySamples[0].Direction;
            float bestScore = float.NegativeInfinity;

            for (int index = 0; index < raySamples.Length; index++)
            {
                RaySample sample = raySamples[index];
                float alignment = desired.sqrMagnitude > 0.0001f ? Vector2.Dot(desired, sample.Direction) : 0.2f;
                float score = sample.NormalizedDistance * 1.25f + alignment * 0.9f - GetTypePenalty(sample.HitType);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = sample.Direction;
                }
            }

            return bestDirection.normalized;
        }

        public float GetNormalizedClearance(Vector2 direction, bool ignoreTarget = true)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return 1f;
            }

            RaycastHit2D hit = FindNearestHit(Origin, direction.normalized, rayDistance, null, ignoreTarget);

            if (hit.collider == null)
            {
                return 1f;
            }

            return Mathf.Clamp01(hit.distance / rayDistance);
        }

        public bool HasDirectLineOfSight(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Vector2 origin = Origin;
            Vector2 toTarget = (Vector2)target.position - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.01f || distance > targetDetectionRange)
            {
                return distance <= 0.01f;
            }

            RaycastHit2D hit = FindNearestHit(origin, toTarget / distance, distance, target, false);

            if (hit.collider == null)
            {
                return true;
            }

            return BelongsToTransform(hit.collider, target);
        }

        private void UpdateTargetInfo(Vector2 origin, Transform target)
        {
            TargetDetected = false;
            HasLineOfSight = false;
            TargetDistance = float.PositiveInfinity;
            TargetDirection = Vector2.zero;

            if (target == null)
            {
                return;
            }

            Vector2 toTarget = (Vector2)target.position - origin;
            float distance = toTarget.magnitude;

            TargetDistance = distance;
            TargetDirection = distance > 0.001f ? toTarget / distance : Vector2.zero;

            if (distance > targetDetectionRange)
            {
                return;
            }

            HasLineOfSight = HasDirectLineOfSight(target);
            TargetDetected = HasLineOfSight;

            if (TargetDetected)
            {
                LastKnownTargetPosition = target.position;
                lastSeenTime = Time.time;
            }
        }

        private RaySample BuildSample(Vector2 origin, Vector2 direction, Transform target)
        {
            RaycastHit2D hit = FindNearestHit(origin, direction, rayDistance, target, false);

            if (hit.collider == null)
            {
                return new RaySample(direction, 1f, PerceptionHitType.None, origin + direction * rayDistance, null);
            }

            float normalizedDistance = Mathf.Clamp01(hit.distance / rayDistance);
            PerceptionHitType hitType = ClassifyCollider(hit.collider, target);
            Vector2 hitPoint = hit.point == Vector2.zero ? origin + direction * hit.distance : hit.point;

            return new RaySample(direction, normalizedDistance, hitType, hitPoint, hit.collider);
        }

        private RaycastHit2D FindNearestHit(Vector2 origin, Vector2 direction, float distance, Transform target, bool ignoreTarget)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int index = 0; index < hits.Length; index++)
            {
                Collider2D hitCollider = hits[index].collider;

                if (hitCollider == null || hitCollider.isTrigger || BelongsToSelf(hitCollider))
                {
                    continue;
                }

                if (ignoreTarget && target != null && BelongsToTransform(hitCollider, target))
                {
                    continue;
                }

                return hits[index];
            }

            return default;
        }

        private PerceptionHitType ClassifyCollider(Collider2D hitCollider, Transform target)
        {
            if (hitCollider == null)
            {
                return PerceptionHitType.None;
            }

            if (target != null && BelongsToTransform(hitCollider, target))
            {
                return PerceptionHitType.Target;
            }

            ArenaObstacle obstacle = hitCollider.GetComponentInParent<ArenaObstacle>();

            if (obstacle != null)
            {
                return obstacle.ObstacleType == ArenaObstacleType.Boundary
                    ? PerceptionHitType.Boundary
                    : PerceptionHitType.Obstacle;
            }

            if (hitCollider.GetComponentInParent<Health>() != null || hitCollider.GetComponentInParent<FactionMember>() != null)
            {
                return PerceptionHitType.OtherActor;
            }

            return PerceptionHitType.Obstacle;
        }

        private bool BelongsToSelf(Collider2D hitCollider)
        {
            for (int index = 0; index < selfColliders.Length; index++)
            {
                if (selfColliders[index] == hitCollider)
                {
                    return true;
                }
            }

            return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
        }

        private static bool BelongsToTransform(Collider2D hitCollider, Transform targetTransform)
        {
            return hitCollider != null &&
                   targetTransform != null &&
                   (hitCollider.transform == targetTransform || hitCollider.transform.IsChildOf(targetTransform));
        }

        private void CacheSelfColliders()
        {
            selfColliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void EnsureSampleBuffer()
        {
            int safeRayCount = Mathf.Max(4, rayCount);

            if (raySamples == null || raySamples.Length != safeRayCount)
            {
                raySamples = new RaySample[safeRayCount];
            }
        }

        private void DrawSample(Vector2 origin, RaySample sample)
        {
            if (!debugDraw)
            {
                return;
            }

            Debug.DrawLine(origin, sample.HitPoint, GetColor(sample.HitType));
        }

        private Color GetColor(PerceptionHitType hitType)
        {
            return hitType switch
            {
                PerceptionHitType.Target => targetColor,
                PerceptionHitType.Obstacle => obstacleColor,
                PerceptionHitType.Boundary => boundaryColor,
                PerceptionHitType.OtherActor => actorColor,
                _ => clearColor
            };
        }

        private static float GetTypePenalty(PerceptionHitType hitType)
        {
            return hitType switch
            {
                PerceptionHitType.Obstacle => 0.55f,
                PerceptionHitType.Boundary => 0.75f,
                PerceptionHitType.OtherActor => 0.2f,
                _ => 0f
            };
        }

        private static Vector2 AngleToDirection(float angleInDegrees)
        {
            float radians = angleInDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }

        private void OnValidate()
        {
            rayCount = Mathf.Max(4, rayCount);
            rayDistance = Mathf.Max(1f, rayDistance);
            targetDetectionRange = Mathf.Max(1f, targetDetectionRange);
            EnsureSampleBuffer();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || raySamples == null)
            {
                return;
            }

            Vector2 origin = Origin;

            for (int index = 0; index < raySamples.Length; index++)
            {
                RaySample sample = raySamples[index];
                Gizmos.color = GetColor(sample.HitType);
                Gizmos.DrawLine(origin, sample.HitPoint);
                Gizmos.DrawSphere(sample.HitPoint, 0.08f);
            }
        }
#endif
    }
}
