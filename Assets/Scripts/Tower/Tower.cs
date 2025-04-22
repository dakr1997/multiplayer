using UnityEngine;
using Unity.Netcode;

public class Tower : NetworkBehaviour
{
    public float TowerRange = 5f;
    public float fireRate = 1f;
    public float TowerDamage = 10;
    public bool isTower = true;
    private float lastFireTime;
    public Transform TowerShootingPoint;
    public GameObject projectilePrefab;
    private Aim aimSystem;

    public float predictionTime = 1f;
    private GameObject currentTarget;

    void Start()
    {
        if (!IsServer) return; // Prevent client-side logic

        lastFireTime = Time.time;
        aimSystem = new Aim();
    }

    private void Update()
    {
        if (!IsServer) return; // Ensure only server handles targeting and firing

        UpdateTargets(); // Continuously update target prediction

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
        if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.transform.position) > TowerRange)
        {
            currentTarget = FindClosestEnemy();
            aimSystem = new Aim();

            if (currentTarget != null)
                aimSystem.AddTarget(currentTarget.transform);
        }
    }

    private void Fire()
    {
        // Ensure we’re only running this on the server
        if (!IsServer) return;

        if (TowerShootingPoint == null || projectilePrefab == null)
        {
            Debug.LogError("Assign TowerShootingPoint and projectilePrefab in the Inspector!");
            return;
        }

        if (currentTarget == null)
            return;

        // Predict enemy's future position
        Vector3? predictedPosition = aimSystem.PredictTargetPosition(currentTarget.transform, predictionTime);
        if (predictedPosition == null)
            return;

        // Rotate tower to face target
        Vector3 direction = (predictedPosition.Value - TowerShootingPoint.position).normalized;
        TowerShootingPoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        // Spawn projectile from server
        GameObject projectile = Instantiate(projectilePrefab, TowerShootingPoint.position, TowerShootingPoint.rotation);
        
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true); // true = server owns it
        }
        else
        {
            Debug.LogError("Missing NetworkObject on projectile prefab!");
            return;
        }

        // Initialize projectile logic
        if (projectile.TryGetComponent<Projectile>(out Projectile projectileScript))
        {
            projectileScript.SetDirection(direction);
            projectileScript.SetDamage(TowerDamage);
            projectileScript.SetSource("Tower");
        }
        else
        {
            Debug.LogError("Projectile script missing on prefab!");
        }
    }

}
