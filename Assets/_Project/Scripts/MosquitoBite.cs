using UnityEngine;

public class MosquitoBite : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Bite Settings")]
    public float biteDistance = 0.6f;
    public float bitePrepareTime = 0.5f;
    public float biteCooldown = 2.0f;
    public int biteDamage = 10;

    [Header("Feedback")]
    public AudioClip biteSound;
    public float biteSoundVolume = 0.6f;

    private float closeTimer = 0f;
    private float cooldownTimer = 0f;
    private MosquitoTarget mosquitoTarget;

    private void Start()
    {
        mosquitoTarget = GetComponent<MosquitoTarget>();

        if (playerTarget == null && Camera.main != null)
        {
            playerTarget = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsDeadOrDying)
        {
            return;
        }


        if (mosquitoTarget != null && mosquitoTarget.IsDead)
        {
            return;
        }

        if (playerTarget == null)
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= biteDistance)
        {
            closeTimer += Time.deltaTime;

            if (closeTimer >= bitePrepareTime && cooldownTimer <= 0f)
            {
                Bite();
                cooldownTimer = biteCooldown;
                closeTimer = 0f;
            }
        }
        else
        {
            closeTimer = 0f;
        }
    }

    private void Bite()
    {
        // 花露水免疫检查
        if (MosquitoImmunityController.Instance != null &&
            MosquitoImmunityController.Instance.IsImmune)
        {
            Debug.Log("Mosquito bite blocked: player is immune.");
            return;
        }

        Debug.Log("Mosquito bite!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(biteDamage);
        }

        if (biteSound != null)
        {
            AudioSource.PlayClipAtPoint(biteSound, transform.position, biteSoundVolume);
        }
    }
}
