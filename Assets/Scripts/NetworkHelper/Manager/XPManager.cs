using Unity.Netcode;
using UnityEngine;
using Core.Enemies.Base;

public class XPManager : NetworkBehaviour
{
    public static XPManager Instance { get; private set; }
    
    [SerializeField] private NetworkObject expBubblePrefab;
    [SerializeField] private int defaultXpAmount = 10;
    
    private NetworkObjectPool poolManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Get pool manager reference after network spawning
        poolManager = NetworkObjectPool.Instance;
        
        if (poolManager == null)
        {
            Debug.LogError("NetworkObjectPool not found! Trying to find it in scene...");
            poolManager = FindObjectOfType<NetworkObjectPool>();
            
            if (poolManager == null)
            {
                Debug.LogError("NetworkObjectPool still not found! XP bubbles will not spawn.");
            }
            else
            {
                Debug.Log("NetworkObjectPool found through FindObjectOfType.");
            }
        }
        
        if (expBubblePrefab == null)
        {
            Debug.LogError("ExpBubble prefab not assigned in XPManager!");
        }
        
        // Subscribe to enemy death events
        EnemyAI.OnEnemyDied += HandleEnemyDeath;
        
        Debug.Log("XPManager initialized with poolManager: " + (poolManager != null ? "OK" : "NULL"));
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Unsubscribe from events
        EnemyAI.OnEnemyDied -= HandleEnemyDeath;
    }
    
    /// <summary>
    /// Award XP to a specific player
    /// </summary>
    public void AwardXP(ulong playerId, int amount)
    {
        if (!IsServer)
        {
            GrantXPServerRpc(playerId, amount);
            return;
        }
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in AwardXP!");
            return;
        }
        
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
        {
            if (client.PlayerObject != null)
            {
                var exp = client.PlayerObject.GetComponent<PlayerExperience>();
                if (exp != null)
                {
                    exp.AddXP(amount);
                }
                else
                {
                    Debug.LogWarning($"[Server] Player {playerId} missing PlayerExperience component.");
                }
            }
        }
    }
    
    /// <summary>
    /// Award XP to all connected players
    /// </summary>
    public void AwardXPToAll(int amount)
    {
        if (!IsServer)
        {
            GrantXPToAllPlayersServerRpc(amount);
            return;
        }
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in AwardXPToAll!");
            return;
        }
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var exp = client.PlayerObject.GetComponent<PlayerExperience>();
                if (exp != null)
                {
                    exp.AddXP(amount);
                }
            }
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    private void GrantXPServerRpc(ulong playerId, int amount)
    {
        if (!IsServer) return;
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in GrantXPServerRpc!");
            return;
        }
        
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerId);
        if (playerObject != null)
        {
            var exp = playerObject.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.AddXP(amount);
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void GrantXPToAllPlayersServerRpc(int amount)
    {
        if (!IsServer) return;
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in GrantXPToAllPlayersServerRpc!");
            return;
        }
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var exp = client.PlayerObject.GetComponent<PlayerExperience>();
                if (exp != null)
                {
                    exp.AddXP(amount);
                }
            }
        }
    }
    
    /// <summary>
    /// Handles enemy death by spawning XP bubbles
    /// </summary>
    private void HandleEnemyDeath(EnemyAI enemy)
    {
        if (!IsServer || enemy == null) return;
        
        Debug.Log($"Enemy died: {(enemy.name != null ? enemy.name : "unnamed")}");
        
        // Check for null Data
        if (enemy.Data == null)
        {
            Debug.LogWarning("Enemy died but has null Data reference!");
            SpawnExpBubble(enemy.transform.position, defaultXpAmount);
            return;
        }
        
        // Get XP amount from enemy data
        int xpAmount = enemy.Data.experienceReward;
        
        // Spawn XP bubble
        SpawnExpBubble(enemy.transform.position, xpAmount);
    }
    
    /// <summary>
    /// Spawns an XP bubble at the specified position
    /// </summary>
    private void SpawnExpBubble(Vector3 position, int xpAmount)
    {
        if (!IsServer) return;
        
        // Check for null references and try to recover
        if (poolManager == null)
        {
            Debug.LogError("Cannot spawn XP bubble - poolManager is null! Trying to recover...");
            poolManager = NetworkObjectPool.Instance;
            
            if (poolManager == null)
            {
                poolManager = FindObjectOfType<NetworkObjectPool>();
                if (poolManager == null)
                {
                    Debug.LogError("Still cannot find NetworkObjectPool! XP bubble cannot spawn.");
                    return;
                }
            }
        }
        
        if (expBubblePrefab == null)
        {
            Debug.LogError("Cannot spawn XP bubble - expBubblePrefab is null!");
            return;
        }
        
        // Try to get bubble from pool
        NetworkObject bubbleNetObj = poolManager.Get(expBubblePrefab);
        if (bubbleNetObj == null)
        {
            Debug.LogError("Failed to get ExpBubble from pool!");
            return;
        }
        
        if (bubbleNetObj.TryGetComponent<ExpBubble>(out var bubble))
        {
            // IMPORTANT: Set position directly BEFORE initialization
            bubbleNetObj.transform.position = position;
            
            // Then initialize the bubble with the same position
            bubble.Initialize(position, xpAmount);
            
            // Spawn only after position is set correctly
            if (!bubbleNetObj.IsSpawned)
            {
                bubbleNetObj.Spawn(true);
                Debug.Log($"Spawned XP bubble at {position} with {xpAmount} XP");
            }
        }
        else
        {
            Debug.LogError("ExpBubble component missing on prefab!");
            poolManager.Release(expBubblePrefab, bubbleNetObj);
        }
    }
}