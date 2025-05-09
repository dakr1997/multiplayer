// Location: Core/Towers/Projectiles/ProjectileSpawner.cs
using UnityEngine;
using Unity.Netcode;

namespace Core.Towers.Projectiles
{
    public class ProjectileSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject projectilePrefab;
        [SerializeField] private Transform shootingPoint;
        [SerializeField] private ProjectileData defaultProjectileData;
        
        public Transform ShootingPoint => shootingPoint;
        public ProjectileData ProjectileData { get; set; }
        
        private NetworkObjectPool poolManager;
        private GameObject owner;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            poolManager = NetworkObjectPool.Instance;
            
            if (shootingPoint == null)
                shootingPoint = transform;
            
            // Set default projectile data if available
            if (ProjectileData == null && defaultProjectileData != null)
                ProjectileData = defaultProjectileData;
            
            // Get the owner game object (usually the Tower)
            owner = transform.parent != null ? transform.parent.gameObject : gameObject;
        }
        
        public void SetShootingPoint(Transform point)
        {
            if (point != null)
                shootingPoint = point;
        }
        
        /// <summary>
        /// Spawns a projectile in the specified direction
        /// </summary>
        public void SpawnProjectile(Vector3 direction)
        {
            if (!IsValidToSpawn())
                return;
            
            NetworkObject projectileNetObj = poolManager.Get(projectilePrefab);
            if (projectileNetObj == null)
                return;
            
            if (projectileNetObj.TryGetComponent<Projectile>(out var projectile))
            {
                ConfigureProjectile(projectile, direction);
                SpawnOnNetwork(projectileNetObj);
            }
            else
            {
                poolManager.Release(projectilePrefab, projectileNetObj);
            }
        }
        
        private bool IsValidToSpawn()
        {
            return IsServer && 
                   projectilePrefab != null && 
                   ProjectileData != null && 
                   poolManager != null;
        }
        
        private void ConfigureProjectile(Projectile projectile, Vector3 direction)
        {
            projectile.transform.position = shootingPoint.position;
            projectile.transform.rotation = shootingPoint.rotation;
            projectile.Initialize(direction, ProjectileData, gameObject.name, owner);
        }
        
        private void SpawnOnNetwork(NetworkObject projectileNetObj)
        {
            if (!projectileNetObj.IsSpawned)
                projectileNetObj.Spawn(true);
        }
    }
}