using UnityEngine;

public class MosquitoClearUtility : MonoBehaviour
{
    public static MosquitoClearUtility Instance { get; private set; }

    [SerializeField] private GameObject clearEffectPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ClearAllMosquitoes()
    {
        MosquitoTarget[] targets = FindObjectsOfType<MosquitoTarget>();
        int clearedCount = 0;

        foreach (MosquitoTarget target in targets)
        {
            if (target == null || target.IsDead)
            {
                continue;
            }

            if (clearEffectPrefab != null)
            {
                Instantiate(clearEffectPrefab, target.transform.position, Quaternion.identity);
            }

            Destroy(target.gameObject);
            clearedCount++;
        }

        // Pause spawning for 20 seconds after clearing
        MosquitoSpawner spawner = FindObjectOfType<MosquitoSpawner>();
        if (spawner != null)
        {
            spawner.PauseSpawning(20f);
            Debug.Log($"MosquitoClearUtility: called PauseSpawning(20f) on spawner");
        }
        else
        {
            Debug.LogError("MosquitoClearUtility: No MosquitoSpawner found in scene!");
        }

        Debug.Log($"MosquitoClearUtility: cleared {clearedCount} mosquitoes, respawn paused 20s");
    }
}
