using UnityEngine;
using Unity.Netcode;

public class EnemyDamage : NetworkBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 2f;
    
    private float lastAttackTime;
    private Transform towerTransform;

    private void Start()
    {
        // Cache the tower reference
        towerTransform = MainTowerHP.Instance.transform;
    }

    private void Update()
    {
        if (!IsServer) return;
        
        if (Time.time - lastAttackTime >= attackCooldown)
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
        return Vector3.Distance(transform.position, towerTransform.position) <= attackRange;
    }

    private void AttackTower()
    {
        if (MainTowerHP.Instance != null && MainTowerHP.Instance.IsAlive)
        {
            MainTowerHP.Instance.TakeDamage(damageAmount, gameObject.name);
            PlayAttackEffectsClientRpc();
        }
    }

    [ClientRpc]
    private void PlayAttackEffectsClientRpc()
    {
        // Visual/Sound effects here
        Debug.Log($"Enemy {gameObject.name} attacked the tower!");
    }

    // Optional: Visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}