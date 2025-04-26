using UnityEngine;
using Unity.Netcode;

public class Tower : NetworkBehaviour
{
    [Header("Tower Settings")]
    public float TowerRange = 5f;
    public float fireRate = 1f;
    public float TowerDamage = 10f;

    private float fireCooldown = 0f;
    public Transform TowerShootingPoint;
    [SerializeField] private ProjectileSpawner projectileSpawner;
    private Aim aimSystem;
    public float predictionTime = 1f;
    private GameObject currentTarget;

    private HealthComponent health;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        InitializeTower();
    }

    public void InitializeTower()
    {
        fireCooldown = 0f;
        aimSystem = new Aim();

        if (projectileSpawner == null)
        {
            Debug.LogError("ProjectileSpawner reference missing!");
            return;
        }

        projectileSpawner.TowerDamage = TowerDamage;
        projectileSpawner.TowerShootingPoint = TowerShootingPoint;
    }

    private void Update()
    {
        if (!IsServer || isDead) return;

        fireCooldown += Time.deltaTime;
        UpdateTargets();

        if (fireCooldown >= 1f / fireRate)
        {
            Fire();
            fireCooldown = 0f;
        }
    }

    private void UpdateTargets()
    {
        if (currentTarget == null || !currentTarget.activeInHierarchy || Vector3.Distance(transform.position, currentTarget.transform.position) > TowerRange)
        {
            currentTarget = FindClosestEnemy();
            aimSystem = new Aim();

            if (currentTarget != null)
                aimSystem.AddTarget(currentTarget.transform);
        }
    }

    private GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float closestDist = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < TowerRange && dist < closestDist)
            {
                closest = enemy;
                closestDist = dist;
            }
        }

        return closest;
    }

    private void Fire()
    {
        if (TowerShootingPoint == null || projectileSpawner == null || currentTarget == null)
        {
            return;
        }

        Vector3? predictedPos = aimSystem.PredictTargetPosition(currentTarget.transform, predictionTime);
        if (predictedPos == null) return;

        Vector3 direction = (predictedPos.Value - TowerShootingPoint.position).normalized;
        TowerShootingPoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        projectileSpawner.SpawnProjectile(direction);
    }

    private void HandleTowerDeath()
    {
        if (!IsServer || isDead) return;

        isDead = true;
        GetComponent<Collider2D>().enabled = false;
        enabled = false;

        PlayDeathEffectsClientRpc();

        if (IsSceneObject())
        {
            gameObject.SetActive(false);
            SetTowerActiveClientRpc(false);
            Destroy(gameObject, 1f);
        }
        else
        {
            GetComponent<NetworkObject>().Despawn(true);
            Destroy(gameObject, 1f);
        }
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc() { }

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
