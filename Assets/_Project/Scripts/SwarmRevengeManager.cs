using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmRevengeManager : MonoBehaviour
{
    public static SwarmRevengeManager Instance { get; private set; }

    [Header("References")]
    public MosquitoSpawner mosquitoSpawner;

    [Header("Trigger Condition")]
    public int killsRequired = 5;
    public float timeWindow = 12f;
    public float cooldown = 30f;

    [Header("Revenge Swarm")]
    public int revengeMosquitoCount = 12;
    public float revengeDuration = 12f;

    public bool IsRevengeActive => isRevengeActive;

    public event Action OnRevengeStarted;
    public event Action OnRevengeEnded;

    private readonly Queue<float> killTimes = new Queue<float>();
    private float lastTriggerTime = -999f;
    private bool subscribed = false;
    private bool isRevengeActive = false;
    private Coroutine revengeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnMosquitoKilled -= RegisterKill;
        }
    }

    private void TrySubscribe()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnMosquitoKilled -= RegisterKill;
        GameManager.Instance.OnMosquitoKilled += RegisterKill;

        subscribed = true;
        Debug.Log("SwarmRevengeManager: subscribed to OnMosquitoKilled.");
    }

    private void RegisterKill()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlayerDead)
        {
            return;
        }

        float now = Time.time;

        killTimes.Enqueue(now);

        while (killTimes.Count > 0 && now - killTimes.Peek() > timeWindow)
        {
            killTimes.Dequeue();
        }

        Debug.Log("Recent mosquito kills: " + killTimes.Count);

        if (killTimes.Count >= killsRequired &&
            now - lastTriggerTime >= cooldown &&
            !isRevengeActive)
        {
            TriggerRevengeSwarm();
        }
    }

    private void TriggerRevengeSwarm()
    {
        lastTriggerTime = Time.time;
        killTimes.Clear();

        Debug.Log("蚊群的复仇触发！");

        if (mosquitoSpawner != null)
        {
            mosquitoSpawner.SpawnRevengeSwarm(revengeMosquitoCount);
        }
        else
        {
            Debug.LogWarning("SwarmRevengeManager: MosquitoSpawner is not assigned.");
        }

        if (revengeCoroutine != null)
        {
            StopCoroutine(revengeCoroutine);
        }

        revengeCoroutine = StartCoroutine(RevengeRoutine());
    }

    private IEnumerator RevengeRoutine()
    {
        isRevengeActive = true;
        OnRevengeStarted?.Invoke();

        yield return new WaitForSeconds(revengeDuration);

        isRevengeActive = false;
        OnRevengeEnded?.Invoke();

        revengeCoroutine = null;
    }
}
