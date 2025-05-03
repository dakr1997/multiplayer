using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private EnemyData enemyData;
    
    // Components
    private Rigidbody2D rb;
    private HealthComponent health;
    
    // State
    private Vector2 randomDirection;
    private float lastDirectionChangeTime;
    private List<Transform> potentialTargets = new List<Transform>();
    
    // Network variables
    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    
    // Events
    public static event System.Action<EnemyAI> OnEnemySpawned;
    public static event System.Action<EnemyAI> OnEnemyDied;
    
    // Properties
    public EnemyData Data => enemyData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<HealthComponent>();
        
        if (health != null)
        {
            health.OnDied += HandleDeath;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        // Initialize with data from the scriptable object
        if (enemyData != null)
        {
            // Set up the enemy based on data
            if (health != null)
            {
                // Health will be initialized in HealthComponent
            }
        }
        
        // Initial state
        randomDirection = Random.insideUnitCircle.normalized;
        lastDirectionChangeTime = Time.time;
        
        // Find targets
        FindTargets();
        
        // Trigger event
        OnEnemySpawned?.Invoke(this);
    }

    private void Update()
    {
        if (!IsServer) return;
        
        // Update direction periodically
        if (Time.time - lastDirectionChangeTime > Random.Range(1f, 3f))
        {
            randomDirection = Random.insideUnitCircle.normalized;
            lastDirectionChangeTime = Time.time;
            
            // Refresh targets
            FindTargets();
        }
        
        // Move the enemy
        MoveEnemy();
    }

    private void MoveEnemy()
    {
        // Get closest target
        Transform closestTarget = GetClosestTarget();
        Vector2 moveDirection;
        
        if (closestTarget != null)
        {
            // Calculate direction to target
            Vector2 toTarget = ((Vector2)closestTarget.position - rb.position).normalized;
            
            // Combine random and target directions
            moveDirection = (randomDirection * enemyData.randomMovementWeight + 
                            toTarget * enemyData.targetSeekingWeight).normalized;
            
            // Update network variable for clients
            targetPosition.Value = closestTarget.position;
        }
        else
        {
            // No target, use random direction
            moveDirection = randomDirection;
        }
        
        // Apply movement
        rb.linearVelocity = moveDirection * enemyData.moveSpeed;
    }

    private void FindTargets()
    {
        potentialTargets.Clear();
        
        // Find towers and players
        AddTargetsWithTag("Tower");
        AddTargetsWithTag("Player");
    }

    private void AddTargetsWithTag(string tag)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
        {
            if (obj != null)
            {
                AddTarget(obj.transform);
            }
        }
    }

    private void AddTarget(Transform target)
    {
        if (!potentialTargets.Contains(target))
        {
            potentialTargets.Add(target);
        }
    }

    private Transform GetClosestTarget()
    {
        if (potentialTargets.Count == 0) return null;
        
        Transform closest = null;
        float minDist = float.MaxValue;
        Vector2 currentPosition = rb.position;

        foreach (Transform target in potentialTargets)
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

    private void HandleDeath()
    {
        if (!IsServer) return;
        
        // Trigger event
        OnEnemyDied?.Invoke(this);
        
        // XP award is now handled by XPManager listening to OnEnemyDied
    }
    
    public override void OnNetworkDespawn()
    {
        if (health != null)
        {
            health.OnDied -= HandleDeath;
        }
        
        base.OnNetworkDespawn();
    }
}