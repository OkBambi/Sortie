using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AmmoMeterUI : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Drag the Player's CombatSystem here to track ammo")]
    [SerializeField] private CombatSystem combatSystem;

    [Header("Prefab & Container")]
    [Tooltip("The parent transform that will hold all the ejected segments. Should match the RectTransform of your main Ammo bar.")]
    [SerializeField] private Transform segmentsContainer;
    [Tooltip("The hollow circle Image prefab. MUST have the same Fill Origin and Size as the main Ammo bar!")]
    [SerializeField] private GameObject segmentPrefab;

    [Header("Main Bar Synchronization")]
    [Tooltip("This must exactly match the Max Fill Limit on your UniversalUIFill script (e.g. 0.3)")]
    [Range(0f, 1f)]
    [SerializeField] private float maxFillLimit = 0.3f;
    [Tooltip("The base Z-rotation of your main ammo bar (usually 0 if you didn't rotate the container)")]
    [SerializeField] private float startAngleOffset = 0f;
    [Tooltip("Does your main ammo bar fill Clockwise?")]
    [SerializeField] private bool fillClockwise = true;

    [Space]
    [Tooltip("Which direction does your Fill Origin point? (Bottom = 0, -1, 0 | Top = 0, 1, 0 | Right = 1, 0, 0 | Left = -1, 0, 0)")]
    [SerializeField] private Vector3 fillOriginDirection = Vector3.down;

    [Header("Ejection Animation")]
    [SerializeField] private Color activeColor = Color.white;
    [Tooltip("Shrinks the flying ghost slightly so it looks like a distinct shell casing breaking off. Set to 0 for a perfectly flush slice.")]
    [SerializeField] private float ghostGapAngle = 1.0f;
    [Tooltip("How far outward the segment flies when spent.")]
    [SerializeField] private float ejectDistance = 30f;
    [Tooltip("How long the ejection animation lasts.")]
    [SerializeField] private float ejectDuration = 0.3f;

    private int currentTrackingMaxAmmo = -1;
    private int lastKnownAmmo = -1;

    void Update()
    {
        if (combatSystem == null) return;

        WeaponSlot activeWeapon = combatSystem.GetActiveWeapon();
        if (activeWeapon == null || activeWeapon.Data == null)
        {
            lastKnownAmmo = 0;
            currentTrackingMaxAmmo = 0;
            return;
        }

        int currentAmmo = activeWeapon.CurrentAmmo;
        int maxAmmo = activeWeapon.Data.MaxAmmo;

        if (maxAmmo != currentTrackingMaxAmmo)
        {
            currentTrackingMaxAmmo = maxAmmo;
            lastKnownAmmo = currentAmmo;
        }

        else if (currentAmmo < lastKnownAmmo)
        {
            for (int i = currentAmmo; i < lastKnownAmmo; i++)
            {
                EjectSegment(i, maxAmmo);
            }
            lastKnownAmmo = currentAmmo;
        }

        else if (currentAmmo > lastKnownAmmo)
        {
            lastKnownAmmo = currentAmmo;
        }
    }

    private void EjectSegment(int index, int maxAmmo)
    {
        float segmentFill = maxFillLimit / maxAmmo;
        float segmentAngle = 360f * segmentFill;

        float zAngle = startAngleOffset + ((fillClockwise ? -1f : 1f) * index * segmentAngle);

        GameObject ghostObj = Instantiate(segmentPrefab, segmentsContainer);
        ghostObj.SetActive(true);

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        Image ghostImg = ghostObj.GetComponent<Image>();

        float finalFillAngle = segmentAngle - ghostGapAngle;
        ghostImg.fillAmount = Mathf.Max(finalFillAngle / 360f, 0.001f);
        ghostImg.color = activeColor;

        float gapOffset = (fillClockwise ? -1f : 1f) * (ghostGapAngle / 2f);
        ghostRect.localPosition = Vector3.zero;
        ghostRect.localScale = Vector3.one;
        ghostRect.localRotation = Quaternion.Euler(0f, 0f, zAngle + gapOffset);

        float centerZAngle = zAngle + ((fillClockwise ? -1f : 1f) * (segmentAngle / 2f));
        StartCoroutine(EjectRoutine(ghostObj, ghostRect, ghostImg, centerZAngle));
    }

    private IEnumerator EjectRoutine(GameObject ghostObj, RectTransform rect, Image img, float centerZAngle)
    {
        Vector3 startPos = rect.localPosition;
        Color startColor = img.color;

        Vector3 outwardDirection = Quaternion.Euler(0, 0, centerZAngle) * fillOriginDirection;
        Vector3 endPos = startPos + (outwardDirection * ejectDistance);

        float elapsed = 0f;

        while (elapsed < ejectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ejectDuration;

            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            rect.localPosition = Vector3.Lerp(startPos, endPos, easeT);

            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0f, t);
            img.color = newColor;

            yield return null;
        }

        Destroy(ghostObj);
    }
}