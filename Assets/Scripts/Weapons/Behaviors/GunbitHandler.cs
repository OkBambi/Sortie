using System.Collections.Generic;
using UnityEngine;

public class GunbitHandler : MonoBehaviour
{
    public static GunbitHandler Instance;

    public class ActiveGunbit
    {
        public Transform MoveRoot;
        public Transform AimRoot;
        public GameObject VisualPrefab;
        public Transform MuzzlePoint;

        public Vector3 BaseOffset;
        public float FollowSmoothTime;
        public float RotationSpeed;
        public float HoverSpeed;
        public float HoverAmplitude;

        // State
        public Vector3 CurrentVelocity;
        public float RandomHoverOffset;
        public float Health;

        // Orbital State
        public Vector3 OrbitAxis;
        public float OrbitRadius;
        public float OrbitSpeed;
        public float CurrentOrbitAngle;

        // Combat State
        public WeaponInstance Weapon;
        public bool IsPreparingToFire;
        public float FireDelayTimer;
        public Transform CurrentTarget;
    }

    private List<ActiveGunbit> activeGunbits = new List<ActiveGunbit>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public void SpawnGunbit(Transform moveRoot, Transform aimRoot, Vector3 offset, float smoothTime, float rotSpeed, float hSpeed, float hAmp, float health, GameObject visualObj, WeaponData weaponData)
    {
        GameObject visual = null;
        Transform muzzle = null;

        if (visualObj != null)
        {
            visual = Instantiate(visualObj, moveRoot.position, aimRoot.rotation);
            muzzle = visual.transform.Find("Muzzle");
            if (muzzle == null) muzzle = visual.transform;
        }

        WeaponInstance bitWeapon = null;
        if (weaponData != null)
        {
            bitWeapon = new WeaponInstance();

            WeaponContext ctx = new WeaponContext
            {
                Runner = this,
                PlayerRoot = visual != null ? visual.transform : moveRoot,
                ShootingPoint = muzzle,
                MuzzlePoint = muzzle,
                CurrentTarget = null,
                TargetPoint = Vector3.zero,
                Audio = null
            };

            bitWeapon.Initialize(weaponData, ctx);
        }

        float randomizedSmoothTime = smoothTime * Random.Range(0.6f, 1.4f);

        activeGunbits.Add(new ActiveGunbit
        {
            MoveRoot = moveRoot,
            AimRoot = aimRoot,
            VisualPrefab = visual,
            MuzzlePoint = muzzle,
            BaseOffset = offset,
            FollowSmoothTime = randomizedSmoothTime,
            RotationSpeed = rotSpeed,
            HoverSpeed = hSpeed,
            HoverAmplitude = hAmp,
            CurrentVelocity = Vector3.zero,
            RandomHoverOffset = Random.Range(0f, 100f),
            Health = health,

            // init orbit
            OrbitAxis = Random.onUnitSphere, 
            OrbitRadius = Random.Range(1.5f, 3.5f),
            OrbitSpeed = Random.Range(30f, 90f) * (Random.value > 0.5f ? 1f : -1f), 
            CurrentOrbitAngle = Random.Range(0f, 360f), 

            Weapon = bitWeapon,
            IsPreparingToFire = false
        });
    }

    public void CommandSwarmToFire(Transform target, float maxRandomDelay = 0.4f)
    {
        foreach (var bit in activeGunbits)
        {
            if (bit.Weapon != null && !bit.Weapon.IsReloading && bit.Weapon.CurrentAmmo > 0)
            {
                bit.CurrentTarget = target;
                bit.FireDelayTimer = Random.Range(0f, maxRandomDelay);
                bit.IsPreparingToFire = true;
            }
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        int totalBits = activeGunbits.Count;

        for (int i = activeGunbits.Count - 1; i >= 0; i--)
        {
            ActiveGunbit bit = activeGunbits[i];

            if (bit.MoveRoot == null || !bit.MoveRoot.gameObject.activeInHierarchy || bit.Health <= 0)
            {
                DestroyGunbit(i, bit);
                continue;
            }

            //orbit
            bit.CurrentOrbitAngle += bit.OrbitSpeed * dt;

            Vector3 tangent = Vector3.Cross(bit.OrbitAxis, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(bit.OrbitAxis, Vector3.right);
            tangent.Normalize();

            Vector3 orbitOffset = Quaternion.AngleAxis(bit.CurrentOrbitAngle, bit.OrbitAxis) * tangent * bit.OrbitRadius;

            float driftTime = Time.time * bit.HoverSpeed;
            float driftX = Mathf.Sin(driftTime * 0.8f + bit.RandomHoverOffset) * bit.HoverAmplitude;
            float driftY = Mathf.Sin(driftTime + bit.RandomHoverOffset) * bit.HoverAmplitude;
            float driftZ = Mathf.Cos(driftTime * 0.9f + bit.RandomHoverOffset) * bit.HoverAmplitude;

            Vector3 finalOffset = orbitOffset + bit.BaseOffset + new Vector3(driftX, driftY, driftZ);

            Vector3 targetPosition = bit.MoveRoot.position + bit.AimRoot.TransformDirection(finalOffset);

            // floor is labva
            Vector3 rayStart = new Vector3(targetPosition.x, bit.MoveRoot.position.y + 5f, targetPosition.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f))
            {
                if (hit.collider.CompareTag("Ground"))
                {
                    float minClearanceHeight = hit.point.y + 0.6f; 
                    if (targetPosition.y < minClearanceHeight)
                    {
                        targetPosition.y = minClearanceHeight;
                    }
                }
            }

            if (bit.VisualPrefab != null)
            {
                bit.VisualPrefab.transform.position = Vector3.SmoothDamp(
                    bit.VisualPrefab.transform.position,
                    targetPosition,
                    ref bit.CurrentVelocity,
                    bit.FollowSmoothTime
                );

                Quaternion targetRotation = bit.AimRoot.rotation;

                if ((bit.IsPreparingToFire || bit.CurrentTarget != null) && bit.CurrentTarget.gameObject.activeInHierarchy)
                {
                    Vector3 dirToTarget = (bit.CurrentTarget.position - bit.VisualPrefab.transform.position).normalized;
                    if (dirToTarget != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(dirToTarget);
                    }
                }


                bit.VisualPrefab.transform.rotation = Quaternion.Slerp(
                    bit.VisualPrefab.transform.rotation,
                    targetRotation,
                    bit.RotationSpeed * dt
                );
            }

            //fire
            if (bit.IsPreparingToFire)
            {
                bit.FireDelayTimer -= dt;

                if (bit.FireDelayTimer <= 0)
                {
                    ExecuteGunbitFire(bit);
                    bit.IsPreparingToFire = false;
                }
            }

            activeGunbits[i] = bit;
        }
    }

    private void ExecuteGunbitFire(ActiveGunbit bit)
    {
        if (bit.Weapon == null || bit.CurrentTarget == null || !bit.CurrentTarget.gameObject.activeInHierarchy)
            return;

        bit.Weapon.Context.CurrentTarget = bit.CurrentTarget;
        bit.Weapon.Context.TargetPoint = bit.CurrentTarget.position;

        bit.Weapon.TryFire(1f);
    }

    private void DestroyGunbit(int index, ActiveGunbit bit)
    {
        if (bit.VisualPrefab != null)
        {
            Destroy(bit.VisualPrefab);
        }
        activeGunbits.RemoveAt(index);
    }
}