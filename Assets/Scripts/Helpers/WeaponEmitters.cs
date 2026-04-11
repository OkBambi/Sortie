using UnityEngine;

//PROJECTILE
[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Projectile")]
public class ProjectileEmitter : WeaponEmitter
{
    public int BaseDamage = 10;
    public float BulletForce = 60f;
    public float BulletLifetime = 3f;

    [Header("Advanced")]
    public bool IsHoming = false;
    public float HomingTurnSpeed = 5f;

    [Tooltip("If > 0, it acts as a Grenade/Rocket launcher")]
    public float ExplosionRadius = 0f;

    [Header("Visuals")]
    public GameObject BulletVisualPrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject HitParticlePrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        Vector3 direction = (instance.Context.TargetPoint - instance.Context.ShootingPoint.position).normalized;
        Vector3 velocity = direction * BulletForce;

        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation, instance.Context.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        // Apply charge modifier to damage
        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        ProjectileManager.Instance.SpawnProjectile(
            instance.Context.MuzzlePoint.position,
            velocity,
            finalDamage,
            BulletLifetime,
            instance.Context.PlayerRoot,
            BulletVisualPrefab,
            HitParticlePrefab,
            IsHoming ? instance.Context.CurrentTarget : null,
            HomingTurnSpeed,
            ExplosionRadius
        );
    }
}

//Sword
[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Melee")]
public class MeleeEmitter : WeaponEmitter
{
    public int BaseDamage = 25;
    public float HitRadius = 2f;
    public float ForwardOffset = 1f;
    public LayerMask EnemyLayer;
    public GameObject SwingVFXPrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        if (SwingVFXPrefab != null)
        {
            GameObject vfx = Instantiate(SwingVFXPrefab, instance.Context.MuzzlePoint.position, instance.Context.PlayerRoot.rotation, instance.Context.PlayerRoot);
            Destroy(vfx, 0.5f);
        }

        Vector3 hitCenter = instance.Context.PlayerRoot.position + (instance.Context.PlayerRoot.forward * ForwardOffset);
        Collider[] hitEnemies = Physics.OverlapSphere(hitCenter, HitRadius, EnemyLayer);

        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(finalDamage);
            }
        }
    }
}

