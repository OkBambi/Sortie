using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CombatInput
{
    public bool Shoot;
    public bool Reload;
    public int NumberKeyMap; // E.g., 1, 2, 3 to switch weapons
}

// Data passed to the weapon so it knows where to spawn things and can run Coroutines
public struct WeaponContext
{
    public MonoBehaviour Runner;       // For starting coroutines (like trails/delays)
    public Transform PlayerRoot;       // To know who is attacking
    public Transform ShootingPoint;    // The origin of aim
    public Transform MuzzlePoint;      // Where visuals spawn
    public Transform CurrentTarget;    // The locked-on target (if any)
    public AudioManager Audio;         // To play sounds
}

// Runtime state of a specific weapon in a slot
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
    [SerializeField] private Transform characterRoot; 
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Shooting Points")]
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform muzzlePoint;

    [Header("Loadout")]
    [SerializeField] private List<WeaponData> initialLoadout;
    private List<WeaponSlot> loadoutSlots = new List<WeaponSlot>();
    private int activeSlotIndex = 0;

    private Transform currentTarget;
    private AudioManager audioManager;

    void Start()
    {
        audioManager = FindAnyObjectByType<AudioManager>();

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

    public void UpdateAim(Vector3 targetPoint, Transform targetTransform = null)
    {
        var activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || characterRoot == null) return;

        currentTarget = targetTransform;

        Vector3 direction = (targetPoint - characterRoot.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void ProcessCombat(CombatInput input)
    {
        if (loadoutSlots.Count == 0) return;

        // Weapon Switching
        if (input.NumberKeyMap >= 0 && input.NumberKeyMap < loadoutSlots.Count && input.NumberKeyMap != activeSlotIndex)
        {
            if (!GetActiveWeapon().IsReloading)
                activeSlotIndex = input.NumberKeyMap;
        }

        WeaponSlot activeWeapon = GetActiveWeapon();

        // Reload Logic
        if ((input.Reload || (input.Shoot && activeWeapon.CurrentAmmo <= 0)) && !activeWeapon.IsReloading && activeWeapon.CurrentAmmo < activeWeapon.Data.MaxAmmo)
        {
            StartCoroutine(ReloadRoutine(activeWeapon));
            return;
        }

        // Firing Logic
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

        // Build the context package
        WeaponContext context = new WeaponContext
        {
            Runner = this,
            PlayerRoot = characterRoot,
            ShootingPoint = shootingPoint,
            MuzzlePoint = muzzlePoint,
            CurrentTarget = currentTarget,
            Audio = audioManager
        };

        weapon.Data.PerformAttack(context);
    }

    private IEnumerator ReloadRoutine(WeaponSlot weapon)
    {
        weapon.IsReloading = true;
        // TODO: UI System will read weapon.IsReloading to show the circle

        yield return new WaitForSeconds(weapon.Data.ReloadTime);

        weapon.CurrentAmmo = weapon.Data.MaxAmmo;
        weapon.IsReloading = false;
    }
}