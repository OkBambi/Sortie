using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UniversalUISegments : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private GameObject owner;
    [SerializeField] private ResourceType resourceType = ResourceType.LeftAmmo;

    [Header("Containers")]
    [SerializeField] private Transform segmentsContainer;
    [SerializeField] private Transform ghostContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject segmentPrefab;

    [Header("Visuals")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color spentColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color refillColor;
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
    [SerializeField] private bool animateRefill = true;
    [SerializeField] private float refillDuration = 0.15f;
    [SerializeField] private float refillStartScale = 1.5f;

    [Header("Dynamic Sizing")]
    [SerializeField] private bool dynamicCellSizing = true;
    [Tooltip("The ammo count that your Grid Layout Group's current Cell Size is perfectly tuned for.")]
    [SerializeField] private int referenceAmmoCount = 30;
    [Tooltip("Max segments per row before wrapping down to a new row.")]
    [SerializeField] private int maxSegmentsPerRow = 10;
    [Tooltip("Maximum scale multiplier for cell sizes. Prevents low-ammo weapons from having massive segments.")]
    [SerializeField] private float maxCellScale = 2.0f;

    private IResourceProvider _resourceProvider;
    private List<Image> _spawnedSegments = new List<Image>();

    private int _currentMax = -1;
    private int _lastKnownCurrent = -1;

    private GridLayoutGroup _gridLayoutGroup;
    private Vector2 _baseCellSize;
    private Vector2 _baseSpacing;

    void Start()
    {
        if (ghostContainer == null && segmentsContainer != null)
        {
            ghostContainer = segmentsContainer.parent;
        }

        if (segmentsContainer != null)
        {
            _gridLayoutGroup = segmentsContainer.GetComponent<GridLayoutGroup>();
            if (_gridLayoutGroup != null)
            {
                _baseCellSize = _gridLayoutGroup.cellSize;
                _baseSpacing = _gridLayoutGroup.spacing;
            }
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

        if (dynamicCellSizing && _gridLayoutGroup != null && newMax > 0)
        {
            RectTransform rt = segmentsContainer.GetComponent<RectTransform>();

            float availableWidth = rt.rect.width - _gridLayoutGroup.padding.left - _gridLayoutGroup.padding.right;
            float availableHeight = rt.rect.height - _gridLayoutGroup.padding.top - _gridLayoutGroup.padding.bottom;

            int columns = Mathf.Min(newMax, maxSegmentsPerRow);
            int rows = Mathf.CeilToInt((float)newMax / columns);

            // Force the grid layout to wrap exactly at our calculated column count!
            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = columns;

            // Use exact container width if available, otherwise calculate the total width of your baseline configuration
            float targetTotalWidth = availableWidth > 0
                ? availableWidth
                : (maxSegmentsPerRow * _baseCellSize.x) + (Mathf.Max(0, maxSegmentsPerRow - 1) * _baseSpacing.x);

            // Scale factor relative to our target columns, but bounded so low ammo counts don't get huge
            float scaleFactor = (float)maxSegmentsPerRow / columns;
            scaleFactor = Mathf.Min(scaleFactor, maxCellScale);

            float newSpacingX = _baseSpacing.x * scaleFactor;
            float totalSpacingWidth = Mathf.Max(0, columns - 1) * newSpacingX;

            // Distribute remaining width, limit it using maxCellScale
            float rawCellWidth = Mathf.Max(0.01f, (targetTotalWidth - totalSpacingWidth) / columns);
            float newCellWidth = Mathf.Min(rawCellWidth, _baseCellSize.x * maxCellScale);

            // Use exact container height to prevent vertical overflow, and divide by rows if stacked
            float targetHeight = availableHeight > 0 ? availableHeight : _baseCellSize.y;
            float totalSpacingHeight = Mathf.Max(0, rows - 1) * _baseSpacing.y;
            float rawCellHeight = Mathf.Max(0.01f, (targetHeight - totalSpacingHeight) / rows);

            float newCellHeight = Mathf.Min(rawCellHeight, _baseCellSize.y * maxCellScale);

            _gridLayoutGroup.cellSize = new Vector2(newCellWidth, newCellHeight);
            _gridLayoutGroup.spacing = new Vector2(newSpacingX, _baseSpacing.y);
        }

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

        ghostRect.sizeDelta = new Vector2(originalImg.rectTransform.rect.width, originalImg.rectTransform.rect.height);
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

        img.color = refillColor;

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
            img.color = activeColor;
        }
    }
}