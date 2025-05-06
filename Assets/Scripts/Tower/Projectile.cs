using UnityEngine;
using Unity.Netcode;
using Core.Interfaces;
public class Projectile : PoolableNetworkObject
{
    [SerializeField] private LayerMask collisionLayers;
    
    // Movement state
    private Vector3 startPos;
    private Vector3 direction;
    private ProjectileData data;
    private bool hasHit = false;
    
    // Collision state
    private string source;
    private float damage;
    private int hitCount = 0;
    private GameObject owner;
    private int ownerInstanceID = -1;
    
    // Properties
    public bool HasHit => hasHit;
    
    public void Initialize(Vector3 direction, ProjectileData projectileData, string source, GameObject owner = null)
    {
        // Basic validation
        if (projectileData == null)
        {
            hasHit = true; // Prevent movement
            return;
        }
        
        // Set properties
        this.direction = direction.normalized;
        this.data = projectileData;
        this.damage = projectileData.damage;
        this.source = source;
        this.startPos = transform.position;
        this.hasHit = false;
        this.hitCount = 0;
        
        // Store owner for collision ignoring
        this.owner = owner;
        this.ownerInstanceID = owner != null ? owner.GetInstanceID() : -1;
        
        // Reset rigidbody if present
        ResetRigidbody();
    }
    
    private void ResetRigidbody()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer && data == null)
            ReturnToPool(0.1f);
    }
    
    public override void OnSpawn()
    {
        base.OnSpawn();
        hasHit = false;
        hitCount = 0;
    }
    
    private void Update()
    {
        if (!IsServer || hasHit || data == null) 
            return;
        
        MoveProjectile();
        CheckMaxDistance();
    }
    
    private void MoveProjectile()
    {
        Vector3 moveAmount = direction * data.speed * Time.deltaTime;
        transform.Translate(moveAmount, Space.World);
    }
    
    private void CheckMaxDistance()
    {
        if (Vector3.Distance(transform.position, startPos) > data.maxDistance)
        {
            hasHit = true;
            ReturnToPool(0.1f);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasHit) 
            return;
        
        if (ShouldIgnoreCollision(other))
            return;
        
        if (IsValidCollisionLayer(other))
            HandleCollision(other);
    }
    
    private bool ShouldIgnoreCollision(Collider2D other)
    {
        // Ignore collisions with owner
        if (owner != null && other.gameObject.GetInstanceID() == ownerInstanceID)
            return true;
        
        // Ignore collisions with friendly objects
        if (other.gameObject.layer == gameObject.layer && !other.CompareTag("Enemy"))
            return true;
            
        return false;
    }
    
    private bool IsValidCollisionLayer(Collider2D other)
    {
        return ((1 << other.gameObject.layer) & collisionLayers.value) != 0;
    }
    
    private void HandleCollision(Collider2D other)
    {
        // Apply damage if target is damageable
        if (other.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(damage, source);
        
        // Play effects
        PlayHitEffectsClientRpc(other.transform.position);
        
        // Increment hit count
        hitCount++;
        
        // Check if projectile should be destroyed
        if (!data.piercing || hitCount >= data.maxTargets)
        {
            hasHit = true;
            ReturnToPool(0.1f);
        }
    }
    
    [ClientRpc]
    private void PlayHitEffectsClientRpc(Vector3 position)
    {
        if (data != null && data.hitEffectPrefab != null)
            Instantiate(data.hitEffectPrefab, position, Quaternion.identity);
    }
}