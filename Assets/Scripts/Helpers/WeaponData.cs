using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string WeaponName = "Weapon";

    [Header("Base Stats")]
    public int MaxAmmo = 25;
    public int BaseDamage = 8;
    public float Range = 100f;
    public float FireRate = 0.04f;
    public float ReloadTime = 1.5f;

    [Header("Base Visuals")]
    public string FireSound = "Shot";

    public abstract void PerformAttack(WeaponContext context);
}