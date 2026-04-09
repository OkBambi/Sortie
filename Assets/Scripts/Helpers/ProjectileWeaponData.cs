using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileWeapon", menuName = "Combat/Weapons/Projectile")]
public class ProjectileWeaponData : WeaponData
{
    [Header("Projectile Stats")]
    public float BulletForce = 60f;
    public GameObject BulletPrefab;

    [Header("Projectile Visuals")]
    public GameObject MuzzleFlashPrefab;
    public ParticleSystem MuzzleSpit;
    public Material BulletTrailMaterial;

    public override void PerformAttack(WeaponContext ctx)
    {
        Vector3 targetPos = ctx.CurrentTarget != null ? ctx.CurrentTarget.position : ctx.TargetPoint;
        Vector3 direction = (targetPos - ctx.ShootingPoint.position);

        direction.y = 0f;
        direction.Normalize();

        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, ctx.MuzzlePoint.position, ctx.MuzzlePoint.rotation, ctx.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        if (MuzzleSpit != null) MuzzleSpit.Play();
        if (ctx.Audio != null) ctx.Audio.Play(FireSound);

        // Projectile Spawning
        GameObject bullet = Instantiate(BulletPrefab, ctx.MuzzlePoint.position, Quaternion.LookRotation(direction));
        bullet.GetComponent<Rigidbody>().linearVelocity = direction * BulletForce;

        var bLogic = bullet.GetComponent<Bullet>();
        if (bLogic != null)
        {
            bLogic.baseDmg = BaseDamage;
            bLogic.source = ctx.PlayerRoot;
        }

        Destroy(bullet, 5f);
    }
}