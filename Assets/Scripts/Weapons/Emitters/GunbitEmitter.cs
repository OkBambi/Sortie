using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Gunbit")]
public class GunbitEmitter : WeaponEmitter
{
    [Header("Gunbit Stats")]
    public int BaseHealth = 100;

    [Tooltip("Base offset applied to the dynamic wing formation.")]
    public Vector3 formationOffset;

    [Header("Drone Movement Dynamics")]
    [Tooltip("How fast the bit catches up to the owner (Lower = Snappier)")]
    public float followSmoothTime = 0.1f; 
    public float rotationSpeed = 20f;

    [Header("Organic Hover")]
    public float hoverSpeed = 3f;
    public float hoverAmplitude = 0.2f;

    [Header("Gunbit Payload")]
    public GameObject GunbitModel;

    [Tooltip("The Modular Weapon Data this drone will use.")]
    public WeaponData GunbitWeaponData;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        Transform moveRoot = instance.Context.PlayerRoot != null
            ? instance.Context.PlayerRoot
            : instance.Context.Runner.transform;


        Transform aimRoot = instance.Context.ShootingPoint != null
            ? instance.Context.ShootingPoint.parent
            : moveRoot;

        GunbitHandler.Instance.SpawnGunbit(
            moveRoot,
            aimRoot,
            formationOffset,
            followSmoothTime,
            rotationSpeed,
            hoverSpeed,
            hoverAmplitude,
            BaseHealth,
            GunbitModel,
            GunbitWeaponData
        );
    }
}