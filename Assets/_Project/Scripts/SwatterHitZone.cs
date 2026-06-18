using UnityEngine;

public class SwatterHitZone : MonoBehaviour
{
    [Header("Hit Settings")]
    public float minHitSpeed = 1.0f;

    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        currentSpeed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlayerDead)
        {
            return;
        }

        MosquitoTarget mosquito = other.GetComponentInParent<MosquitoTarget>();

        if (mosquito == null)
        {
            return;
        }

        if (currentSpeed < minHitSpeed)
        {
            Debug.Log("Touched mosquito, but swing too slow.");
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        mosquito.Hit(hitPoint);
    }
}
