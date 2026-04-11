using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct CombatInput
{
    public bool ShootPrimary;
    public bool ShootSecondary;
    public bool ShootLeftShoulder;
    public bool ShootRightShoulder;
    public bool Reload;
    public Ray AimRay;
}

public class CombatSystem : MonoBehaviour, IResourceProvider
{
    [Header("Aiming")]
    [Tooltip("The base of the character (used for calculating source of damage)")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Transform torso;
    [SerializeField] private bool flipTorsoRotation = false;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Aim & Lock-On Settings")]
    [SerializeField] private float hoverRadius = 2f;
    [SerializeField] private float lockOnTimeRequired = 0.5f;
    [SerializeField] private float lockOnMaxDistance = 50f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Visuals")]
    [Tooltip("Assign a UI/Sprite object in the scene to act as the lock-on reticle")]
    [SerializeField] private GameObject lockOnIndicator;
    [Tooltip("How fast the indicator lerps to the target or cursor")]
    [SerializeField] private float indicatorLerpSpeed = 25f;

    [Header("Lock-On Line")]
    [SerializeField] private LineRenderer lockOnLine;
    [Tooltip("How much space (in world units) to leave empty at the ends of the line")]
    [SerializeField] private float lineGapDistance = 1.5f;

    [Header("Shooting Points")]
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform muzzlePoint;

    [Header("AC6 Loadout")]
    [SerializeField] private WeaponData primaryWeapon;
    [SerializeField] private WeaponData secondaryWeapon;
    [SerializeField] private WeaponData leftShoulderWeapon;
    [SerializeField] private WeaponData rightShoulderWeapon;

    // Runtime tracking
    private WeaponInstance[] loadoutSlots = new WeaponInstance[4];
    private Dictionary<ResourceType, int> resourceToSlotMap = new Dictionary<ResourceType, int>();

    private AudioManager audioManager;

    private ITarget hoveringTarget;
    private float currentHoverTime = 0f;
    private ITarget lockedTarget;
    private Vector3 currentTargetPoint;
    private Image indicatorImage;

    private CombatInput previousInput;

    void Start()
    {
        audioManager = FindAnyObjectByType<AudioManager>();

        if (lockOnIndicator != null)
        {
            indicatorImage = lockOnIndicator.transform.GetChild(0).Find("Indicator").GetComponent<Image>();
        }

        loadoutSlots = new WeaponInstance[4];

        InitializeWeaponSlot(0, primaryWeapon);
        InitializeWeaponSlot(1, secondaryWeapon);
        InitializeWeaponSlot(2, leftShoulderWeapon);
        InitializeWeaponSlot(3, rightShoulderWeapon);

        resourceToSlotMap[ResourceType.PrimaryAmmo] = 0;
        resourceToSlotMap[ResourceType.SecondaryAmmo] = 1;
        resourceToSlotMap[ResourceType.LeftAmmo] = 2;
        resourceToSlotMap[ResourceType.RightAmmo] = 3;
    }

    private void InitializeWeaponSlot(int index, WeaponData data)
    {
        if (data != null)
        {
            loadoutSlots[index] = new WeaponInstance();
            WeaponContext context = new WeaponContext
            {
                Runner = this,
                PlayerRoot = characterRoot,
                ShootingPoint = shootingPoint,
                MuzzlePoint = muzzlePoint,
                Audio = audioManager
            };
            loadoutSlots[index].Initialize(data, context);
        }
    }

    private void HandleAimingAndLockOn(CombatInput input)
    {
        if (characterRoot == null || torso == null) return;

        RaycastHit[] hits = Physics.SphereCastAll(input.AimRay, hoverRadius, Mathf.Infinity, targetLayer);

        ITarget bestTarget = null;
        float closestDistToRay = float.MaxValue;

        foreach (var hit in hits)
        {
            ITarget hitTarget = hit.collider.GetComponentInParent<ITarget>();

            if (hitTarget != null && hitTarget.IsValid)
            {
                float distToRay = Vector3.Cross(input.AimRay.direction, hitTarget.Transform.position - input.AimRay.origin).magnitude;

                if (distToRay < closestDistToRay)
                {
                    closestDistToRay = distToRay;
                    bestTarget = hitTarget;
                }
            }
        }

        if (bestTarget != null)
        {
            if (bestTarget == hoveringTarget)
            {
                currentHoverTime += Time.deltaTime;
                if (currentHoverTime >= lockOnTimeRequired && lockedTarget != bestTarget)
                {
                    lockedTarget = bestTarget; // LOCK ON
                    Debug.Log($"<color=green>[CombatSystem] Locked onto: {lockedTarget.Transform.name}</color>");
                }
            }
            else
            {
                hoveringTarget = bestTarget;
                currentHoverTime = 0f;
            }
        }
        else
        {
            hoveringTarget = null;
            currentHoverTime = 0f;
        }

        if (lockedTarget != null)
        {
            float dist = Vector3.Distance(characterRoot.position, lockedTarget.Transform.position);

            if (!lockedTarget.IsValid || dist > lockOnMaxDistance || (hoveringTarget != null && hoveringTarget != lockedTarget && currentHoverTime >= lockOnTimeRequired))
            {
                Debug.Log($"<color=red>[CombatSystem] Lost lock on: {lockedTarget.Transform.name}</color>");
                lockedTarget = null;
            }
        }

        Vector3 rawMousePos;
        if (Physics.Raycast(input.AimRay, out RaycastHit envHit, Mathf.Infinity))
        {
            rawMousePos = envHit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, characterRoot.position);
            if (groundPlane.Raycast(input.AimRay, out float hitDistance))
            {
                rawMousePos = input.AimRay.GetPoint(hitDistance);
            }
            else
            {
                rawMousePos = input.AimRay.GetPoint(50f);
            }
        }

        if (lockedTarget != null)
        {
            float projSpeed = 100f;

            if (loadoutSlots[0] != null && loadoutSlots[0].Data != null)
            {
                if (loadoutSlots[0].Data.EmitterModule is ProjectileEmitter projEmitter)
                {
                    projSpeed = projEmitter.BulletForce;
                }
            }

            Vector3 targetCenterMass = lockedTarget.Transform.position + (Vector3.up * 0.8f);

            currentTargetPoint = CalculateInterceptCourse(
                shootingPoint.position,
                targetCenterMass,
                lockedTarget.Velocity,
                projSpeed
            );
        }
        else
        {
            currentTargetPoint = rawMousePos;
        }

        if (lockOnIndicator != null)
        {
            Vector3 targetIndicatorPos;

            if (lockedTarget != null)
            {
                if (indicatorImage != null) indicatorImage.color = Color.yellow;
                targetIndicatorPos = lockedTarget.Transform.position;
            }
            else
            {
                if (indicatorImage != null) indicatorImage.color = Color.white;
                targetIndicatorPos = currentTargetPoint;
            }

            lockOnIndicator.transform.position = Vector3.Lerp(
                lockOnIndicator.transform.position,
                targetIndicatorPos,
                Time.deltaTime * indicatorLerpSpeed
            );

            if (Camera.main != null)
            {
                Vector3 lookDir = lockOnIndicator.transform.position - Camera.main.transform.position;
                lockOnIndicator.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (lockOnLine != null)
        {
            if (lockedTarget != null)
            {
                Vector3 lineStart = rawMousePos + (Vector3.up * 0.1f);
                Vector3 lineEnd = lockOnIndicator.transform.position;

                Vector3 dir = lineEnd - lineStart;
                float dist = dir.magnitude;

                if (dist > lineGapDistance * 2f)
                {
                    if (!lockOnLine.enabled) lockOnLine.enabled = true;

                    Vector3 startPos = lineStart + (dir.normalized * lineGapDistance);
                    Vector3 endPos = lineEnd - (dir.normalized * lineGapDistance);

                    lockOnLine.SetPosition(0, startPos);
                    lockOnLine.SetPosition(1, endPos);
                }
                else
                {
                    if (lockOnLine.enabled) lockOnLine.enabled = false;
                }
            }
            else
            {
                if (lockOnLine.enabled) lockOnLine.enabled = false;
            }
        }

        Vector3 direction = (currentTargetPoint - torso.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            if (flipTorsoRotation) direction = -direction;
            torso.rotation = Quaternion.Lerp(torso.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
        }
    }

    private Vector3 CalculateInterceptCourse(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVel, float projSpeed)
    {
        Vector3 dirToTarget = targetPos - shooterPos;

        float a = Vector3.Dot(targetVel, targetVel) - (projSpeed * projSpeed);
        float b = 2f * Vector3.Dot(targetVel, dirToTarget);
        float c = Vector3.Dot(dirToTarget, dirToTarget);

        float discriminant = (b * b) - (4f * a * c);

        if (discriminant > 0f)
        {
            float t1 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
            float t2 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            float t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);

            if (t > 0f) return targetPos + (targetVel * t);
        }

        return targetPos;
    }

    public void ProcessCombat(CombatInput input)
    {
        HandleAimingAndLockOn(input);

        for (int i = 0; i < 4; i++)
        {
            if (loadoutSlots[i] != null)
            {
                loadoutSlots[i].Context.CurrentTarget = lockedTarget?.Transform;
                loadoutSlots[i].Context.TargetPoint = currentTargetPoint;
            }
        }

        HandleWeapon(loadoutSlots[0], input.ShootPrimary, previousInput.ShootPrimary, input.Reload);
        HandleWeapon(loadoutSlots[1], input.ShootSecondary, previousInput.ShootSecondary, input.Reload);
        HandleWeapon(loadoutSlots[2], input.ShootLeftShoulder, previousInput.ShootLeftShoulder, input.Reload);
        HandleWeapon(loadoutSlots[3], input.ShootRightShoulder, previousInput.ShootRightShoulder, input.Reload);

        previousInput = input;
    }

    private void HandleWeapon(WeaponInstance weapon, bool isHeld, bool wasHeld, bool isReloadInput)
    {
        if (weapon == null || weapon.Data == null) return;

        bool isDown = isHeld && !wasHeld;
        bool isUp = !isHeld && wasHeld;

        // Auto-Reload trigger
        if ((isReloadInput || (isHeld && weapon.CurrentAmmo <= 0)) && !weapon.IsReloading && weapon.CurrentAmmo < weapon.Data.MaxAmmo)
        {
            StartCoroutine(ReloadRoutine(weapon));
            return;
        }

        if (!weapon.IsReloading)
        {
            weapon.UpdateInput(isDown, isHeld, isUp);
        }
    }

    private IEnumerator ReloadRoutine(WeaponInstance weapon)
    {
        weapon.IsReloading = true;
        int missingAmmo = weapon.Data.MaxAmmo - weapon.CurrentAmmo;

        if (missingAmmo > 0)
        {
            float timePerBullet = weapon.Data.ReloadTime / missingAmmo;
            for (int i = 0; i < missingAmmo; i++)
            {
                yield return new WaitForSeconds(timePerBullet);
                weapon.CurrentAmmo++;
            }
        }
        weapon.IsReloading = false;
    }

    public float GetResourcePercentage(ResourceType type)
    {
        if (resourceToSlotMap.TryGetValue(type, out int slotIndex))
        {
            var weapon = loadoutSlots[slotIndex];
            if (weapon != null && weapon.Data != null && weapon.Data.MaxAmmo > 0)
                return (float)weapon.CurrentAmmo / weapon.Data.MaxAmmo;
        }
        return 0f;
    }

    public int GetResourceCurrent(ResourceType type)
    {
        if (resourceToSlotMap.TryGetValue(type, out int slotIndex))
            return loadoutSlots[slotIndex]?.CurrentAmmo ?? 0;
        return 0;
    }

    public int GetResourceMax(ResourceType type)
    {
        if (resourceToSlotMap.TryGetValue(type, out int slotIndex))
            return loadoutSlots[slotIndex]?.Data.MaxAmmo ?? 0;
        return 0;
    }
}