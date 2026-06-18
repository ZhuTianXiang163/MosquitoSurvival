using UnityEngine;

public class MosquitoMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Wander Around Spawn")]
    public float wanderSpeed = 0.6f;
    public float wanderRadius = 2.0f;
    public float wanderRetargetInterval = 1.3f;

    [Header("Buzz Around Player")]
    public float detectDistance = 5.0f;
    public float buzzSpeed = 1.0f;
    public float buzzRetargetInterval = 0.8f;

    [Header("Distance Around Player")]
    public float normalMinDistance = 1.2f;
    public float normalMaxDistance = 2.5f;

    [Range(0f, 1f)]
    public float closePassChance = 0.18f;

    public float closeMinDistance = 0.35f;
    public float closeMaxDistance = 0.8f;

    [Header("Vertical Offset Around Head")]
    public float verticalOffsetMin = -0.35f;
    public float verticalOffsetMax = 0.45f;

    [Header("Height Limit")]
    public float minHeight = 0.8f;
    public float maxHeight = 2.2f;

    [Header("Small Random Wiggle")]
    public float noiseStrength = 0.18f;
    public float noiseSpeed = 4.0f;

    private Vector3 spawnPosition;
    private Vector3 currentTarget;
    private float retargetTimer;
    private float randomSeed;

    void Start()
    {
        spawnPosition = transform.position;
        randomSeed = Random.Range(0f, 1000f);

        if (playerTarget == null && Camera.main != null)
        {
            playerTarget = Camera.main.transform;
        }

        PickWanderTarget();
    }

    void Update()
    {
        if (playerTarget == null)
        {
            WanderAroundSpawn();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= detectDistance)
        {
            BuzzAroundPlayer();
        }
        else
        {
            WanderAroundSpawn();
        }
    }

    void WanderAroundSpawn()
    {
        retargetTimer += Time.deltaTime;

        MoveToward(currentTarget, wanderSpeed);

        if (retargetTimer >= wanderRetargetInterval ||
            Vector3.Distance(transform.position, currentTarget) < 0.15f)
        {
            PickWanderTarget();
            retargetTimer = 0f;
        }
    }

    void BuzzAroundPlayer()
    {
        retargetTimer += Time.deltaTime;

        Vector3 noisyTarget = currentTarget + GetNoiseOffset();
        noisyTarget.y = Mathf.Clamp(noisyTarget.y, minHeight, maxHeight);

        MoveToward(noisyTarget, buzzSpeed);

        if (retargetTimer >= buzzRetargetInterval ||
            Vector3.Distance(transform.position, currentTarget) < 0.2f)
        {
            PickPlayerBuzzTarget();
            retargetTimer = 0f;
        }
    }

    void PickWanderTarget()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            Random.Range(-0.4f, 0.4f),
            Random.Range(-wanderRadius, wanderRadius)
        );

        currentTarget = spawnPosition + randomOffset;
        currentTarget.y = Mathf.Clamp(currentTarget.y, minHeight, maxHeight);
    }

    void PickPlayerBuzzTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle;

        if (randomCircle.sqrMagnitude < 0.001f)
        {
            randomCircle = Vector2.right;
        }

        randomCircle.Normalize();

        bool doClosePass = Random.value < closePassChance;

        float distance = doClosePass
            ? Random.Range(closeMinDistance, closeMaxDistance)
            : Random.Range(normalMinDistance, normalMaxDistance);

        Vector3 horizontalOffset = new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        ) * distance;

        float verticalOffset = Random.Range(verticalOffsetMin, verticalOffsetMax);

        currentTarget = playerTarget.position + horizontalOffset + Vector3.up * verticalOffset;
        currentTarget.y = Mathf.Clamp(currentTarget.y, minHeight, maxHeight);
    }

    Vector3 GetNoiseOffset()
    {
        return new Vector3(
            Mathf.Sin(Time.time * noiseSpeed + randomSeed) * noiseStrength,
            Mathf.Sin(Time.time * noiseSpeed * 1.3f + randomSeed) * noiseStrength,
            Mathf.Cos(Time.time * noiseSpeed + randomSeed) * noiseStrength
        );
    }

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 oldPosition = transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        Vector3 moveDirection = transform.position - oldPosition;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
        }
    }
}
