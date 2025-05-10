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
        
        // Tracking variables
        private bool deathProcessed = false;
        
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
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Reset state
            deathProcessed = false;
            
            if (IsServer)
            {
                // Initialize components 
                InitializeComponents();
                
                // Trigger spawn event
                OnEnemySpawned?.Invoke(this);
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
        /// Reset the entity to its initial state when pulled from the object pool
        /// </summary>
        public void ResetState()
        {
            if (!IsServer) return;
            
            Debug.Log($"[EnemyEntity] Resetting state for {gameObject.name}");
            
            // Reset health component
            if (Health != null)
            {
                Health.ResetState();
            }
            
            // Reset AI component
            if (aiComponent != null)
            {
                aiComponent.enabled = true;
                
                // Reset any AI-specific state if needed
                aiComponent.ResetState();
            }
            
            // Reset damage component
            if (damageComponent != null)
            {
                damageComponent.enabled = true;
            }
            
            // Reset renderers
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
            
            // Reset colliders
            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = true;
            }
            
            // Reset physics
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            // Reset state variables
            deathProcessed = false;
        }
        
        /// <summary>
        /// Handle death event from HealthComponent
        /// </summary>
        private void HandleDeath()
        {
            if (!IsServer || deathProcessed) return;
            
            Debug.Log($"[EnemyEntity] {gameObject.name} died");
            
            // Mark death as processed to prevent multiple calls
            deathProcessed = true;
            
            // Make sure AI stops immediately
            if (aiComponent != null)
            {
                aiComponent.enabled = false;
            }
            
            // Make sure damage stops immediately
            if (damageComponent != null)
            {
                damageComponent.enabled = false;
            }
            
            // The HealthComponent handles actual despawning/pooling logic
        }
        
        private void OnDisable()
        {
            // Make sure components are disabled when this entity is disabled
            if (aiComponent != null)
            {
                aiComponent.enabled = false;
            }
            
            if (damageComponent != null)
            {
                damageComponent.enabled = false;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from events
            if (Health != null)
            {
                Health.OnDied -= HandleDeath;
            }
            
            // Trigger despawn event
            OnEnemyDespawned?.Invoke(this);
            
            base.OnNetworkDespawn();
        }
    }
}