using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Bullet : MonoBehaviour
{
    public int baseDmg;
    public Transform source;
    public GameObject p;

    public void OnTriggerEnter(Collider target)
    {
        if (target == null) return;

        print(target.name);

        Material mat = target.GetComponentInChildren<Renderer>().material;

        if (p)
        {
            GameObject particleObject = Instantiate(p, this.transform.position, Quaternion.identity);

            particleObject.GetComponent<ParticleSystemRenderer>().material = mat;
            particleObject.GetComponent<ParticleSystemRenderer>().trailMaterial = mat;
        }

        if (target.GetComponent<IDamage>() != null)
        {
            IDamage idamage = target.GetComponent<IDamage>();
            idamage.TakeDamage(baseDmg);
        }

        Destroy(this.gameObject);
    }
}
