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

        // Modularity additions
        public Transform HomingTarget;
        public float TurnSpeed;
        public float ExplosionRadius;
    }

    private List<Projectile> activeProjectiles = new List<Projectile>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnProjectile(Vector3 startPos, Vector3 velocity, int damage, float lifetime, Transform source, GameObject visualPrefab, GameObject hitParticlePrefab, Transform homingTarget = null, float turnSpeed = 0f, float explosionRadius = 0f)
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
            ExplosionRadius = explosionRadius
        });
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Projectile p = activeProjectiles[i];
            p.Lifetime -= dt;

            if (p.Lifetime <= 0)
            {
                DestroyProjectile(i, p);
                continue;
            }

            // Homing Logic
            if (p.HomingTarget != null && p.HomingTarget.gameObject.activeInHierarchy)
            {
                Vector3 targetDir = (p.HomingTarget.position - p.Position).normalized;
                p.Velocity = Vector3.RotateTowards(p.Velocity.normalized, targetDir, p.TurnSpeed * dt, 0f) * p.Velocity.magnitude;

                if (p.VisualPrefab != null)
                    p.VisualPrefab.transform.rotation = Quaternion.LookRotation(p.Velocity);
            }

            Vector3 nextPos = p.Position + (p.Velocity * dt);
            Vector3 direction = nextPos - p.Position;
            float distance = direction.magnitude;

            if (Physics.Raycast(p.Position, direction.normalized, out RaycastHit hit, distance))
            {
                HandleHit(hit, p);
                DestroyProjectile(i, p);
                continue;
            }

            p.Position = nextPos;
            if (p.VisualPrefab != null) p.VisualPrefab.transform.position = p.Position;

            activeProjectiles[i] = p;
        }
    }

    private void HandleHit(RaycastHit hit, Projectile p)
    {
        if (p.HitParticlePrefab != null)
        {
            Instantiate(p.HitParticlePrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // Explosive AoE Logic
        if (p.ExplosionRadius > 0f)
        {
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, p.ExplosionRadius);
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IDamage>(out var aoeDamageable))
                {
                    aoeDamageable.TakeDamage(p.Damage);
                }
            }
        }
        else // Single Target Logic
        {
            if (hit.collider.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(p.Damage);
            }
        }
    }

    private void DestroyProjectile(int index, Projectile p)
    {
        if (p.VisualPrefab != null) Destroy(p.VisualPrefab);
        activeProjectiles.RemoveAt(index);
    }
}