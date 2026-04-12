using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Swarm Missile")]
public class SwarmMissileEmitter : WeaponEmitter
{
    public int BaseDamage = 15;

    [Header("Volley Settings")]
    public int MissilesPerVolley = 4;
    public float DelayBetweenMissiles = 0.05f;

    [Header("Flight Dynamics")]
    public float MaxSpeed = 80f;
    public float Acceleration = 60f;
    public float InitialEjectSpeed = 15f;
    public float SpreadAngle = 60f;
    public float UpwardEjectBias = 1.5f;

    [Header("Itano Zigzag (The Circus)")]
    public float HomingDelay = 0.4f;
    public float TurnSpeed = 250f;
    public float ZigzagIntensity = 4f;
    public float ZigzagFrequency = 4f;
    public float SnapSharpness = 20f;

    [Header("Explosion")]
    public float ExplosionRadius = 5f;

    [Header("Visuals")]
    public GameObject MissilePrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject HitParticlePrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        if (MissilesPerVolley <= 1)
        {
            SpawnSingleMissile(instance, chargeModifier);
        }
        else
        {
            instance.Context.Runner.StartCoroutine(FireVolleyRoutine(instance, chargeModifier));
        }
    }

    private System.Collections.IEnumerator FireVolleyRoutine(WeaponInstance instance, float chargeModifier)
    {
        for (int i = 0; i < MissilesPerVolley; i++)
        {
            SpawnSingleMissile(instance, chargeModifier);

            if (DelayBetweenMissiles > 0)
            {
                yield return new WaitForSeconds(DelayBetweenMissiles);
            }
        }
    }

    private void SpawnSingleMissile(WeaponInstance instance, float chargeModifier)
    {
        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation, instance.Context.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        // Call the centralized manager
        ItanoCircusHandler.Instance.SpawnMissile(
            instance.Context.MuzzlePoint.position,
            instance.Context.MuzzlePoint.rotation,
            instance.Context.CurrentTarget,
            finalDamage,
            MaxSpeed,
            Acceleration,
            InitialEjectSpeed,
            SpreadAngle,
            UpwardEjectBias,
            HomingDelay,
            TurnSpeed,
            ZigzagIntensity,
            ZigzagFrequency,
            SnapSharpness,
            ExplosionRadius,
            MissilePrefab,
            HitParticlePrefab
        );
    }
}