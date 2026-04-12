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

    [Header("Itano Zigzag (The Circus)")]
    [Tooltip("Time before the missile actually starts chasing the target")]
    public float HomingDelay = 0.4f;
    public float TurnSpeed = 250f;
    [Tooltip("How far off the direct line to the target the missile pulls")]
    public float ZigzagIntensity = 4f;
    [Tooltip("How many times per second the missile changes its zigzag direction")]
    public float ZigzagFrequency = 4f;
    [Tooltip("How violently it snaps to the new trajectory (higher = sharper snap)")]
    public float SnapSharpness = 20f;

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
                zigInt: ZigzagIntensity,
                zigFreq: ZigzagFrequency,
                snapSharp: SnapSharpness,
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
    private float explosionRadius;
    private float homingDelay;
    private GameObject hitVfx;

    // Zigzag properties
    private float zigzagIntensity;
    private float zigzagFrequency;
    private float snapSharpness;

    private float currentSpeed;
    private float timeAlive;
    private Vector3 currentVelocity;

    // Trajectory tracking
    private Vector3 baseDir;
    private float timeSinceLastSnap;
    private float currentZigzagInterval;
    private Vector2 currentZigzagDir;

    private bool hasOvershot = false;
    private float closestDistance = float.MaxValue;

    public void Initialize(Transform target, int damage, float maxSpeed, float accel, float ejectSpeed, float spread, float upwardBias, float delay, float turnSpd, float zigInt, float zigFreq, float snapSharp, float expRad, GameObject hitVfx)
    {
        this.target = target;
        this.damage = damage;
        this.maxSpeed = maxSpeed;
        this.acceleration = accel;
        this.homingDelay = delay;
        this.turnSpeed = turnSpd;
        this.zigzagIntensity = zigInt;
        this.zigzagFrequency = zigFreq;
        this.snapSharpness = snapSharp;
        this.explosionRadius = expRad;
        this.hitVfx = hitVfx;

        this.currentSpeed = ejectSpeed;

        this.currentZigzagDir = Random.insideUnitCircle.normalized;
        this.currentZigzagInterval = (1f / Mathf.Max(0.1f, zigzagFrequency)) * Random.Range(0.8f, 1.2f);
        this.timeSinceLastSnap = 0f;

        Vector3 spreadDir = Quaternion.Euler(
            Random.Range(-spread, spread) * 0.5f,
            Random.Range(-spread, spread),
            0
        ) * transform.forward;

        spreadDir += transform.up * Random.Range(upwardBias * 0.5f, upwardBias);
        spreadDir += transform.right * Random.Range(-upwardBias * 0.5f, upwardBias * 0.5f);
        spreadDir.Normalize();

        this.baseDir = spreadDir;

        transform.rotation = Quaternion.LookRotation(spreadDir);
        currentVelocity = spreadDir * currentSpeed;

        Destroy(gameObject, 6f); 
    }

    void Update()
    {
        timeAlive += Time.deltaTime;
        float dt = Time.deltaTime;

        if (timeAlive > homingDelay)
        {
            //TODO: ignite thrusters, I wanna put vfx here
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * dt);

            float distToTarget = 100f;

            if (target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 targetCenter = target.position + Vector3.up * 1f;
                Vector3 dirToTarget = (targetCenter - transform.position).normalized;
                distToTarget = Vector3.Distance(transform.position, targetCenter);

                if (!hasOvershot)
                {
                    closestDistance = Mathf.Min(closestDistance, distToTarget);

                    if (distToTarget < 15f && distToTarget > closestDistance + 1.5f)
                    {
                        hasOvershot = true; // Lock lost!
                    }
                    else
                    {
                        baseDir = Vector3.RotateTowards(baseDir, dirToTarget, turnSpeed * Mathf.Deg2Rad * dt, 0f);
                    }
                }
            }

            timeSinceLastSnap += dt;
            if (timeSinceLastSnap >= currentZigzagInterval)
            {
                currentZigzagDir = Random.insideUnitCircle.normalized;
                timeSinceLastSnap = 0f;
                currentZigzagInterval = (1f / Mathf.Max(0.1f, zigzagFrequency)) * Random.Range(0.5f, 1.5f);
            }

            Vector3 stableRight = Vector3.Cross(Vector3.up, baseDir).normalized;
            if (stableRight == Vector3.zero) stableRight = Vector3.right; // Fallback
            Vector3 stableUp = Vector3.Cross(baseDir, stableRight).normalized;


            float distanceFactor = hasOvershot ? 1f : Mathf.Clamp01((distToTarget - 8f) / 17f);

            Vector3 zigzagOffset = (stableRight * currentZigzagDir.x + stableUp * currentZigzagDir.y) * (zigzagIntensity * 0.25f * distanceFactor);

            Vector3 desiredDir = (baseDir + zigzagOffset).normalized;

            float activeTurnSpeed = snapSharpness * 50f;

            //final destination
            if (distToTarget < 12f && target != null && !hasOvershot)
            {
                Vector3 targetCenter = target.position + Vector3.up * 1f;
                desiredDir = (targetCenter - transform.position).normalized;
                activeTurnSpeed = Mathf.Max(activeTurnSpeed, turnSpeed * 5f); // uber fast tracking
            }

            Quaternion targetRot = Quaternion.LookRotation(desiredDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, activeTurnSpeed * dt);
        }
        else
        {
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