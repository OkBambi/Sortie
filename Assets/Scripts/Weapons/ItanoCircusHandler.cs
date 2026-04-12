using UnityEngine;

public class ItanoCircusHandler : MonoBehaviour
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