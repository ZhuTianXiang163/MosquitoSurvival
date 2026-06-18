using TMPro;
using UnityEngine;

public class FloatingRewardText : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text text;

    [Header("Timing")]
    public float stayDuration = 2.0f;
    public float fadeDuration = 0.5f;

    [Header("Motion")]
    public float driftSpeed = 0.03f;

    private float timer;
    private Color originalColor;
    private Vector3 driftDirection;
    private Transform cameraTransform;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
        }

        if (text != null)
        {
            originalColor = text.color;
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        // 很轻微的漂移，不会快速飞走
        driftDirection = new Vector3(
            Random.Range(-0.2f, 0.2f),
            Random.Range(0.05f, 0.15f),
            Random.Range(-0.2f, 0.2f)
        ).normalized;
    }

    public void Setup(int amount)
    {
        if (text != null)
        {
            text.text = "+" + amount + "$";
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 只做非常轻微的漂移
        transform.position += driftDirection * driftSpeed * Time.deltaTime;

        FaceCamera();

        if (text != null)
        {
            float alpha = 1f;

            if (timer > stayDuration)
            {
                float fadeTimer = timer - stayDuration;
                alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
            }

            text.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                alpha
            );
        }

        if (timer >= stayDuration + fadeDuration)
        {
            Destroy(gameObject);
        }
    }

    private void FaceCamera()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 directionToCamera = cameraTransform.position - transform.position;

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera.normalized, Vector3.up);
        }
    }
}
