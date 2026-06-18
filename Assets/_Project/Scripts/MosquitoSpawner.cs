using System.Collections.Generic;
using UnityEngine;

public class MosquitoSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject mosquitoPrefab;

    [Header("Spawn Zones")]
    public MosquitoSpawnZone[] spawnZones;

    [Header("Spawn Settings")]
    public int maxMosquitoCount = 4;
    public float spawnInterval = 3.0f;
    public int initialSpawnCount = 1;
    public float spawnPauseAfterClear = 20f;

    private float spawnTimer;
    private float spawnPauseTimer;
    private readonly List<GameObject> spawnedMosquitoes = new List<GameObject>();

    public void PauseSpawning(float duration)
    {
        spawnPauseTimer = duration;
        spawnTimer = 0f; // Reset spawn timer so first spawn happens after pause
        Debug.Log($"MosquitoSpawner: spawning paused for {duration}s, spawnTimer reset");
    }

    public void SpawnRevengeSwarm(int count)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsDeadOrDying)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnMosquito(isRevengeMosquito: true);
        }
    }

    void Start()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnMosquito();
        }
    }

    void Update()
    {
        CleanupDeadMosquitoes();

        // Handle spawn pause (after spray) - count down even during death
        if (spawnPauseTimer > 0)
        {
            spawnPauseTimer -= Time.deltaTime;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsDeadOrDying)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval && spawnPauseTimer <= 0)
        {
            spawnTimer = 0f;

            int aliveCount = CountAliveMosquitoes();

            if (aliveCount < maxMosquitoCount)
            {
                SpawnMosquito();
            }
        }
    }

    int CountAliveMosquitoes()
    {
        int count = 0;

        foreach (GameObject mosquito in spawnedMosquitoes)
        {
            if (mosquito == null)
            {
                continue;
            }

            MosquitoTarget target = mosquito.GetComponent<MosquitoTarget>();

            if (target != null && !target.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    void SpawnMosquito(bool isRevengeMosquito = false)
    {
        if (mosquitoPrefab == null)
        {
            Debug.LogWarning("MosquitoSpawner: mosquitoPrefab is not assigned.");
            return;
        }

        MosquitoSpawnZone zone = PickZoneByWeight();

        if (zone == null)
        {
            Debug.LogWarning("MosquitoSpawner: no valid spawn zone.");
            return;
        }

        GameObject mosquito = Instantiate(
            mosquitoPrefab,
            zone.GetRandomPoint(),
            Quaternion.identity
        );

        mosquito.SetActive(true);
        spawnedMosquitoes.Add(mosquito);

        if (isRevengeMosquito)
        {
            ApplyRevengeSettings(mosquito);
        }
    }

    void ApplyRevengeSettings(GameObject mosquito)
    {
        MosquitoMovement movement = mosquito.GetComponent<MosquitoMovement>();

        if (movement != null)
        {
            movement.detectDistance = 20f;
            movement.buzzSpeed = 1.8f;
            movement.buzzRetargetInterval = 0.45f;

            movement.normalMinDistance = 0.7f;
            movement.normalMaxDistance = 1.5f;

            movement.closePassChance = 0.45f;
            movement.closeMinDistance = 0.25f;
            movement.closeMaxDistance = 0.65f;

            movement.noiseStrength = 0.25f;
            movement.noiseSpeed = 6f;
        }

        MosquitoBite bite = mosquito.GetComponent<MosquitoBite>();

        if (bite != null)
        {
            bite.biteDistance = 0.75f;
            bite.bitePrepareTime = 0.25f;
            bite.biteCooldown = 1.4f;
            bite.biteDamage = 10;
        }
    }


    MosquitoSpawnZone PickZoneByWeight()
    {
        if (spawnZones == null || spawnZones.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;

        foreach (MosquitoSpawnZone zone in spawnZones)
        {
            if (zone != null && zone.weight > 0f)
            {
                totalWeight += zone.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (MosquitoSpawnZone zone in spawnZones)
        {
            if (zone == null || zone.weight <= 0f)
            {
                continue;
            }

            cumulative += zone.weight;

            if (randomValue <= cumulative)
            {
                return zone;
            }
        }

        return null;
    }

    void CleanupDeadMosquitoes()
    {
        for (int i = spawnedMosquitoes.Count - 1; i >= 0; i--)
        {
            GameObject mosquito = spawnedMosquitoes[i];

            if (mosquito == null)
            {
                spawnedMosquitoes.RemoveAt(i);
                continue;
            }

            MosquitoTarget target = mosquito.GetComponent<MosquitoTarget>();

            if (target != null && target.IsDead)
            {
                spawnedMosquitoes.RemoveAt(i);
            }
        }
    }
}
