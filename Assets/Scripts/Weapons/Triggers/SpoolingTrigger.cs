using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Triggers/Spooling")]
public class SpoolingTrigger : WeaponTrigger
{
    public float MinFireRate = 0.5f;
    public float MaxFireRate = 0.05f;
    public float TimeToMaxSpool = 2f;
    public float SpoolCooldownSpeed = 1f;

    public override void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp)
    {
        if (inputHeld)
        {
            instance.CurrentCharge = Mathf.Clamp01(instance.CurrentCharge + (Time.deltaTime / TimeToMaxSpool));

            float currentFireRate = Mathf.Lerp(MinFireRate, MaxFireRate, instance.CurrentCharge);

            if (Time.time - instance.LastFireTime >= currentFireRate)
            {
                instance.TryFire(1f);
            }
        }
        else
        {
            instance.CurrentCharge = Mathf.Clamp01(instance.CurrentCharge - (Time.deltaTime * SpoolCooldownSpeed));
        }
    }
}