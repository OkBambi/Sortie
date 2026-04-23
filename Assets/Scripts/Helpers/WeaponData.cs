using UnityEngine;

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

public class WeaponInstance
{
    public WeaponData Data;
    public WeaponContext Context;

    public int CurrentAmmo;
    public bool IsReloading;

    public float LastFireTime;
    public float CurrentCharge;

    public void Initialize(WeaponData data, WeaponContext context)
    {
        Data = data;
        Context = context;
        CurrentAmmo = data.MaxAmmo;
        IsReloading = false;
        LastFireTime = 0f;
        CurrentCharge = 0f;
    }

    public void UpdateInput(bool isDown, bool isHeld, bool isUp)
    {
        if (Data.TriggerModule != null)
        {
            Data.TriggerModule.HandleInput(this, isDown, isHeld, isUp);
        }
    }

    public void TryFire(float chargeModifier = 1f)
    {
        if (CurrentAmmo <= 0 || IsReloading) return;

        CurrentAmmo--;
        LastFireTime = Time.time;

        if (Data.FireSound != null && Context.Audio != null)
        {
            Context.Audio.PlaySound(Data.FireSound);
        }

        if (Data.EmitterModule != null)
        {
            Data.EmitterModule.Fire(this, chargeModifier);
        }
    }
}

[CreateAssetMenu(fileName = "NewModularWeapon", menuName = "Combat/Weapons/Modular Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string WeaponName = "Weapon";

    [Header("Ammo Stats")]
    public int MaxAmmo = 25;
    public float ReloadTime = 1.5f;

    [Header("Modules")]
    [Tooltip("Controls WHEN it fires (e.g., Auto, Charge, Spooling)")]
    public WeaponTrigger TriggerModule;

    [Tooltip("Controls WHAT happens (e.g., Projectile, Hitscan, Melee)")]
    public WeaponEmitter EmitterModule;

    [Header("Audio")]
    public Sound FireSound;
    public Sound ReloadSound;
}

// 4. BASE MODULES
public abstract class WeaponTrigger : ScriptableObject
{
    public abstract void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp);
}

public abstract class WeaponEmitter : ScriptableObject
{
    public abstract void Fire(WeaponInstance instance, float chargeModifier);
}