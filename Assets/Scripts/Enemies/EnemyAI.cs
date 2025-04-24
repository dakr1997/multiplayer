using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(HealthComponent), typeof(Rigidbody2D))]
public class EnemyAI : NetworkBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 3f;
    public bool isEnemy = true;

    [Header("Targeting")]
    private List<Transform> targets = new List<Transform>();
    private Vector2 randomDirection;
    private float directionChangeTime = 2f;
    private float lastDirectionChange;

    private Rigidbody2D rb;
    private HealthComponent health;

    public static event System.Action<EnemyAI> OnEnemySpawned;
    public static event System.Action<EnemyAI> OnEnemyDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<HealthComponent>();
        health.OnDied += HandleDeath;
    }

    private void Start()
    {
        OnEnemySpawned?.Invoke(this);
        randomDirection = Random.insideUnitCircle.normalized;
        FindTargets();
    }

    private void Update()
    {
        if (!IsServer) return;
        Move();
    }

    private void Move()
    {
        if (Time.time - lastDirectionChange > directionChangeTime)
        {
            randomDirection = Random.insideUnitCircle.normalized;
            lastDirectionChange = Time.time;
            directionChangeTime = Random.Range(0.5f, 2f);
        }

        Transform closestTarget = GetClosestTarget();
        Vector2 moveDirection = randomDirection;

        if (closestTarget != null)
        {
            Vector2 toTarget = ((Vector2)closestTarget.position - rb.position).normalized;
            moveDirection = (randomDirection + toTarget).normalized;
        }

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} has died.");
        OnEnemyDied?.Invoke(this);

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void FindTargets()
    {
        AddTargetsWithTag("Tower");
        AddTargetsWithTag("Player");
    }

    private void AddTargetsWithTag(string tag)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
        {
            if (obj != null) AddTarget(obj.transform);
        }
    }

    private void AddTarget(Transform target)
    {
        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    private Transform GetClosestTarget()
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        Vector2 currentPosition = rb.position;

        foreach (Transform target in targets)
        {
            if (target == null) continue;
            float dist = Vector2.Distance(currentPosition, target.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = target;
            }
        }

        return closest;
    }
}
