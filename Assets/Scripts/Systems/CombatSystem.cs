using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct CombatInput
{
    public bool Shoot;
    public bool Reload;
    public int NumberKeyMap;
    public Ray AimRay;
}

// Data passed to the weapon so it knows where to spawn things and can run Coroutines
public struct WeaponContext
{
    public MonoBehaviour Runner;
    public Transform PlayerRoot;
    public Transform ShootingPoint;
    public Transform MuzzlePoint;
    public Transform CurrentTarget;
    public Vector3 TargetPoint;
    public AudioManager Audio;
}

[Serializable]
public class WeaponSlot
{
    public WeaponData Data;
    public int CurrentAmmo;
    public bool IsReloading;
    public float LastFireTime;

    public void Initialize()
    {
        CurrentAmmo = Data.MaxAmmo;
        IsReloading = false;
        LastFireTime = -Data.FireRate;
    }
}

public class CombatSystem : MonoBehaviour
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

    [Header("Loadout")]
    [SerializeField] private List<WeaponData> initialLoadout;
    private List<WeaponSlot> loadoutSlots = new List<WeaponSlot>();
    private int activeSlotIndex = 0;

    private AudioManager audioManager;

    // Aiming State
    private ITarget hoveringTarget;
    private float currentHoverTime = 0f;
    private ITarget lockedTarget;
    private Vector3 currentTargetPoint;
    private Image indicatorImage;

    void Start()
    {
        audioManager = FindAnyObjectByType<AudioManager>();

        if (lockOnIndicator != null)
        {
            indicatorImage = lockOnIndicator.transform.GetChild(0).Find("Indicator").GetComponent<Image>();
        }

        foreach (var weaponData in initialLoadout)
        {
            var slot = new WeaponSlot { Data = weaponData };
            slot.Initialize();
            loadoutSlots.Add(slot);
        }
    }

    public WeaponSlot GetActiveWeapon()
    {
        if (loadoutSlots.Count == 0) return null;
        return loadoutSlots[activeSlotIndex];
    }

    private void HandleAimingAndLockOn(CombatInput input)
    {
        if (characterRoot == null || torso == null) return;

        if (Physics.SphereCast(input.AimRay, hoverRadius, out RaycastHit hit, Mathf.Infinity, targetLayer))
        {
            ITarget hitTarget = hit.collider.GetComponentInParent<ITarget>();

            if (hitTarget != null && hitTarget.IsValid)
            {
                if (hitTarget == hoveringTarget)
                {
                    currentHoverTime += Time.deltaTime;
                    if (currentHoverTime >= lockOnTimeRequired && lockedTarget != hitTarget)
                    {
                        lockedTarget = hitTarget; // LOCK ON
                        Debug.Log($"<color=green>[CombatSystem] Locked onto: {lockedTarget.Transform.name}</color>");
                    }
                }
                else
                {
                    hoveringTarget = hitTarget;
                    currentHoverTime = 0f;
                }
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

            if (GetActiveWeapon()?.Data is ProjectileWeaponData projData)
                projSpeed = projData.BulletForce;

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
                if (indicatorImage != null) indicatorImage.color = Color.red;
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

        float determinant = (b * b) - (4f * a * c);

        if (determinant > 0f)
        {
            float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a);
            float t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
            float t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);

            if (t > 0f) return targetPos + (targetVel * t);
        }

        return targetPos;
    }

    public void ProcessCombat(CombatInput input)
    {
        if (loadoutSlots.Count == 0) return;

        HandleAimingAndLockOn(input);

        if (input.NumberKeyMap >= 0 && input.NumberKeyMap < loadoutSlots.Count && input.NumberKeyMap != activeSlotIndex)
        {
            if (!GetActiveWeapon().IsReloading)
                activeSlotIndex = input.NumberKeyMap;
        }

        WeaponSlot activeWeapon = GetActiveWeapon();

        if ((input.Reload || (input.Shoot && activeWeapon.CurrentAmmo <= 0)) && !activeWeapon.IsReloading && activeWeapon.CurrentAmmo < activeWeapon.Data.MaxAmmo)
        {
            StartCoroutine(ReloadRoutine(activeWeapon));
            return;
        }

        if (input.Shoot && !activeWeapon.IsReloading && activeWeapon.CurrentAmmo > 0)
        {
            if (Time.time - activeWeapon.LastFireTime >= activeWeapon.Data.FireRate)
            {
                Fire(activeWeapon);
                activeWeapon.LastFireTime = Time.time;
            }
        }
    }

    private void Fire(WeaponSlot weapon)
    {
        weapon.CurrentAmmo--;

        WeaponContext context = new WeaponContext
        {
            Runner = this,
            PlayerRoot = characterRoot,
            ShootingPoint = shootingPoint,
            MuzzlePoint = muzzlePoint,
            CurrentTarget = lockedTarget?.Transform,
            TargetPoint = currentTargetPoint,
            Audio = audioManager
        };

        weapon.Data.PerformAttack(context);
    }

    private IEnumerator ReloadRoutine(WeaponSlot weapon)
    {
        weapon.IsReloading = true;
        yield return new WaitForSeconds(weapon.Data.ReloadTime);
        weapon.CurrentAmmo = weapon.Data.MaxAmmo;
        weapon.IsReloading = false;
    }
}