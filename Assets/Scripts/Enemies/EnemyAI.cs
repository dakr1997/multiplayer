using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class EnemyAI : NetworkBehaviour, IDamageable
{
    // Health variables
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isEnemy = true; // Flag to identify this as an enemy
    
    // Movement variables
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    private List<Transform> targets = new List<Transform>(); // Reference to the target points e.g. Towers, Player, etc.
    private int currentPoint = 0;
    private Vector2 randomDirection;
    private float directionChangeTime = 2f;
    private float lastDirectionChange;
    private Rigidbody2D rb;

    public bool IsAlive => currentHealth > 0f;

    public static event System.Action<EnemyAI> OnEnemySpawned;
    public static event System.Action<EnemyAI> OnEnemyDied;

    private void Start()
    {
        OnEnemySpawned?.Invoke(this);
        rb = GetComponent<Rigidbody2D>();

        FindTargets(); // Find all targets in the scene
        randomDirection = Random.insideUnitCircle.normalized;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, string source = null)
    {
        Debug.Log($"TakeDamage called on {gameObject.name} with amount: {amount} and source: {source}");
        if (!IsServer) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage from {source}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");

        if (IsServer)
        {
            OnEnemyDied?.Invoke(this);
        }

        GetComponent<NetworkObject>().Despawn();

            // Despawn the enemy from the network
            GetComponent<NetworkObject>().Despawn();
        }

    private void FindTargets()
    {
        GameObject[] towerObjects = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject tower in towerObjects)
        {
            AddTarget(tower.transform);
        }

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in playerObjects)
        {
            AddTarget(player.transform);
        }
    }

    private void Update()
    {
        if (!IsServer) return; // Only server controls AI
        Move(); // Call the Move function to move the enemy
    }

    void AddTarget(Transform target)
    {
        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    void RemoveTarget(Transform target)
    {
        if (targets.Contains(target))
        {
            targets.Remove(target);
        }
    }

    void Move()
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

    Transform GetClosestTarget()
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
