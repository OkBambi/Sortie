using UnityEngine;

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
            float modifier = instance.CurrentCharge / MaxChargeTime; 
            instance.TryFire(modifier);
            instance.CurrentCharge = 0f;
        }
    }
}