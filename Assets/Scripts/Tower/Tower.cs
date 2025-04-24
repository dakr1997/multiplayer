using UnityEngine;
using Unity.Netcode;

public class Tower : NetworkBehaviour
{
    [Header("Tower Settings")]
    public float TowerRange = 5f;
    public float fireRate = 1f;
    public float TowerDamage = 10;
    private float lastFireTime;
    public Transform TowerShootingPoint;
    public GameObject projectilePrefab;
    private Aim aimSystem;

    public float predictionTime = 1f;
    private GameObject currentTarget;

    // Health
    private HealthComponent health;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        InitializeTower();
    }

    public void InitializeTower()
    {
        lastFireTime = Time.time;
        aimSystem = new Aim();

        health = GetComponent<HealthComponent>();
        if (health != null)
        {
            health.OnDied += HandleTowerDeath;
        }
        else
        {
            Debug.LogError("HealthComponent missing from Tower!");
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleTowerDeath;
        }
    }

    private void Update()
    {
        if (!IsServer || isDead) return;

        UpdateTargets();

        if (Time.time - lastFireTime >= 1f / fireRate)
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    private GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0)
        {
            return null;
        }

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < TowerRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void UpdateTargets()
    {
        if (currentTarget == null || 
            !currentTarget.activeInHierarchy || 
            Vector3.Distance(transform.position, currentTarget.transform.position) > TowerRange)
        {
            currentTarget = FindClosestEnemy();
            aimSystem = new Aim();

            if (currentTarget != null)
                aimSystem.AddTarget(currentTarget.transform);
        }
    }

    private void Fire()
    {
        if (TowerShootingPoint == null || projectilePrefab == null)
        {
            Debug.LogError("Assign TowerShootingPoint and projectilePrefab in the Inspector!");
            return;
        }

        if (currentTarget == null) return;

        Vector3? predictedPosition = aimSystem.PredictTargetPosition(currentTarget.transform, predictionTime);
        if (predictedPosition == null) return;

        Vector3 direction = (predictedPosition.Value - TowerShootingPoint.position).normalized;
        TowerShootingPoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        SpawnProjectile(direction);
    }

    private void SpawnProjectile(Vector3 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, TowerShootingPoint.position, TowerShootingPoint.rotation);
        
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Missing NetworkObject on projectile prefab!");
            Destroy(projectile);
            return;
        }

        netObj.Spawn(true);

        if (projectile.TryGetComponent<Projectile>(out Projectile projectileScript))
        {
            projectileScript.Initialize(direction, TowerDamage, "Tower");
        }
        else
        {
            Debug.LogError("Projectile script missing on prefab!");
        }
    }

    private void HandleTowerDeath()
    {
        if (!IsServer || isDead) return;
        
        isDead = true;
        Debug.Log($"Destroying tower {gameObject.name}");
        
        // Disable components immediately
        GetComponent<Collider2D>().enabled = false;
        enabled = false;

        // Play death effects
        PlayDeathEffectsClientRpc();

        if (IsSceneObject())
        {
            // Handle static scene object destruction
            gameObject.SetActive(false);
            SetTowerActiveClientRpc(false);
            Destroy(gameObject, 1f);
        }
        else
        {
            // Handle spawned prefab destruction
            GetComponent<NetworkObject>().Despawn(true);
            Destroy(gameObject, 1f);
        }
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        // Play visual/sound effects here
        // This runs on all clients
    }

    [ClientRpc]
    private void SetTowerActiveClientRpc(bool active)
    {
        gameObject.SetActive(active);
    }

    private bool IsSceneObject()
    {
        return GetComponent<NetworkObject>().IsSceneObject != null;
    }
}