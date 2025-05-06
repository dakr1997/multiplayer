using UnityEngine;
using Unity.Netcode;
using Player.Base;

namespace Player.Classes.Archer
{
    /// <summary>
    /// Archer-specific abilities and stats
    /// </summary>
    public class ArcherComponent : NetworkBehaviour
    {
        [Header("Archer Settings")]
        [SerializeField] private float arrowDamage = 7f;
        [SerializeField] private float arrowSpeed = 15f;
        [SerializeField] private float cooldown = 1f;
        
        // References
        private PlayerEntity playerEntity;
        private ProjectileSpawner projectileSpawner;
        private float lastAttackTime;
        
        private void Awake()
        {
            playerEntity = GetComponent<PlayerEntity>();
            projectileSpawner = GetComponent<ProjectileSpawner>();
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            // Check for ranged attack input
            if (Input.GetMouseButtonDown(0) && Time.time > lastAttackTime + cooldown)
            {
                // Get aim direction from mouse position
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                Vector3 direction = (mousePos - transform.position).normalized;
                
                RequestRangedAttackServerRpc(direction);
                lastAttackTime = Time.time;
            }
        }
        
        [ServerRpc]
        private void RequestRangedAttackServerRpc(Vector3 direction)
        {
            if (!IsServer) return;
            
            // Spawn projectile in specified direction
            if (projectileSpawner != null)
            {
                projectileSpawner.SpawnProjectile(direction);
            }
            
            // Notify clients
            PerformRangedAttackClientRpc();
        }
        
        [ClientRpc]
        private void PerformRangedAttackClientRpc()
        {
            // Play attack animation and effects
            Debug.Log("Archer ranged attack!");
        }
    }
}