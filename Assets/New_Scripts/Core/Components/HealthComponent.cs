// Location: Core/Components/HealthComponent.cs
using UnityEngine;
using Unity.Netcode;
using System;
using Core.Interfaces;
using Core.Enemies.Base;

namespace Core.Components
{
    public class HealthComponent : NetworkBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float destroyDelay = 2f;

        // Network variable for health
        private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        
        // Network variable for alive state
        private NetworkVariable<bool> isAliveState = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        
        // Wave multipliers
        private float healthMultiplier = 1f;
        private bool hasAppliedMultiplier = false;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        // Properties
        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth * healthMultiplier;
        public bool IsAlive => isAliveState.Value;
        public float HealthPercent => currentHealth.Value / MaxHealth;
        
        /// <summary>
        /// Set health multiplier for wave difficulty scaling
        /// </summary>
        public void SetHealthMultiplier(float multiplier)
        {
            if (hasAppliedMultiplier)
            {
                // If we've already applied a multiplier, just scale current health proportionally
                float healthPercent = currentHealth.Value / (maxHealth * healthMultiplier);
                healthMultiplier = Mathf.Max(1f, multiplier);
                
                if (IsServer)
                {
                    currentHealth.Value = maxHealth * healthMultiplier * healthPercent;
                }
            }
            else
            {
                // First-time application
                healthMultiplier = Mathf.Max(1f, multiplier);
                
                if (IsServer)
                {
                    currentHealth.Value = maxHealth * healthMultiplier;
                }
                
                hasAppliedMultiplier = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Initialize with multiplier if it's been set
                if (hasAppliedMultiplier)
                {
                    currentHealth.Value = maxHealth * healthMultiplier;
                }
                else
                {
                    currentHealth.Value = maxHealth;
                }
                
                // Ensure alive state is true
                isAliveState.Value = true;
            }

            currentHealth.OnValueChanged += HandleHealthChanged;
            isAliveState.OnValueChanged += HandleAliveStateChanged;
        }

        public void TakeDamage(float amount, string source = null)
        {
            if (!IsServer || !isAliveState.Value) return;

            float newHealth = Mathf.Clamp(currentHealth.Value - amount, 0, MaxHealth);
            currentHealth.Value = newHealth;

            Debug.Log($"{gameObject.name} took {amount} damage from {source}. Health: {currentHealth.Value}/{MaxHealth}");

            if (newHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsServer || !isAliveState.Value) return;

            float newHealth = Mathf.Clamp(currentHealth.Value + amount, 0, MaxHealth);
            currentHealth.Value = newHealth;

            Debug.Log($"{gameObject.name} healed for {amount}. Health: {currentHealth.Value}/{MaxHealth}");
        }

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, MaxHealth);
        }
        
        private void HandleAliveStateChanged(bool oldValue, bool newValue)
        {
            if (oldValue == true && newValue == false)
            {
                // The entity just died, disable all important components
                DisableComponentsLocally();
            }
        }
        
        private void DisableComponentsLocally()
        {
            // Disable AI if present
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.enabled = false;
            }
            
            // Disable EnemyDamage if present
            EnemyDamage damage = GetComponent<EnemyDamage>();
            if (damage != null)
            {
                damage.enabled = false;
            }
            
            // Disable Rigidbody2D forces
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
            
            // Disable colliders
            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} died.");
            
            // Set alive state to false - this will be synchronized to all clients
            isAliveState.Value = false;

            // Play death effects and disable visual components on clients
            DisableGameObjectClientRpc();

            // Disable components locally on the server
            DisableComponentsLocally();

            // Notify listeners
            OnDied?.Invoke();
            
            // Get the poolable component FIRST before potentially despawning
            var poolable = GetComponent<PoolableNetworkObject>();
            
            // Now handle pooling with a delay
            if (poolable != null)
            {
                Debug.Log($"[HealthComponent] Found poolable component, returning {gameObject.name} to pool after delay");
                poolable.ReturnToPool(2.0f);
            }
            else if (destroyOnDeath)
            {
                StartCoroutine(DestroyAfterDelay(destroyDelay));
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

        [ClientRpc]
        private void DisableGameObjectClientRpc()
        {
            // Spawn death effect if specified
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            }

            // Disable all renderers (visual only)
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
            
            // Disable colliders
            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }
            
            // Disable Rigidbody2D forces but keep it active
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
            
            // We no longer need to disable MonoBehaviours here since we're using
            // the NetworkVariable isAliveState to control behavior at the source
        }

        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= HandleHealthChanged;
            isAliveState.OnValueChanged -= HandleAliveStateChanged;
            base.OnNetworkDespawn();
        }
    }
}