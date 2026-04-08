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
        Vector3 direction = ctx.CurrentTarget != null ?
            (ctx.CurrentTarget.position - ctx.ShootingPoint.position).normalized :
            ctx.ShootingPoint.forward;

        // Visuals
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

        // Trails
        if (BulletTrailMaterial != null)
        {
            Vector3 endPos = Physics.Raycast(ctx.ShootingPoint.position, direction, out RaycastHit hit, Range)
                ? hit.point
                : ctx.MuzzlePoint.position + direction * Range;

            ctx.Runner.StartCoroutine(CreateBulletTrail(ctx.MuzzlePoint.position, endPos));
        }
    }

    private IEnumerator CreateBulletTrail(Vector3 start, Vector3 end)
    {
        yield return new WaitForSeconds(0.025f);
        GameObject line = new GameObject("BulletTrail");
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material = BulletTrailMaterial;

        Destroy(line, 0.02f);
    }
}