//BEAM MAGNUM
[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Raycast")]
public class RaycastEmitter : WeaponEmitter
{
    public int BaseDamage = 5;
    public float Range = 50f;
    public LayerMask HitLayer;
    public GameObject HitVFX;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        Vector3 direction = (instance.Context.TargetPoint - instance.Context.ShootingPoint.position).normalized;
        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        if (Physics.Raycast(instance.Context.ShootingPoint.position, direction, out RaycastHit hit, Range, HitLayer))
        {
            if (HitVFX != null)
            {
                Instantiate(HitVFX, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (hit.collider.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(finalDamage);
            }
        }
    }
}

//ITALIAN NO SAUCE!!
[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Swarm Missile")]
public class SwarmMissileEmitter : WeaponEmitter
{
    public int BaseDamage = 15;

    [Header("Volley Settings")]
    [Tooltip("How many missiles to fire per trigger pull")]
    public int MissilesPerVolley = 4;
    [Tooltip("Delay between each missile. Set to 0 to fire all simultaneously.")]
    public float DelayBetweenMissiles = 0.05f;

    [Header("Flight Dynamics")]
    public float MaxSpeed = 80f;
    public float Acceleration = 60f;
    public float InitialEjectSpeed = 15f;
    [Tooltip("How wide the missiles spread when fired")]
    public float SpreadAngle = 60f;
    [Tooltip("Forces missiles to shoot upwards before tracking")]
    public float UpwardEjectBias = 1.5f;

    [Header("Itano Wobble")]
    [Tooltip("Time before the missile actually starts chasing the target")]
    public float HomingDelay = 0.4f;
    public float TurnSpeed = 180f;
    [Tooltip("How aggressively the missile swerves off-path")]
    public float WobbleIntensity = 2.5f;
    [Tooltip("How fast the missile wiggles")]
    public float WobbleSpeed = 15f;

    [Header("Explosion")]
    public float ExplosionRadius = 5f;

    [Header("Visuals")]
    public GameObject MissilePrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject HitParticlePrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        if (MissilesPerVolley <= 1)
        {
            SpawnSingleMissile(instance, chargeModifier);
        }
        else
        {
            instance.Context.Runner.StartCoroutine(FireVolleyRoutine(instance, chargeModifier));
        }
    }

    private System.Collections.IEnumerator FireVolleyRoutine(WeaponInstance instance, float chargeModifier)
    {
        for (int i = 0; i < MissilesPerVolley; i++)
        {
            SpawnSingleMissile(instance, chargeModifier);

            if (DelayBetweenMissiles > 0)
            {
                yield return new WaitForSeconds(DelayBetweenMissiles);
            }
        }
    }

    private void SpawnSingleMissile(WeaponInstance instance, float chargeModifier)
    {
        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation, instance.Context.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        if (MissilePrefab != null)
        {
            GameObject missileObj = Instantiate(MissilePrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation);
            var behavior = missileObj.AddComponent<ItanoMissileBehavior>();

            int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

            behavior.Initialize(
                target: instance.Context.CurrentTarget,
                damage: finalDamage,
                maxSpeed: MaxSpeed,
                accel: Acceleration,
                ejectSpeed: InitialEjectSpeed,
                spread: SpreadAngle,
                upwardBias: UpwardEjectBias,
                delay: HomingDelay,
                turnSpd: TurnSpeed,
                wobbleInt: WobbleIntensity,
                wobbleSpd: WobbleSpeed,
                expRad: ExplosionRadius,
                hitVfx: HitParticlePrefab
            );
        }
    }
}

// ITANI LO SAHN!!!
public class ItanoMissileBehavior : MonoBehaviour
{
    private Transform target;
    private int damage;
    private float maxSpeed;
    private float acceleration;
    private float turnSpeed;
    private float wobbleIntensity;
    private float wobbleSpeed;
    private float explosionRadius;
    private float homingDelay;
    private GameObject hitVfx;

    private float currentSpeed;
    private float timeAlive;
    private float randomSeed;
    private Vector3 currentVelocity;

    public void Initialize(Transform target, int damage, float maxSpeed, float accel, float ejectSpeed, float spread, float upwardBias, float delay, float turnSpd, float wobbleInt, float wobbleSpd, float expRad, GameObject hitVfx)
    {
        this.target = target;
        this.damage = damage;
        this.maxSpeed = maxSpeed;
        this.acceleration = accel;
        this.homingDelay = delay;
        this.turnSpeed = turnSpd;
        this.wobbleIntensity = wobbleInt;
        this.wobbleSpeed = wobbleSpd;
        this.explosionRadius = expRad;
        this.hitVfx = hitVfx;

        this.randomSeed = Random.Range(0f, 100f);
        this.currentSpeed = ejectSpeed;

        // Calculate chaotic ejection trajectory
        Vector3 spreadDir = Quaternion.Euler(
            Random.Range(-spread, spread) * 0.5f,
            Random.Range(-spread, spread),
            0
        ) * transform.forward;

        // Force the missile upwards/outwards for the classic anime arc
        spreadDir += transform.up * Random.Range(upwardBias * 0.5f, upwardBias);
        spreadDir += transform.right * Random.Range(-upwardBias * 0.5f, upwardBias * 0.5f);
        spreadDir.Normalize();

        transform.rotation = Quaternion.LookRotation(spreadDir);
        currentVelocity = spreadDir * currentSpeed;

        Destroy(gameObject, 8f); // Absolute max lifetime failsafe
    }

    void Update()
    {
        timeAlive += Time.deltaTime;
        float dt = Time.deltaTime;

        if (timeAlive > homingDelay)
        {
            // Ignite thrusters and accelerate
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * dt);

            Vector3 targetDir = transform.forward;
            float distToTarget = 100f; // Default high if no target

            if (target != null && target.gameObject.activeInHierarchy)
            {
                // Aim slightly ahead of or directly at target center
                Vector3 targetCenter = target.position + Vector3.up * 1f;
                targetDir = (targetCenter - transform.position).normalized;
                distToTarget = Vector3.Distance(transform.position, targetCenter);
            }

            // 1. TIGHTER WOBBLE: Calculate stable axes relative to the target line, NOT the missile's local rotation.
            // Using the missile's local rotation causes a centrifugal feedback loop that pushes it away!
            Vector3 stableRight = Vector3.Cross(Vector3.up, targetDir).normalized;
            if (stableRight == Vector3.zero) stableRight = Vector3.right; // Fallback if pointing straight up/down
            Vector3 stableUp = Vector3.Cross(targetDir, stableRight).normalized;

            float t = timeAlive * wobbleSpeed + randomSeed;
            Vector3 wobble = (stableRight * Mathf.Cos(t) + stableUp * Mathf.Sin(t * 1.3f)) * wobbleIntensity;

            // 2. HIT ASSURANCE: Decay the wobble to 0 as we get close so it actually hits!
            float distanceFactor = Mathf.Clamp01(distToTarget / 15f); // Wobble starts dying off within 15 meters
            wobble *= distanceFactor;

            Vector3 desiredDir = (targetDir + wobble).normalized;

            // 3. SNAPPING ASSURANCE: Increase turn speed by 4x when right next to the target to prevent orbiting
            float activeTurnSpeed = target != null ? Mathf.Lerp(turnSpeed * 4f, turnSpeed, distanceFactor) : turnSpeed;

            // Swerve towards the trajectory
            Quaternion targetRot = Quaternion.LookRotation(desiredDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, activeTurnSpeed * dt);
        }
        else
        {
            // Hang time: Air friction slows the missile briefly before tracking begins
            currentSpeed = Mathf.Lerp(currentSpeed, currentSpeed * 0.5f, dt * 3f);
        }

        currentVelocity = transform.forward * currentSpeed;
        Vector3 nextPos = transform.position + (currentVelocity * dt);

        // Raycast hit detection
        float distance = currentVelocity.magnitude * dt;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit);
            return;
        }

        transform.position = nextPos;
    }

    private void HandleHit(RaycastHit hit)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, hit.point, Quaternion.LookRotation(hit.normal));
        }

        if (explosionRadius > 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(hit.point, explosionRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IDamage>(out var aoeDamageable))
                {
                    aoeDamageable.TakeDamage(damage);
                }
            }
        }
        else
        {
            if (hit.collider.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}