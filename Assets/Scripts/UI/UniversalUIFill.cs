using UnityEngine;
using UnityEngine.UI;

public class UniversalUIFill : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The bar that snaps instantly to the current value.")]
    [SerializeField] private Image frontFillImage;
    [Tooltip("The bar that smoothly lerps to the current value behind the front bar.")]
    [SerializeField] private Image lerpFillImage;

    [Header("Data Source")]
    [SerializeField] private GameObject owner;
    [SerializeField] private ResourceType resourceType = ResourceType.Health;

    [Header("Fill Settings")]
    [Tooltip("The maximum fillAmount this image should reach. So, if 0.3, 100% health = 0.3 fillAmount")]
    [Range(0f, 1f)]
    [SerializeField] private float maxFillLimit = 1.0f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Color Zones (Front Bar)")]
    [SerializeField] private Color normalColor = Color.white;

    [Space]
    [Tooltip("When the resource drops below this percentage, it turns the Warning Color.")]
    [Range(0f, 1f)]
    [SerializeField] private float warningThreshold = 0.5f;
    [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0f); 

    [Space]
    [Tooltip("When the resource drops below this percentage, it turns the Critical Color.")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalThreshold = 0.15f;
    [SerializeField] private Color criticalColor = Color.red;

    private IResourceProvider _resourceProvider;
    private float _currentDisplayedPercent = 1f;

    void Start()
    {
        SetOwner(owner);
    }

    void Update()
    {
        if (_resourceProvider == null) return;

        float targetPercent = _resourceProvider.GetResourcePercentage(resourceType);

        float targetFillAmount = targetPercent * maxFillLimit;

        if (frontFillImage != null)
        {
            frontFillImage.fillAmount = targetFillAmount;

            if (targetPercent <= criticalThreshold)
            {
                frontFillImage.color = criticalColor;
            }
            else if (targetPercent <= warningThreshold)
            {
                frontFillImage.color = warningColor;
            }
            else
            {
                frontFillImage.color = normalColor;
            }
        }

        if (lerpFillImage != null)
        {
            if (targetFillAmount > _currentDisplayedPercent)
            {
                _currentDisplayedPercent = targetFillAmount;
            }
            else
            {
                _currentDisplayedPercent = Mathf.Lerp(_currentDisplayedPercent, targetFillAmount, Time.deltaTime * smoothSpeed);
            }

            lerpFillImage.fillAmount = _currentDisplayedPercent;
        }
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
        if (owner != null)
        {
            _resourceProvider = owner.GetComponentInParent<IResourceProvider>();

            if (_resourceProvider != null)
            {
                _currentDisplayedPercent = _resourceProvider.GetResourcePercentage(resourceType) * maxFillLimit;
            }
        }
        else
        {
            _resourceProvider = null;
        }
    }
}