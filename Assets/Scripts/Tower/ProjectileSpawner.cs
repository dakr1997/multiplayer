using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;

public class ProjectileSpawner : NetworkBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] public Transform TowerShootingPoint;
    [SerializeField] public float TowerDamage;
    
    // Pool settings
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;
    
    private IObjectPool<Projectile> projectilePool;

    private void Awake()
    {
        projectilePool = new ObjectPool<Projectile>(
            CreatePooledProjectile,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            collectionCheck,
            defaultCapacity,
            maxSize);
    }

    private Projectile CreatePooledProjectile()
    {
        Projectile projectile = Instantiate(projectilePrefab);
        projectile.ObjectPool = projectilePool;
        
        // Get or add NetworkObject if needed
        var netObj = projectile.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            netObj = projectile.gameObject.AddComponent<NetworkObject>();
        }
        
        return projectile;
    }

    private void OnTakeFromPool(Projectile projectile)
    {
        projectile.gameObject.SetActive(true);
        projectile.transform.SetPositionAndRotation(TowerShootingPoint.position, TowerShootingPoint.rotation);
    }

    private void OnReturnedToPool(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(Projectile projectile)
    {
        Destroy(projectile.gameObject);
    }

    public void SpawnProjectile(Vector3 direction)
    {
        if (!IsServer) return;
        
        Projectile projectile = projectilePool.Get();
        
        if (projectile.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (!netObj.IsSpawned)
            {
                netObj.Spawn(true);
            }
            
            projectile.Initialize(direction, TowerDamage, "Tower");
        }
        else
        {
            Debug.LogError("Missing NetworkObject on projectile prefab!");
            projectilePool.Release(projectile);
        }
    }
}