using UnityEngine;
using Unity.Netcode;

public class EnemyDamage : NetworkBehaviour
{
    [SerializeField] private EnemyData enemyData;
    
    // State variables
    private float lastAttackTime;
    private Transform towerTransform;
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        // Cache reference to main tower
        if (MainTowerHP.Instance != null)
        {
            towerTransform = MainTowerHP.Instance.transform;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        
        if (Time.time - lastAttackTime >= enemyData.attackCooldown)
        {
            if (IsTowerInRange())
            {
                AttackTower();
                lastAttackTime = Time.time;
            }
        }
    }

    private bool IsTowerInRange()
    {
        return towerTransform != null && 
               Vector3.Distance(transform.position, towerTransform.position) <= enemyData.attackRange;
    }

    private void AttackTower()
    {
        if (MainTowerHP.Instance != null && MainTowerHP.Instance.IsAlive)
        {
            MainTowerHP.Instance.TakeDamage(enemyData.damage, gameObject.name);
            PlayAttackEffectsClientRpc();
        }
    }

    [ClientRpc]
    private void PlayAttackEffectsClientRpc()
    {
        // Visual/Sound effects here
        Debug.Log($"Enemy {gameObject.name} attacked the tower!");
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
        }
    }
}