using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;

public class ProjectileSpawner : NetworkBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] public Transform TowerShootingPoint;
    [SerializeField] public float TowerDamage;
    
    private NetworkObjectPool poolManager;

    private void Awake()
    {
        poolManager = FindObjectOfType<NetworkObjectPool>();
    }

    public void SpawnProjectile(Vector3 direction)
    {
        if (!IsServer) return;
        
        var projectile = poolManager.Get(projectilePrefab.GetComponent<NetworkObject>())?.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.transform.SetPositionAndRotation(TowerShootingPoint.position, TowerShootingPoint.rotation);
            projectile.Initialize(direction, TowerDamage, "Tower");
        }
    }
}