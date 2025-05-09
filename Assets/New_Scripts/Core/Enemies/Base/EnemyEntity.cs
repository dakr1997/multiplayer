using UnityEngine;
using Unity.Netcode;
using System;
using Core.Components;
using Core.Entities;

namespace Core.Enemies.Base
{
    /// <summary>
    /// Base enemy entity class that coordinates all enemy components
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyEntity : GameEntity
    {
        [Header("Enemy Configuration")]
        [SerializeField] private EnemyData enemyData;
        
        // Components
        private EnemyAI aiComponent;
        private EnemyDamage damageComponent;
        
        // Properties
        public EnemyData Data => enemyData;
        
        // Events
        public static event Action<EnemyEntity> OnEnemySpawned;
        public static event Action<EnemyEntity> OnEnemyDespawned;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Get required components
            aiComponent = GetComponent<EnemyAI>();
            damageComponent = GetComponent<EnemyDamage>();
            
            // Validate required data
            if (enemyData == null)
            {
                Debug.LogError($"[EnemyEntity] {gameObject.name} is missing EnemyData!");
            }
        }
        
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            if (IsServer)
            {
                // Initialize with base values
                if (enemyData != null)
                {
                    // Set up AI component
                    if (aiComponent != null)
                    {
                        aiComponent.SetEnemyData(enemyData);
                    }
                    
                    // Set up damage component
                    if (damageComponent != null)
                    {
                        damageComponent.SetEnemyData(enemyData);
                    }
                }
                
                // Subscribe to health events
                if (Health != null)
                {
                    Health.OnDied += HandleDeath;
                }
                
                // Trigger spawn event
                OnEnemySpawned?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Initialize this enemy for a specific wave
        /// </summary>
        public void InitializeForWave(int waveNumber, float healthMultiplier = 1f, float damageMultiplier = 1f)
        {
            if (!IsServer) return;
            
            // Apply health multiplier
            if (Health != null)
            {
                Health.SetHealthMultiplier(healthMultiplier);
            }
            
            // Apply damage multiplier
            if (damageComponent != null)
            {
                damageComponent.SetDamageMultiplier(damageMultiplier);
            }
            
            // Initialize AI with wave data
            if (aiComponent != null)
            {
                aiComponent.InitializeForWave(waveNumber);
            }
        }
        
        /// <summary>
        /// Handle death event from HealthComponent
        /// </summary>
        private void HandleDeath()
        {
            if (!IsServer) return;
            
            Debug.Log($"[EnemyEntity] {gameObject.name} died");
            
            // Disable AI if we have it (prevent movement during death animation)
            var aiComponent = GetComponent<EnemyAI>();
            if (aiComponent != null)
            {
                aiComponent.enabled = false;
            }
            
            // Return to object pool if poolable (with delay to allow death effects)
            if (TryGetComponent<PoolableEnemy>(out var poolable))
            {
                Debug.Log($"[EnemyEntity] Returning {gameObject.name} to pool after delay");
                poolable.ReturnToPool(2.0f);
            }
            else if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                // Fallback for non-pooled enemies
                Debug.Log($"[EnemyEntity] No poolable component found, destroying {gameObject.name}");
                StartCoroutine(DestroyAfterDelay(2.0f));
            }
        }


        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unsubscribe from events
            if (Health != null)
            {
                Health.OnDied -= HandleDeath;
            }
            
            // Trigger despawn event
            OnEnemyDespawned?.Invoke(this);
        }
    }
}