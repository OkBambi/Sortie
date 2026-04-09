using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileWeapon", menuName = "Combat/Weapons/Projectile")]
public class ProjectileWeaponData : WeaponData
{
    [Header("Projectile Stats")]
    public float BulletForce = 60f;
    public float BulletLifetime = 3f;

    [Tooltip("MUST NOT HAVE rigidbodies or colliders. Visuals only!")]
    public GameObject BulletVisualPrefab;

    [Header("Projectile Visuals")]
    public GameObject MuzzleFlashPrefab;
    public ParticleSystem MuzzleSpit;
    public Material BulletTrailMaterial;

    [Tooltip("The particle effect to spawn upon hitting a target")]
    public GameObject HitParticlePrefab;

    public override void PerformAttack(WeaponContext ctx)
    {
        Vector3 direction = (ctx.TargetPoint - ctx.ShootingPoint.position).normalized;

        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, ctx.MuzzlePoint.position, ctx.MuzzlePoint.rotation, ctx.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        if (MuzzleSpit != null) MuzzleSpit.Play();
        if (ctx.Audio != null) ctx.Audio.Play(FireSound);

        Vector3 velocity = direction * BulletForce;

        ProjectileManager.Instance.SpawnProjectile(
            ctx.MuzzlePoint.position,
            velocity,
            BaseDamage,
            BulletLifetime,
            ctx.PlayerRoot,
            BulletVisualPrefab,
            HitParticlePrefab
        );
    }
}