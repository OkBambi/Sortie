using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerObjective : MonoBehaviour
{
    [Tooltip("The ID of the objective to update")]
    [SerializeField] private string objectiveId;

    [Tooltip("How much progress to add")]
    [SerializeField] private int progressAmount = 1;

    [Tooltip("Should this trigger disable itself after being used once?")]
    [SerializeField] private bool triggerOnce = true;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player != null)
        {
            if (SortieManager.Instance != null)
            {
                SortieManager.Instance.UpdateObjective(objectiveId, progressAmount);

                Debug.Log($"Player hit trigger! Updated objective '{objectiveId}' by {progressAmount}.");

                if (triggerOnce)
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("SortieManager instance is not found in the scene!");
            }
        }
    }
}