using UnityEngine;

[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "Combat/Weapons/Melee")]
public class MeleeWeaponData : WeaponData
{
    [Header("Melee Stats")]
    public float HitRadius = 2f;
    public LayerMask EnemyLayer;

    [Header("Melee Visuals")]
    public GameObject SwingVFXPrefab;

    public override void PerformAttack(WeaponContext ctx)
    {
        if (ctx.Audio != null) ctx.Audio.Play(FireSound); 

        if (SwingVFXPrefab != null)
        {
            GameObject vfx = Instantiate(SwingVFXPrefab, ctx.MuzzlePoint.position, ctx.PlayerRoot.rotation, ctx.PlayerRoot);
            Destroy(vfx, 0.5f);
        }

        Vector3 hitCenter = ctx.PlayerRoot.position + (ctx.PlayerRoot.forward * (HitRadius * 0.5f));
        Collider[] hitEnemies = Physics.OverlapSphere(hitCenter, HitRadius, EnemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log($"Melee hit: {enemy.name} for {BaseDamage} damage!");
        }
    }
}