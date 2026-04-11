using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UniversalUISegments : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private GameObject owner;
    [SerializeField] private ResourceType resourceType = ResourceType.Ammo;

    [Header("Containers")]
    [Tooltip("The parent holding a Grid or LayoutGroup component.")]
    [SerializeField] private Transform segmentsContainer;
    [Tooltip("Where ejected segments fly. Usually just the canvas or the parent of the LayoutGroup so they break free of the grid.")]
    [SerializeField] private Transform ghostContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject segmentPrefab;

    [Header("Visuals")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color spentColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private bool hideEmptySegments = false;

    [Header("Ejection Animation")]
    [SerializeField] private Vector2 ejectDirection = new Vector2(1f, 1f);
    [SerializeField] private float ejectDistance = 50f;
    [SerializeField] private float ejectDuration = 0.35f;
    [Tooltip("Adds a random arc to the ejection direction")]
    [SerializeField] private float randomSpreadAngle = 25f;
    [Tooltip("Makes the UI element spin as it ejects")]
    [SerializeField] private float randomTorqueAngle = 90f;

    [Header("Refill Animation")]
    [Tooltip("Makes the segment pop/snap into place when refilled.")]
    [SerializeField] private bool animateRefill = true;
    [SerializeField] private float refillDuration = 0.15f;
    [Tooltip("How large the segment starts before snapping into place (1 = normal size)")]
    [SerializeField] private float refillStartScale = 1.5f;

    private IResourceProvider _resourceProvider;
    private List<Image> _spawnedSegments = new List<Image>();

    private int _currentMax = -1;
    private int _lastKnownCurrent = -1;

    void Start()
    {
        if (ghostContainer == null && segmentsContainer != null)
        {
            ghostContainer = segmentsContainer.parent;
        }

        SetOwner(owner);
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
        if (owner != null)
        {
            _resourceProvider = owner.GetComponentInParent<IResourceProvider>();
        }
        else
        {
            _resourceProvider = null;
            ClearSegments();
        }
    }

    void Update()
    {
        if (_resourceProvider == null || segmentsContainer == null || segmentPrefab == null) return;

        int currentAmount = _resourceProvider.GetResourceCurrent(resourceType);
        int maxAmount = _resourceProvider.GetResourceMax(resourceType);

        if (maxAmount != _currentMax)
        {
            RebuildSegments(maxAmount);
            _currentMax = maxAmount;
            _lastKnownCurrent = currentAmount;
            UpdateSegmentColors(currentAmount);
            return;
        }

        if (currentAmount < _lastKnownCurrent)
        {
            for (int i = currentAmount; i < _lastKnownCurrent; i++)
            {
                if (i >= 0 && i < _spawnedSegments.Count)
                {
                    EjectSegment(i);
                }
            }
            _lastKnownCurrent = currentAmount;
        }

        else if (currentAmount > _lastKnownCurrent)
        {
            UpdateSegmentColors(currentAmount);

            if (_lastKnownCurrent >= 0 && animateRefill)
            {
                for (int i = _lastKnownCurrent; i < currentAmount; i++)
                {
                    if (i < _spawnedSegments.Count)
                    {
                        StartCoroutine(RefillClickRoutine(_spawnedSegments[i]));
                    }
                }
            }

            _lastKnownCurrent = currentAmount;
        }
    }

    private void RebuildSegments(int newMax)
    {
        ClearSegments();

        for (int i = 0; i < newMax; i++)
        {
            GameObject segObj = Instantiate(segmentPrefab, segmentsContainer);
            segObj.SetActive(true);
            Image img = segObj.GetComponent<Image>();

            if (img != null)
            {
                _spawnedSegments.Add(img);
            }
        }
    }

    private void ClearSegments()
    {
        foreach (var img in _spawnedSegments)
        {
            if (img != null) Destroy(img.gameObject);
        }
        _spawnedSegments.Clear();
    }

    private void UpdateSegmentColors(int currentAmount)
    {
        for (int i = 0; i < _spawnedSegments.Count; i++)
        {
            Image img = _spawnedSegments[i];
            if (i < currentAmount)
            {
                img.gameObject.SetActive(true);
                img.color = activeColor;
            }
            else
            {
                img.color = spentColor;
                img.gameObject.SetActive(!hideEmptySegments);
            }
        }
    }

    private void EjectSegment(int index)
    {
        Image originalImg = _spawnedSegments[index];

        originalImg.color = spentColor;
        originalImg.gameObject.SetActive(!hideEmptySegments);

        GameObject ghostObj = Instantiate(segmentPrefab, ghostContainer);
        ghostObj.SetActive(true);

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        Image ghostImg = ghostObj.GetComponent<Image>();

        ghostImg.color = activeColor;
        ghostRect.position = originalImg.rectTransform.position;
        ghostRect.rotation = originalImg.rectTransform.rotation;
        ghostRect.sizeDelta = originalImg.rectTransform.sizeDelta;
        ghostRect.pivot = originalImg.rectTransform.pivot;

        float spreadZ = Random.Range(-randomSpreadAngle, randomSpreadAngle);
        Vector3 randomDirection = Quaternion.Euler(0, 0, spreadZ) * ejectDirection.normalized;

        float torque = Random.Range(-randomTorqueAngle, randomTorqueAngle);

        StartCoroutine(EjectRoutine(ghostObj, ghostRect, ghostImg, randomDirection, torque));
    }

    private IEnumerator EjectRoutine(GameObject ghostObj, RectTransform rect, Image img, Vector3 dir, float torque)
    {
        Vector3 startPos = rect.localPosition;
        Vector3 endPos = startPos + (dir * ejectDistance);

        Quaternion startRot = rect.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, torque);

        Color startColor = img.color;
        float elapsed = 0f;

        while (elapsed < ejectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ejectDuration;

            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            rect.localPosition = Vector3.Lerp(startPos, endPos, easeT);
            rect.localRotation = Quaternion.Lerp(startRot, endRot, easeT);

            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0f, t);
            img.color = newColor;

            yield return null;
        }

        Destroy(ghostObj);
    }

    private IEnumerator RefillClickRoutine(Image img)
    {
        if (img == null) yield break;

        RectTransform rect = img.rectTransform;
        Vector3 targetScale = Vector3.one;
        Vector3 startScale = Vector3.one * refillStartScale;

        float elapsed = 0f;

        while (elapsed < refillDuration)
        {
            if (img == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / refillDuration;

            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            rect.localScale = Vector3.Lerp(startScale, targetScale, easeT);

            yield return null;
        }

        if (img != null)
        {
            rect.localScale = targetScale;
        }
    }
}