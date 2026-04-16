using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;

    private struct Projectile
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Lifetime;
        public int Damage;
        public Transform Source;

        public GameObject VisualPrefab;
        public GameObject HitParticlePrefab;

        public Transform HomingTarget;
        public float TurnSpeed;
        public float ExplosionRadius;

        // New Additions
        public float Gravity;
        public bool IsSticky;
        public float StickDelay;

        // Runtime Sticky State
        public bool IsStuck;
        public Transform StuckTransform;
        public Vector3 StuckLocalPosition;
        public Quaternion StuckLocalRotation;
    }

    private List<Projectile> activeProjectiles = new List<Projectile>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Updated signature to take new physics variables
    public void SpawnProjectile(Vector3 startPos, Vector3 velocity, int damage, float lifetime, Transform source, GameObject visualPrefab, GameObject hitParticlePrefab, Transform homingTarget = null, float turnSpeed = 0f, float explosionRadius = 0f, float gravity = 0f, bool isSticky = false, float stickDelay = 0f)
    {
        GameObject visual = null;
        if (visualPrefab != null)
        {
            visual = Instantiate(visualPrefab, startPos, Quaternion.LookRotation(velocity));
        }

        activeProjectiles.Add(new Projectile
        {
            Position = startPos,
            Velocity = velocity,
            Lifetime = lifetime,
            Damage = damage,
            Source = source,
            VisualPrefab = visual,
            HitParticlePrefab = hitParticlePrefab,
            HomingTarget = homingTarget,
            TurnSpeed = turnSpeed,
            ExplosionRadius = explosionRadius,
            Gravity = gravity,
            IsSticky = isSticky,
            StickDelay = stickDelay,
            IsStuck = false
        });
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Projectile p = activeProjectiles[i];

            // Handle Sticky State
            if (p.IsStuck)
            {
                p.Lifetime -= dt;
                if (p.StuckTransform != null)
                {
                    p.Position = p.StuckTransform.TransformPoint(p.StuckLocalPosition);
                    if (p.VisualPrefab != null)
                    {
                        p.VisualPrefab.transform.position = p.Position;
                        p.VisualPrefab.transform.rotation = p.StuckTransform.rotation * p.StuckLocalRotation;
                    }
                }
                else
                {
                    // Whatever it stuck to died/disappeared. Start falling.
                    p.IsStuck = false;
                }

                if (p.Lifetime <= 0)
                {
                    HandleExplosion(p.Position, Vector3.up, p);
                    DestroyProjectile(i, p);
                }
                else
                {
                    activeProjectiles[i] = p; // Save struct
                }
                continue;
            }

            p.Lifetime -= dt;
            if (p.Lifetime <= 0)
            {
                DestroyProjectile(i, p);
                continue;
            }

            // Apply Gravity
            if (p.Gravity > 0)
            {
                p.Velocity += Vector3.down * (p.Gravity * dt);
            }

            // Homing Logic
            if (p.HomingTarget != null && p.HomingTarget.gameObject.activeInHierarchy)
            {
                Vector3 targetDir = (p.HomingTarget.position - p.Position).normalized;
                p.Velocity = Vector3.RotateTowards(p.Velocity.normalized, targetDir, p.TurnSpeed * dt, 0f) * p.Velocity.magnitude;
            }

            if (p.VisualPrefab != null && p.Velocity != Vector3.zero)
                p.VisualPrefab.transform.rotation = Quaternion.LookRotation(p.Velocity);

            Vector3 nextPos = p.Position + (p.Velocity * dt);
            Vector3 direction = nextPos - p.Position;
            float distance = direction.magnitude;

            if (Physics.Raycast(p.Position, direction.normalized, out RaycastHit hit, distance))
            {
                if (p.IsSticky)
                {
                    p.IsStuck = true;
                    p.StuckTransform = hit.transform;
                    p.StuckLocalPosition = hit.transform.InverseTransformPoint(hit.point);
                    p.StuckLocalRotation = Quaternion.Inverse(hit.transform.rotation) * Quaternion.LookRotation(hit.normal);
                    p.Lifetime = p.StickDelay; // Reset lifetime to become the fuse timer

                    activeProjectiles[i] = p;
                    continue;
                }
                else
                {
                    HandleExplosion(hit.point, hit.normal, p);
                    DestroyProjectile(i, p);
                    continue;
                }
            }

            p.Position = nextPos;
            if (p.VisualPrefab != null) p.VisualPrefab.transform.position = p.Position;

            activeProjectiles[i] = p;
        }
    }

    private void HandleExplosion(Vector3 hitPoint, Vector3 normal, Projectile p)
    {
        if (p.HitParticlePrefab != null)
        {
            Instantiate(p.HitParticlePrefab, hitPoint, Quaternion.LookRotation(normal));
        }

        if (p.ExplosionRadius > 0f)
        {
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, p.ExplosionRadius);
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IDamage>(out var aoeDamageable))
                {
                    // Pass the explosion center (hitPoint) so targets are pushed outwards
                    aoeDamageable.TakeDamage(p.Damage, hitPoint);
                }
            }
        }
        else
        {
            // For single target, we check a tiny sphere to catch what we hit
            Collider[] pointHit = Physics.OverlapSphere(hitPoint, 0.5f);
            foreach (var col in pointHit)
            {
                if (col.TryGetComponent<IDamage>(out var damageable))
                {
                    damageable.TakeDamage(p.Damage, hitPoint);
                    break;
                }
            }
        }
    }

    private void DestroyProjectile(int index, Projectile p)
    {
        if (p.VisualPrefab != null) Destroy(p.VisualPrefab);
        activeProjectiles.RemoveAt(index);
    }
}