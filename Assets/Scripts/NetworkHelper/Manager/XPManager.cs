using Unity.Netcode;
using UnityEngine;

public class XPManager : NetworkBehaviour
{
    public static XPManager Instance { get; private set; }

    [SerializeField] private NetworkObject expBubblePrefab;
    private NetworkObjectPool poolManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        poolManager = FindObjectOfType<NetworkObjectPool>();
        if (poolManager == null)
        {
            Debug.LogError("NetworkObjectPool not found in scene!");
        }
    }

    private void OnEnable() => EnemyAI.OnEnemyDied += HandleEnemyDeath;
    private void OnDisable() => EnemyAI.OnEnemyDied -= HandleEnemyDeath;

    public void AwardXP(ulong playerId, int amount)
    {
        if (!IsServer)
        {
            Debug.Log($"[Client] Requesting XP award for player {playerId}");
            GrantXPServerRpc(playerId, amount);
            return;
        }

        Debug.Log($"[Server] Awarding {amount} XP to player {playerId}");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
        {
            var exp = client.PlayerObject?.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.AddXP(amount);
            }
            else
            {
                Debug.LogWarning($"[Server] Player {playerId} missing PlayerExperience component.");
            }
        }
        else
        {
            Debug.LogWarning($"[Server] No connected client found with ID {playerId}");
        }
    }

    public void AwardXPToAll(int amount)
    {
        if (!IsServer)
        {
            Debug.Log($"[Client] Requesting XP award for all players");
            GrantXPToAllPlayersServerRpc(amount);
            return;
        }

        Debug.Log($"[Server] Awarding {amount} XP to all players");

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var exp = client.PlayerObject?.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.AddXP(amount);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrantXPServerRpc(ulong playerId, int amount)
    {
        if (!IsServer) return;

        Debug.Log($"[ServerRPC] Granting {amount} XP to player {playerId}");

        var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerId);
        if (player != null)
        {
            var xp = player.GetComponent<PlayerExperience>();
            if (xp != null)
            {
                xp.AddXP(amount);
                return;
            }
        }

        Debug.LogError($"[ServerRPC] Failed to award XP to player {playerId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrantXPToAllPlayersServerRpc(int amount)
    {
        if (!IsServer) return;

        Debug.Log($"[ServerRPC] Granting {amount} XP to all players");

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var xp = client.PlayerObject?.GetComponent<PlayerExperience>();
            if (xp != null)
            {
                xp.AddXP(amount);
            }
        }
    }

    private void HandleEnemyDeath(EnemyAI enemy)
    {
        if (!IsServer || enemy == null) return;

        // Get bubble from pool
        NetworkObject bubbleNetObj = poolManager.Get(expBubblePrefab);
        if (bubbleNetObj == null)
        {
            Debug.LogError("Failed to get ExpBubble from pool!");
            return;
        }

        ExpBubble bubble = bubbleNetObj.GetComponent<ExpBubble>();
        if (bubble == null)
        {
            Debug.LogError("ExpBubble component missing on prefab!");
            poolManager.Release(expBubblePrefab, bubbleNetObj);
            return;
        }

        // Initialize and position
        bubble.transform.position = enemy.transform.position;
        bubble.Initialize(enemy.transform.position, 10); // Default 10 XP

        // Spawn on network if needed
        if (!bubbleNetObj.IsSpawned)
        {
            bubbleNetObj.Spawn(true);
        }
    }
}