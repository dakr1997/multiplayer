using UnityEngine;
using Unity.Netcode;

public class ProjectileSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] public Transform TowerShootingPoint;
    [SerializeField] public float TowerDamage;
    
    private NetworkObjectPool poolManager;

    private void Awake()
    {
        poolManager = FindObjectOfType<NetworkObjectPool>();
        if (poolManager == null)
        {
            Debug.LogError("NetworkObjectPool not found in scene!");
        }
    }

    public void SpawnProjectile(Vector3 direction)
    {
        if (!IsServer) return;
        
        NetworkObject projectileNetObj = poolManager.Get(projectilePrefab.GetComponent<NetworkObject>());
        if (projectileNetObj == null)
        {
            Debug.LogError("Failed to get projectile from pool!");
            return;
        }

        Projectile projectile = projectileNetObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("Projectile component missing on prefab!");
            poolManager.Release(projectilePrefab.GetComponent<NetworkObject>(), projectileNetObj);
            return;
        }

        // Set position/rotation
        projectile.transform.SetPositionAndRotation(TowerShootingPoint.position, TowerShootingPoint.rotation);
        
        // Initialize
        projectile.Initialize(direction, TowerDamage, "Tower");
        
        // Spawn on network if needed
        if (!projectileNetObj.IsSpawned)
        {
            projectileNetObj.Spawn(true);
        }
    }
}