using UnityEngine;
using Unity.Netcode;

public class Projectile : PoolableNetworkObject
{
    // Configuration
    [SerializeField] private LayerMask collisionLayers;
    
    // State
    private Vector3 startPos;
    private Vector3 direction;
    private float damage;
    private string source;
    private bool hasHit = false;
    private ProjectileData data;
    private int hitCount = 0;
    private GameObject owner; // Reference to the owner game object
    private int ownerInstanceID; // Store the owner's instance ID for comparison
    
    public void Initialize(Vector3 direction, ProjectileData projectileData, string source, GameObject owner = null)
    {
        this.direction = direction.normalized;
        this.data = projectileData;
        this.owner = owner;
        this.ownerInstanceID = owner != null ? owner.GetInstanceID() : -1;
        
        if (projectileData == null)
        {
            Debug.LogError("ProjectileData is null in Initialize!");
            this.damage = 10f; // Default damage
            hasHit = true; // Prevent movement
            return;
        }
        
        this.damage = projectileData.damage;
        this.source = source;
        this.startPos = transform.position;
        this.hasHit = false;
        this.hitCount = 0;
        
        Debug.Log($"Projectile initialized: Direction={direction}, Speed={projectileData.speed}, Position={startPos}");
        
        // Reset any rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        if (owner != null)
        {
            Debug.Log($"Projectile will ignore collisions with owner: {owner.name}");
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Check if properly initialized
            if (data == null)
            {
                Debug.LogError("Projectile spawned without ProjectileData!");
                ReturnToPool(0.1f);
            }
        }
    }
    
    public override void OnSpawn()
    {
        base.OnSpawn();
        hasHit = false;
        hitCount = 0;
    }
    
    private void Update()
    {
        if (!IsServer || hasHit) return;
        
        if (data == null)
        {
            Debug.LogError($"Projectile {gameObject.name} has null ProjectileData!");
            hasHit = true;
            ReturnToPool(0.1f);
            return;
        }
        
        // Move
        Vector3 moveAmount = direction * data.speed * Time.deltaTime;
        transform.Translate(moveAmount, Space.World);
        
        // Only log occasionally to avoid spamming console
        if (Time.frameCount % 20 == 0)
        {
            Debug.Log($"Projectile moving: Position={transform.position}, Movement={moveAmount}, Direction={direction}, Speed={data.speed}");
        }
        
        // Check distance
        if (Vector3.Distance(transform.position, startPos) > data.maxDistance)
        {
            Debug.Log($"Projectile max distance reached: {data.maxDistance}");
            hasHit = true;
            ReturnToPool(0.1f);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasHit) return;
        
        // Skip if colliding with owner
        if (owner != null && other.gameObject.GetInstanceID() == ownerInstanceID)
        {
            Debug.Log($"Skipping collision with owner: {other.gameObject.name}");
            return;
        }
        
        // Skip if colliding with objects in the same tower layer (assuming tower and projectile are on the same layer)
        // Only if they're not on the enemy layer
        if (other.gameObject.layer == gameObject.layer && 
            !other.CompareTag("Enemy"))
        {
            Debug.Log($"Skipping collision with friendly object: {other.gameObject.name}");
            return;
        }
        
        Debug.Log($"Projectile trigger with {other.gameObject.name}, Layer: {other.gameObject.layer}");
        
        if (((1 << other.gameObject.layer) & collisionLayers.value) != 0)
        {
            // Apply damage
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, source);
                Debug.Log($"Applied {damage} damage to {other.gameObject.name}");
            }
            
            // Effects
            PlayHitEffectsClientRpc(other.transform.position);
            
            // Increment hit count
            hitCount++;
            
            // Check if we should despawn
            if (!data.piercing || hitCount >= data.maxTargets)
            {
                hasHit = true;
                ReturnToPool(0.1f);
            }
        }
    }
    
    [ClientRpc]
    private void PlayHitEffectsClientRpc(Vector3 position)
    {
        // Spawn hit effect if we have one
        if (data != null && data.hitEffectPrefab != null)
        {
            Instantiate(data.hitEffectPrefab, position, Quaternion.identity);
            Debug.Log("Spawned hit effect");
        }
    }
}