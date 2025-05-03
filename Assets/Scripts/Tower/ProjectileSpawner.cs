using UnityEngine;
using Unity.Netcode;

public class ProjectileSpawner : NetworkBehaviour
{
    // Configuration
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform shootingPoint;
    
    // Properties
    public Transform ShootingPoint => shootingPoint;
    public ProjectileData ProjectileData { get; set; }
    
    // State
    private NetworkObjectPool poolManager;
    private GameObject owner; // Reference to the owner game object
    
    private void Awake()
    {
        poolManager = NetworkObjectPool.Instance;
        if (poolManager == null)
        {
            Debug.LogError("NetworkObjectPool instance not found!");
        }
        
        if (shootingPoint == null)
        {
            shootingPoint = transform;
            Debug.Log("No shooting point assigned, using self transform.");
        }
        
        // Get the owner game object (usually the Tower)
        owner = transform.parent != null ? transform.parent.gameObject : gameObject;
        
        if (owner != null)
        {
            Debug.Log($"Found owner: {owner.name}");
        }
    }
    
    public void SetShootingPoint(Transform point)
    {
        if (point == null)
        {
            Debug.LogError("Trying to set null shooting point!");
            return;
        }
        
        shootingPoint = point;
        Debug.Log($"Shooting point set to {point.name}");
    }
    
    /// <summary>
    /// Spawns a projectile in the specified direction
    /// </summary>
    public void SpawnProjectile(Vector3 direction)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SpawnProjectile called on client!");
            return;
        }
        
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab not assigned!");
            return;
        }
        
        if (ProjectileData == null)
        {
            Debug.LogError("ProjectileData is null in ProjectileSpawner!");
            return;
        }
        
        if (poolManager == null)
        {
            Debug.LogError("NetworkObjectPool is null!");
            return;
        }
        
        Debug.Log($"Spawning projectile with ProjectileData: {ProjectileData.projectileName}, Speed: {ProjectileData.speed}");
        
        // Get from pool
        NetworkObject projectileNetObj = poolManager.Get(projectilePrefab);
        if (projectileNetObj == null)
        {
            Debug.LogError("Failed to get projectile from pool!");
            return;
        }
        
        // Initialize projectile
        if (projectileNetObj.TryGetComponent<Projectile>(out var projectile))
        {
            // Position
            projectile.transform.position = shootingPoint.position;
            projectile.transform.rotation = shootingPoint.rotation;
            
            // Initialize with owner to ignore
            projectile.Initialize(direction, ProjectileData, gameObject.name, owner);
            
            // Spawn on network
            if (!projectileNetObj.IsSpawned)
            {
                projectileNetObj.Spawn(true);
                Debug.Log($"Projectile spawned on network at {shootingPoint.position}, direction {direction}");
            }
            else
            {
                Debug.Log("Projectile already spawned on network");
            }
        }
        else
        {
            Debug.LogError("Projectile component missing on prefab!");
            poolManager.Release(projectilePrefab, projectileNetObj);
        }
    }
}