using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public struct CombatInput
{
    public bool Shoot;
    public bool Reload;
}

public class PlayerGun : MonoBehaviour
{
    [Header("Aiming")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float rotationSpeed = 10f;
    private Transform currentTarget;
    private Vector2 lookInput;
    [Space]
    [Header("Shooting")]
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Material bulletTrailMaterial;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ParticleSystem bulletSpit;
    [Space]
    [Header("Current Weapon")]
    [SerializeField] private int maxAmmo = 25;
    [SerializeField] private int currentAmmo;
    [SerializeField] private int baseDmg = 8;
    [SerializeField] private float bulletForce = 60f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float timeBetweenShots = 0.04f;
    [SerializeField] private float reloadTime = 1.5f;
    [Space]
    [Header("Weapon Modified Stats")]
    [SerializeField] public int M_maxAmmo;
    [SerializeField] public int M_baseDmg;
    [SerializeField] public float M_bulletForce;
    [SerializeField] public float M_range;
    [SerializeField] public float M_timeBetweenShots;
    [SerializeField] public float M_reloadTime;
    [Space]
    [Header("Visuals")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image reloadCircle;

    private float currentReloadTime;

    private bool _requestedShoot;
    private bool isFiring = false;

    private bool _requestedReload;
    private bool isReloading = false;

    AudioManager audioManager;

    public void ResetAmmo()
    {
        currentAmmo = maxAmmo;
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        audioManager = GameObject.FindAnyObjectByType<AudioManager>();
        ResetStats();
    }

    public void ResetStats()
    {
        M_maxAmmo = maxAmmo;
        M_baseDmg = baseDmg;
        M_bulletForce = bulletForce;
        M_range = range;
        M_timeBetweenShots = timeBetweenShots;
        M_reloadTime = reloadTime;
    }

    public void RotateGunTowardsMouse()
    {
        if (playerCamera == null || playerRoot == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.GetComponent<ITarget>() != null)
            {
                if (Vector3.Distance(transform.position, hit.transform.position) <= M_range)
                    currentTarget = hit.transform;
                else
                    currentTarget = null;
            }
            else
            {
                currentTarget = null;
            }
        }

        Vector3 targetPoint = currentTarget ? currentTarget.position : ray.GetPoint(M_range);
        Vector3 direction = (targetPoint - playerRoot.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void UseWeapon(CombatInput input)
    {
        _requestedShoot = input.Shoot && !isReloading;

        _requestedReload = input.Reload;

        if (_requestedShoot && !isFiring && !isReloading)
        {
            StartCoroutine(ContinuousFire());
        }

        if (_requestedShoot && !isReloading && currentAmmo == 0)
        {
            StartCoroutine(Reload());
        }

        if (_requestedReload && !isReloading)
        {
            StartCoroutine(Reload());
        }

        //ammoText.text = currentAmmo + "/" + M_maxAmmo;
    }

    IEnumerator ContinuousFire()
    {
        isFiring = true;

        while (_requestedShoot)
        {
            if (currentAmmo > 0)
            {
                GameObject m = Instantiate(muzzleFlash, muzzlePoint.position, muzzlePoint.rotation);
                m.transform.parent = muzzlePoint;
                Destroy(m, 0.05f);

                FireBullet();
                currentAmmo--;
            }
            else
            {
                _requestedShoot = false;
            }

            yield return new WaitForSeconds(M_timeBetweenShots);
        }

        isFiring = false;
    }

    private void FireBullet()
    {
        Vector3 direction;

        if (currentTarget != null)
        {
            direction = (currentTarget.transform.position - shootingPoint.position).normalized;
        }
        else
        {
            direction = shootingPoint.forward;
        }

        GameObject b = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(direction));
        b.GetComponent<Rigidbody>().linearVelocity = direction * M_bulletForce;
        b.GetComponent<Bullet>().baseDmg = M_baseDmg;
        b.GetComponent<Bullet>().source = GetComponentInParent<PlayerMovement>().transform;

        bulletSpit.Play();

        Destroy(b, 5f);

        RaycastHit hit;
        if (Physics.Raycast(shootingPoint.position, direction, out hit, Mathf.Infinity))
        {
            StartCoroutine(CreateBulletTrail(hit.point));
        }
        else
        {
            StartCoroutine(CreateBulletTrail(muzzlePoint.position + direction * M_range));
        }

        if (audioManager == null)
        {
            audioManager = FindAnyObjectByType<AudioManager>();
        }
        audioManager.Play("Shot");
    }

    IEnumerator CreateBulletTrail(Vector3 endPosition)
    {
        yield return new WaitForSeconds(0.025f);
        GameObject line = new GameObject("BulletTrail");
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, muzzlePoint.position);
        lr.SetPosition(1, endPosition);
        lr.material = bulletTrailMaterial;

        Destroy(line, 0.02f);
    }

    IEnumerator Reload()
    {
        Debug.Log("Reloading...");
        isReloading = true;
        ammoText.gameObject.SetActive(false);
        reloadCircle.gameObject.SetActive(true);
        reloadCircle.fillAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < M_reloadTime)
        {
            elapsedTime += Time.deltaTime;
            reloadCircle.fillAmount = elapsedTime / M_reloadTime;
            yield return null;
        }

        reloadCircle.fillAmount = 1f;
        currentAmmo = M_maxAmmo;
        isReloading = false;
        reloadCircle.gameObject.SetActive(false);
        ammoText.gameObject.SetActive(true);
        Debug.Log("Done Reloading.");
    }
}