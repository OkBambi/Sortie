using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Triggers/Auto")]
public class AutoTrigger : WeaponTrigger
{
    public float FireRate = 0.1f;
    public bool RequiresRepeatedClicks = false;

    public override void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp)
    {
        bool shouldFire = RequiresRepeatedClicks ? inputDown : inputHeld;

        if (shouldFire && Time.time - instance.LastFireTime >= FireRate)
        {
            instance.TryFire(1f);
        }
    }
}

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

[CreateAssetMenu(menuName = "Combat/Modules/Triggers/Charged")]
public class ChargeTrigger : WeaponTrigger
{
    public float MaxChargeTime = 2f;
    public bool AutoFireAtMaxCharge = false;

    public override void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp)
    {
        if (inputHeld)
        {
            instance.CurrentCharge = Mathf.Clamp(instance.CurrentCharge + Time.deltaTime, 0f, MaxChargeTime);

            if (AutoFireAtMaxCharge && instance.CurrentCharge >= MaxChargeTime)
            {
                instance.TryFire(1f);
                instance.CurrentCharge = 0f;
            }
        }
        else if (inputUp && instance.CurrentCharge > 0)
        {
            float modifier = instance.CurrentCharge / MaxChargeTime; // Returns 0.0 to 1.0
            instance.TryFire(modifier);
            instance.CurrentCharge = 0f;
        }
    }
}