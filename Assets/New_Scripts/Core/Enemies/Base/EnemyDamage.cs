// Location: Core/Enemies/Base/EnemyDamage.cs
using UnityEngine;
using Unity.Netcode;
using Core.Towers.MainTower;
using Core.Components;

namespace Core.Enemies.Base
{
    public class EnemyDamage : NetworkBehaviour
    {
        [SerializeField] private EnemyData enemyData;
        
        // State variables
        private float lastAttackTime;
        private Transform towerTransform;
        
        // Wave-specific multipliers
        private float damageMultiplier = 1f;
        
        // Reference to health component
        private HealthComponent healthComponent;
        
        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
        }
        
        /// <summary>
        /// Set enemy data for configuration
        /// </summary>
        public void SetEnemyData(EnemyData data)
        {
            if (data != null)
            {
                enemyData = data;
            }
        }
        
        /// <summary>
        /// Set damage multiplier for wave difficulty scaling
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(1f, multiplier);
        }
        
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
            // Only process if:
            // 1. This is the server
            // 2. This component is enabled (will be disabled on death)
            // 3. The enemy is alive 
            if (!IsServer || !enabled || !gameObject.activeInHierarchy || 
                (healthComponent != null && !healthComponent.IsAlive)) 
                return;
            
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
                // Apply damage multiplier
                float damage = enemyData.damage * damageMultiplier;
                
                MainTowerHP.Instance.TakeDamage(damage, gameObject.name);
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
}