using System.Collections.Generic;
using UnityEngine;

public class ItanoCircusHandler : MonoBehaviour
{
    public static ItanoCircusHandler Instance;

    private struct ItanoMissile
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 CurrentVelocity;
        public float CurrentSpeed;
        public Vector3 BaseDir;

        public Transform Target;
        public int Damage;
        public float MaxSpeed;
        public float Acceleration;
        public float TurnSpeed;
        public float ExplosionRadius;
        public float HomingDelay;

        public float ZigzagIntensity;
        public float ZigzagFrequency;
        public float SnapSharpness;

        public float TimeAlive;
        public float TimeSinceLastSnap;
        public float CurrentZigzagInterval;
        public Vector2 CurrentZigzagDir;

        public bool HasOvershot;
        public float ClosestDistance;

        public GameObject VisualPrefab;
        public GameObject HitVfx;
    }

    private List<ItanoMissile> activeMissiles = new List<ItanoMissile>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnMissile(Vector3 startPos, Quaternion startRot, Transform target, int damage, float maxSpeed, float accel, float ejectSpeed, float spread, float upwardBias, float delay, float turnSpd, float zigInt, float zigFreq, float snapSharp, float expRad, GameObject visualObj, GameObject hitVfx)
    {
        Vector3 spreadDir = Quaternion.Euler(
            Random.Range(-spread, spread) * 0.5f,
            Random.Range(-spread, spread),
            0
        ) * (startRot * Vector3.forward);

        spreadDir += (startRot * Vector3.up) * Random.Range(upwardBias * 0.5f, upwardBias);
        spreadDir += (startRot * Vector3.right) * Random.Range(-upwardBias * 0.5f, upwardBias * 0.5f);
        spreadDir.Normalize();

        GameObject visual = null;
        if (visualObj != null)
        {
            visual = Instantiate(visualObj, startPos, Quaternion.LookRotation(spreadDir));
        }

        activeMissiles.Add(new ItanoMissile
        {
            Position = startPos,
            Rotation = Quaternion.LookRotation(spreadDir),
            CurrentVelocity = spreadDir * ejectSpeed,
            CurrentSpeed = ejectSpeed,
            BaseDir = spreadDir,
            Target = target,
            Damage = damage,
            MaxSpeed = maxSpeed,
            Acceleration = accel,
            TurnSpeed = turnSpd,
            ExplosionRadius = expRad,
            HomingDelay = delay,
            ZigzagIntensity = zigInt,
            ZigzagFrequency = zigFreq,
            SnapSharpness = snapSharp,
            TimeAlive = 0f,
            TimeSinceLastSnap = 0f,
            CurrentZigzagInterval = (1f / Mathf.Max(0.1f, zigFreq)) * Random.Range(0.8f, 1.2f),
            CurrentZigzagDir = Random.insideUnitCircle.normalized,
            HasOvershot = false,
            ClosestDistance = float.MaxValue,
            VisualPrefab = visual,
            HitVfx = hitVfx
        });
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = activeMissiles.Count - 1; i >= 0; i--)
        {
            ItanoMissile m = activeMissiles[i];
            m.TimeAlive += dt;

            if (m.TimeAlive > m.HomingDelay)
            {
                m.CurrentSpeed = Mathf.MoveTowards(m.CurrentSpeed, m.MaxSpeed, m.Acceleration * dt);

                float distToTarget = 100f;
                if (m.Target != null && m.Target.gameObject.activeInHierarchy)
                {
                    Vector3 targetCenter = m.Target.position + Vector3.up * 1f;
                    Vector3 dirToTarget = (targetCenter - m.Position).normalized;
                    distToTarget = Vector3.Distance(m.Position, targetCenter);

                    if (!m.HasOvershot)
                    {
                        m.ClosestDistance = Mathf.Min(m.ClosestDistance, distToTarget);

                        if (distToTarget < 15f && distToTarget > m.ClosestDistance + 1.5f)
                        {
                            m.HasOvershot = true; // Lock lost
                        }
                        else
                        {
                            m.BaseDir = Vector3.RotateTowards(m.BaseDir, dirToTarget, m.TurnSpeed * Mathf.Deg2Rad * dt, 0f);
                        }
                    }
                }

                m.TimeSinceLastSnap += dt;
                if (m.TimeSinceLastSnap >= m.CurrentZigzagInterval)
                {
                    m.CurrentZigzagDir = Random.insideUnitCircle.normalized;
                    m.TimeSinceLastSnap = 0f;
                    m.CurrentZigzagInterval = (1f / Mathf.Max(0.1f, m.ZigzagFrequency)) * Random.Range(0.5f, 1.5f);
                }

                Vector3 stableRight = Vector3.Cross(Vector3.up, m.BaseDir).normalized;
                if (stableRight == Vector3.zero) stableRight = Vector3.right;
                Vector3 stableUp = Vector3.Cross(m.BaseDir, stableRight).normalized;

                float distanceFactor = m.HasOvershot ? 1f : Mathf.Clamp01((distToTarget - 8f) / 17f);
                Vector3 zigzagOffset = (stableRight * m.CurrentZigzagDir.x + stableUp * m.CurrentZigzagDir.y) * (m.ZigzagIntensity * 0.25f * distanceFactor);
                Vector3 desiredDir = (m.BaseDir + zigzagOffset).normalized;
                float activeTurnSpeed = m.SnapSharpness * 50f;

                if (distToTarget < 12f && m.Target != null && !m.HasOvershot)
                {
                    Vector3 targetCenter = m.Target.position + Vector3.up * 1f;
                    desiredDir = (targetCenter - m.Position).normalized;
                    activeTurnSpeed = Mathf.Max(activeTurnSpeed, m.TurnSpeed * 5f);
                }

                m.Rotation = Quaternion.RotateTowards(m.Rotation, Quaternion.LookRotation(desiredDir), activeTurnSpeed * dt);
            }
            else
            {
                m.CurrentSpeed = Mathf.Lerp(m.CurrentSpeed, m.CurrentSpeed * 0.5f, dt * 3f);
            }

            m.CurrentVelocity = (m.Rotation * Vector3.forward) * m.CurrentSpeed;
            Vector3 nextPos = m.Position + (m.CurrentVelocity * dt);
            float distance = m.CurrentVelocity.magnitude * dt;

            if (Physics.Raycast(m.Position, m.Rotation * Vector3.forward, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit, m);
                DestroyMissile(i, m);
                continue;
            }

            m.Position = nextPos;
            if (m.VisualPrefab != null)
            {
                m.VisualPrefab.transform.position = m.Position;
                m.VisualPrefab.transform.rotation = m.Rotation;
            }

            // Timeout failsafe
            if (m.TimeAlive > 8f)
            {
                DestroyMissile(i, m);
                continue;
            }

            activeMissiles[i] = m;
        }
    }

    private void HandleHit(RaycastHit hit, ItanoMissile m)
    {
        if (m.HitVfx != null) Instantiate(m.HitVfx, hit.point, Quaternion.LookRotation(hit.normal));

        if (m.ExplosionRadius > 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(hit.point, m.ExplosionRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IDamage>(out var aoeDamageable))
                {
                    aoeDamageable.TakeDamage(m.Damage);
                }
            }
        }
        else
        {
            if (hit.collider.TryGetComponent<IDamage>(out var damageable)) damageable.TakeDamage(m.Damage);
        }
    }

    private void DestroyMissile(int index, ItanoMissile m)
    {
        if (m.VisualPrefab != null) Destroy(m.VisualPrefab);
        activeMissiles.RemoveAt(index);
    }
}