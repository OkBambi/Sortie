using UnityEngine;

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