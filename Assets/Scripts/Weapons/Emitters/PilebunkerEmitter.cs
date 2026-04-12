using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Pilebunker")]
public class PilebunkerEmitter : WeaponEmitter
{
    public int BaseDamage = 150;
    public float HitRadius = 2.5f;
    public float ForwardOffset = 1.5f;
    public LayerMask EnemyLayer;

    [Header("Movement")]
    [Tooltip("Shoots the player forward when triggered")]
    public float ForwardDashForce = 40f;
    [Tooltip("Pushes the player backward due to recoil (if desired)")]
    public float RecoilForce = 0f;

    [Header("Visuals")]
    public GameObject StrikeVFXPrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        // Handle Movement (Dash / Recoil)
        if (instance.Context.PlayerRoot.TryGetComponent(out IWeaponForceReceiver forceReceiver))
        {
            Vector3 finalForce = Vector3.zero;

            if (ForwardDashForce > 0)
                finalForce += instance.Context.PlayerRoot.forward * ForwardDashForce;

            if (RecoilForce > 0)
                finalForce -= instance.Context.PlayerRoot.forward * RecoilForce;

            forceReceiver.ApplyWeaponForce(finalForce);
        }

        if (StrikeVFXPrefab != null)
        {
            GameObject vfx = Instantiate(StrikeVFXPrefab, instance.Context.MuzzlePoint.position, instance.Context.PlayerRoot.rotation, instance.Context.PlayerRoot);
            Destroy(vfx, 1f);
        }

        // Deal Damage
        Vector3 hitCenter = instance.Context.PlayerRoot.position + (instance.Context.PlayerRoot.forward * ForwardOffset);
        Collider[] hitEnemies = Physics.OverlapSphere(hitCenter, HitRadius, EnemyLayer);

        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(finalDamage);
            }
        }
    }
}