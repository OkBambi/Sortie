using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UniversalUIValueText : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private GameObject owner;
    [SerializeField] private ResourceType resourceType = ResourceType.LeftAmmo;

    [Header("UI Assets")]
    [SerializeField] private TextMeshProUGUI textComponent;

    private IResourceProvider _resourceProvider;

    void Start()
    {
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
        }
    }

    void Update()
    {
        if (_resourceProvider == null) return;

        int currentAmount = _resourceProvider.GetResourceCurrent(resourceType);
        int maxAmount = _resourceProvider.GetResourceMax(resourceType);

        textComponent.text = currentAmount.ToString() + "|" + maxAmount.ToString();
    }
}
