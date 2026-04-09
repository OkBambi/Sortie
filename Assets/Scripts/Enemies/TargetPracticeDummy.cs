using System.Collections;
using UnityEngine;

public class TargetPracticeDummy : BaseEnemy
{
    [Header("Strafe Settings")]
    public bool enableStrafing = false;
    public float strafeDistance = 3f;
    public float strafeSpeed = 2f;

    [Header("Visual Feedback")]
    public Renderer dummyRenderer;
    [Tooltip("The shader property name for the main color. Usually _BaseColor in URP or _Color")]
    public string colorPropertyName = "_BaseColor";
    public Color hitColor = new Color(1f, 0.5f, 0f);
    public float feedbackDuration = 0.5f;
    public float wobbleIntensity = 20f;

    private Vector3 startPosition;
    private Quaternion originalRotation;
    private Color originalColor = Color.white;
    private Coroutine feedbackCoroutine;

    protected override void Start()
    {
        base.Start();

        startPosition = transform.position;
        originalRotation = transform.localRotation;

        if (dummyRenderer == null) dummyRenderer = GetComponentInChildren<Renderer>();

        if (dummyRenderer != null)
        {
            if (dummyRenderer.material.HasProperty(colorPropertyName))
            {
                originalColor = dummyRenderer.material.GetColor(colorPropertyName);
            }
            else if (dummyRenderer.material.HasProperty("_Color"))
            {
                colorPropertyName = "_Color";
                originalColor = dummyRenderer.material.GetColor(colorPropertyName);
            }
            else
            {
                Debug.LogWarning($"Shader property '{colorPropertyName}' not found on {dummyRenderer.name}'s material. Color flash may not work!");
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        if (enableStrafing && strafeDistance > 0 && strafeSpeed > 0)
        {
            float offset = Mathf.Sin(Time.time * strafeSpeed) * strafeDistance;
            transform.position = startPosition + (transform.right * offset);
        }
    }

    public override void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);

        Debug.Log($"Dummy took {damage} damage! Remaining Health: {currentHealth}");

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);

        float randomSpeedX = Random.Range(30f, 45f);
        float randomSpeedZ = Random.Range(20f, 35f);
        float randomDir = Random.value > 0.5f ? 1f : -1f;

        feedbackCoroutine = StartCoroutine(VisualFeedbackRoutine(randomSpeedX, randomSpeedZ, randomDir));

        if (currentHealth <= 0)
        {
            Debug.Log("Dummy Destroyed!");
            // Destroy(gameObject);
        }
    }

    private IEnumerator VisualFeedbackRoutine(float speedX, float speedZ, float dirMult)
    {
        float elapsed = 0f;

        while (elapsed < feedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / feedbackDuration;

            if (dummyRenderer != null && dummyRenderer.material.HasProperty(colorPropertyName))
            {
                dummyRenderer.material.SetColor(colorPropertyName, Color.Lerp(hitColor, originalColor, t));
            }

            float damper = 1f - t;
            float wobbleX = Mathf.Sin(elapsed * speedX) * wobbleIntensity * damper * dirMult;
            float wobbleZ = Mathf.Cos(elapsed * speedZ) * (wobbleIntensity * 0.3f) * damper;

            transform.localRotation = originalRotation * Quaternion.Euler(wobbleX, 0f, wobbleZ);

            yield return null;
        }

        if (dummyRenderer != null && dummyRenderer.material.HasProperty(colorPropertyName))
        {
            dummyRenderer.material.SetColor(colorPropertyName, originalColor);
        }
        transform.localRotation = originalRotation;
    }
}