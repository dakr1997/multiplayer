using System;
using UnityEngine;
using Unity.Netcode;
using Core.GameManagement;

namespace Core.Towers.MainTower
{
    /// <summary>
    /// Handles the main tower's health and destruction
    /// </summary>
    public class MainTowerHP : NetworkBehaviour
    {
        // Singleton instance
        private static MainTowerHP _instance;
        public static MainTowerHP Instance { get { return _instance; } }
        
        [Header("Tower Settings")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private GameObject destroyedEffectPrefab;
        [SerializeField] private GameObject towerModel;
        
        // Network variable for health synchronization
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
        
        // Events
        public static event Action OnTowerDestroyed;
        public event Action<float, float> OnHealthChanged; // current, max
        
        // Property to get current health percentage
        public float HealthPercentage => networkHealth.Value / maxHealth;
        
        // Public properties for health access
        public float CurrentHealth => networkHealth.Value;
        public float MaxHealth => maxHealth;
        
        // Property to check if tower is alive
        public bool IsAlive => networkHealth.Value > 0;
        
        private void Awake()
        {
            // Set singleton instance
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MainTowerHP] Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Initialize health
            currentHealth = maxHealth;
            
            Debug.Log("[MainTowerHP] Singleton instance initialized");
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                // Initialize network variable
                networkHealth.Value = maxHealth;
                
                // Connect to GameManager
                GameManager gameManager = GameServices.Get<GameManager>();
                if (gameManager != null)
                {
                    gameManager.ConnectMainTower(this);
                    Debug.Log("[MainTowerHP] Connected to GameManager");
                }
                else
                {
                    Debug.LogWarning("[MainTowerHP] GameManager not found during spawn!");
                }
            }
            
            // Subscribe to health changes
            networkHealth.OnValueChanged += OnNetworkHealthChanged;
            
            Debug.Log("[MainTowerHP] Tower spawned with health: " + networkHealth.Value);
        }
        
        /// <summary>
        /// Handle damage to the tower
        /// </summary>
        public void TakeDamage(float damage, string sourceName = "Unknown")
        {
            if (!IsServer) return;
            
            // Calculate new health
            float newHealth = Mathf.Max(0, networkHealth.Value - damage);
            
            // Update network variable
            networkHealth.Value = newHealth;
            
            Debug.Log($"[MainTowerHP] Tower took {damage} damage from {sourceName}. Health: {newHealth}/{maxHealth}");
            
            // Check if tower is destroyed
            if (newHealth <= 0)
            {
                DestroyTower();
            }
        }
        
        /// <summary>
        /// Request to set the tower HP from client
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSetHPServerRpc(float newHP, ServerRpcParams serverRpcParams = default)
        {
            // Optional validation (for example, only allow admins)
            // ulong clientId = serverRpcParams.Receive.SenderClientId;
            
            // Set new health
            networkHealth.Value = Mathf.Clamp(newHP, 0, maxHealth);
            
            // Check if tower is destroyed
            if (networkHealth.Value <= 0)
            {
                DestroyTower();
            }
        }
        
        /// <summary>
        /// Heal the tower
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsServer) return;
            
            // Calculate new health
            float newHealth = Mathf.Min(maxHealth, networkHealth.Value + amount);
            
            // Update network variable
            networkHealth.Value = newHealth;
            
            Debug.Log($"[MainTowerHP] Tower healed for {amount}. Health: {newHealth}/{maxHealth}");
        }
        
        /// <summary>
        /// Set tower to full health
        /// </summary>
        public void Reset()
        {
            if (!IsServer) return;
            
            networkHealth.Value = maxHealth;
            
            // Enable tower model if previously disabled
            if (towerModel != null)
            {
                towerModel.SetActive(true);
            }
            
            Debug.Log("[MainTowerHP] Tower reset to full health");
        }
        
        /// <summary>
        /// Destroy the tower
        /// </summary>
        private void DestroyTower()
        {
            if (!IsServer) return;
            
            Debug.Log("[MainTowerHP] Tower destroyed! Health = " + networkHealth.Value);
            
            // Spawn destruction effect
            if (destroyedEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(destroyedEffectPrefab, transform.position, transform.rotation);
                NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    networkObj.Spawn();
                }
            }
            
            // Disable tower model
            if (towerModel != null)
            {
                towerModel.SetActive(false);
            }
            
            // Log before invoking event
            Debug.Log($"[MainTowerHP] About to invoke OnTowerDestroyed event. Has listeners: {OnTowerDestroyed != null}");
            
            // Trigger events
            OnTowerDestroyed?.Invoke();
            
            Debug.Log("[MainTowerHP] OnTowerDestroyed event invoked");
            
            // Also directly notify GameManager to ensure it gets the message
            GameManager gameManager = GameServices.Get<GameManager>();
            if (gameManager != null)
            {
                Debug.Log("[MainTowerHP] Directly calling GameManager.OnMainTowerDestroyed()");
                gameManager.OnMainTowerDestroyed();
            }
            else
            {
                Debug.LogError("[MainTowerHP] GameManager not found when tower was destroyed!");
                
                // Try to find GameStateManager directly as a fallback
                GameState.GameStateManager stateManager = GameServices.Get<GameState.GameStateManager>();
                if (stateManager != null)
                {
                    Debug.Log("[MainTowerHP] Directly changing state to GameOver via GameStateManager");
                    stateManager.ChangeState(GameState.GameStateType.GameOver);
                }
                else
                {
                    Debug.LogError("[MainTowerHP] GameStateManager not found either! Cannot transition to GameOver state!");
                }
            }
            
            // We don't actually destroy the network object, just disable it visually
            // This allows us to reset the tower later if needed
        }
        
        /// <summary>
        /// Handle changes to the network health variable
        /// </summary>
        private void OnNetworkHealthChanged(float oldValue, float newValue)
        {
            // Update local variable
            currentHealth = newValue;
            
            // Trigger health changed event
            OnHealthChanged?.Invoke(newValue, maxHealth);
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unsubscribe from events
            networkHealth.OnValueChanged -= OnNetworkHealthChanged;
            
            // Clear instance reference if this is the current instance
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void OnDestroy()
        {
            // Clear instance reference if this is the current instance
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}