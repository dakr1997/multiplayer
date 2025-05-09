using UnityEngine;
using Unity.Netcode;
using Core.GameManagement;
using Core.WaveSystem;
using Core.Components;
using Core.Enemies;
using Core.Enemies.Base;
using System.Collections.Generic;
using Core.Towers.MainTower;
using Core.Towers.Projectiles;
using Core.Towers.Utilities;
public class Tower : NetworkBehaviour
{
    [SerializeField] private TowerData towerData;
    [SerializeField] private Transform shootPoint; // Set this in the prefab inspector
    [SerializeField] private Transform towerSprite; // Reference to the sprite transform for rotation management
    
    // Components
    private MainTowerHP health;
    private ProjectileSpawner projectileSpawner;
    private Aim aimSystem;
    
    // State variables
    private float fireCooldown = 0f;
    private GameObject currentTarget;
    
    // NEW: Target acquisition timing
    private float targetUpdateCooldown = 0f;
    private float targetUpdateInterval = 0.5f; // Update targets twice per second instead of every frame
    
    private void Awake()
    {
        health = GetComponent<MainTowerHP>();
        projectileSpawner = GetComponent<ProjectileSpawner>();
        
        // If no towerSprite is assigned, try to find it
        if (towerSprite == null)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                towerSprite = renderers[0].transform;
                Debug.Log($"Auto-assigned tower sprite: {towerSprite.name}");
            }
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        InitializeTower();
    }
    
    private void InitializeTower()
    {
        Debug.Log("Initializing Tower");
        
        fireCooldown = 0f;
        aimSystem = new Aim();
        
        if (projectileSpawner == null)
        {
            Debug.LogWarning("ProjectileSpawner not found, adding component.");
            projectileSpawner = gameObject.AddComponent<ProjectileSpawner>();
        }
        
        // Set up projectile spawner
        if (towerData != null)
        {
            if (towerData.projectileData == null)
            {
                Debug.LogError("TowerData.projectileData is null! Please assign a ProjectileData in the TowerData scriptable object.");
                return;
            }
            
            Debug.Log($"Setting ProjectileData: {towerData.projectileData.projectileName}, Speed: {towerData.projectileData.speed}");
            projectileSpawner.ProjectileData = towerData.projectileData;
            
            // Set up the shooting point
            if (shootPoint == null)
            {
                Debug.LogWarning("ShootPoint not assigned, using tower transform.");
                shootPoint = transform;
            }
            
            projectileSpawner.SetShootingPoint(shootPoint);
        }
        else
        {
            Debug.LogError("TowerData is null! Please assign a TowerData scriptable object to this Tower.");
        }
    }
    
    private void Update()
    {
        if (!IsServer || !health.IsAlive) return;
        
        fireCooldown += Time.deltaTime;
        targetUpdateCooldown += Time.deltaTime;
        
        // Only update targets periodically rather than every frame
        if (targetUpdateCooldown >= targetUpdateInterval)
        {
            UpdateTargets();
            targetUpdateCooldown = 0f;
        }
        
        // Only log target info occasionally to reduce log spam
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60 FPS
        {
            Debug.Log($"Tower Update: Current Target: {(currentTarget != null ? currentTarget.name : "None")}, " +
                     $"FireCooldown: {fireCooldown}, FireRate: {towerData.fireRate}");
        }
        
        if (fireCooldown >= 1f / towerData.fireRate && currentTarget != null)
        {
            Fire();
            fireCooldown = 0f;
        }
    }
    
    private void UpdateTargets()
    {
        if (currentTarget == null || 
            !currentTarget.activeInHierarchy || 
            Vector3.Distance(transform.position, currentTarget.transform.position) > towerData.attackRange)
        {
            // Lost current target or out of range, find a new one
            GameObject previousTarget = currentTarget;
            currentTarget = FindClosestEnemy();
            
            if (currentTarget != previousTarget)
            {
                // Reset aim system for the new target
                if (currentTarget != null)
                {
                    aimSystem = new Aim();
                    aimSystem.AddTarget(currentTarget.transform);
                    
                    Debug.Log($"Target changed to {currentTarget.name}");
                }
                else if (previousTarget != null)
                {
                    Debug.Log("Lost target");
                }
            }
        }
    }
    
    private GameObject FindClosestEnemy()
    {
        // Get EnemyManager first
        if (EnemyManager.Instance != null)
        {
            // Get enemies in range using the EnemyManager
            List<EnemyAI> enemiesInRange = EnemyManager.Instance.GetActiveEnemiesInRangeSorted(
                transform.position, 
                towerData.attackRange);
            
            // If we have any enemies, return the first one (closest one due to sorting)
            if (enemiesInRange.Count > 0 && enemiesInRange[0] != null)
            {
                return enemiesInRange[0].gameObject;
            }
        }
        
        // Fallback if EnemyManager not available
        return FindClosestEnemyFallback();
    }
    
    // Fallback method in case EnemyManager is not available
    private GameObject FindClosestEnemyFallback()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (enemies.Length == 0)
        {
            // No enemies found
            return null;
        }
        
        GameObject closest = null;
        float closestDist = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < towerData.attackRange && dist < closestDist)
            {
                closest = enemy;
                closestDist = dist;
            }
        }
        
        return closest;
    }
    
    private void Fire()
    {
        if (projectileSpawner == null || currentTarget == null) return;
        
        Vector3? predictedPos = aimSystem.PredictTargetPosition(currentTarget.transform, 1f);
        
        if (predictedPos == null) return;
        
        Vector3 direction = (predictedPos.Value - projectileSpawner.ShootingPoint.position).normalized;
        
        // Only rotate the shooting point, not the main tower sprite
        if (shootPoint != null)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            shootPoint.rotation = rotation;
        }
        
        // Keep the tower sprite upright if assigned
        if (towerSprite != null && towerSprite != shootPoint)
        {
            towerSprite.rotation = Quaternion.identity;
        }
        
        // Spawn projectile
        projectileSpawner.SpawnProjectile(direction);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (towerData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, towerData.attackRange);
        }
    }
}