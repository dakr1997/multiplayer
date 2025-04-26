using Unity.Netcode;
using UnityEngine;

public class XPManager : NetworkBehaviour
{
    public static XPManager Instance { get; private set; }

    [SerializeField] private GameObject expBubblePrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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

    // ClientRpc to notify clients about tower HP change
    [ClientRpc]
    private void NotifyClientsOfTowerHPChangeClientRpc(int amount)
    {
        {
            Debug.LogError("[XPManager] TowerHealth component missing on player object!");
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

        var bubbleObj = Instantiate(expBubblePrefab, enemy.transform.position, Quaternion.identity);
        var bubble = bubbleObj.GetComponent<ExpBubble>();

        if (bubble != null)
        {
            bubble.expAmount = 10;

            if (bubbleObj.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("[Server] ExpBubble is missing NetworkObject!");
                Destroy(bubbleObj);
            }
        }
        else
        {
            Debug.LogError("[Server] ExpBubble prefab missing ExpBubble component!");
            Destroy(bubbleObj);
        }
    }
}
