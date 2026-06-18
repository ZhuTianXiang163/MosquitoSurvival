using UnityEngine;

public class MosquitoImmunityController : MonoBehaviour
{
    public static MosquitoImmunityController Instance { get; private set; }

    private float remainingTime;

    public bool IsImmune => remainingTime > 0f;
    public float RemainingTime => Mathf.Max(0f, remainingTime);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
        }
    }

    public void Activate(float duration)
    {
        remainingTime = Mathf.Max(remainingTime, duration);
        Debug.Log($"MosquitoImmunityController: immunity activated for {duration} seconds");
    }
}
