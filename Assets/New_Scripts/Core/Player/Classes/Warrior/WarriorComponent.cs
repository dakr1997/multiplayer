using UnityEngine;
using Unity.Netcode;
using Player.Base;

namespace Player.Classes.Warrior
{
    /// <summary>
    /// Warrior-specific abilities and stats
    /// </summary>
    public class WarriorComponent : NetworkBehaviour
    {
        [Header("Warrior Settings")]
        [SerializeField] private float meleeAttackStrength = 10f;
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private float cooldown = 1.5f;
        
        // References
        private PlayerEntity playerEntity;
        private float lastAttackTime;
        
        private void Awake()
        {
            playerEntity = GetComponent<PlayerEntity>();
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            // Check for melee attack input
            if (Input.GetMouseButtonDown(0) && Time.time > lastAttackTime + cooldown)
            {
                RequestMeleeAttackServerRpc();
                lastAttackTime = Time.time;
            }
        }
        
        [ServerRpc]
        private void RequestMeleeAttackServerRpc()
        {
            if (!IsServer) return;
            
            // Perform melee attack
            PerformMeleeAttack();
            
            // Notify clients
            PerformMeleeAttackClientRpc();
        }
        
        [ClientRpc]
        private void PerformMeleeAttackClientRpc()
        {
            // Play attack animation and effects
            Debug.Log("Warrior melee attack!");
        }
        
        private void PerformMeleeAttack()
        {
            // Find enemies in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeRange);
            
            foreach (var hit in hits)
            {
                // Apply damage to enemies
                if (hit.CompareTag("Enemy"))
                {
                    // Apply damage using the DamageHelper
                    DamageHelper.ApplyDamage(hit.gameObject, meleeAttackStrength, "WarriorMelee");
                }
            }
        }
    }
}