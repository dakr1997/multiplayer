using Unity.Netcode;
using UnityEngine;

public class XPManager : NetworkBehaviour
{
    public static XPManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GrantXPServerRpc(ulong playerId, int amount)
    {
        if (!IsServer) return;

        var player = NetworkManager.SpawnManager.GetPlayerNetworkObject(playerId);
        if (player != null && player.TryGetComponent<PlayerExperience>(out var xp))
        {
            xp.AddXP(amount);
        }
    }
    [ServerRpc(RequireOwnership = false)]    
    public void GrantXPToAllPlayersServerRpc(int amount)
    {
        if (!IsServer) return;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = clientPair.Value.PlayerObject;
            if (playerObject != null && playerObject.TryGetComponent<PlayerExperience>(out var xp))
            {
                xp.AddXP(amount);
            }
        }

        Debug.Log($"[XPManager] Granted {amount} XP to all players.");
    }

    public void AwardXP(ulong collectorId, int amount)
    {
        if (IsServer)
        {
            // Direct call if already on server
            GrantXPServerRpc(collectorId, amount);
        }
        else
        {
            // Client request to server
            GrantXPServerRpc(collectorId, amount);
        }
    }

    public void AwardXPToAll(int amount)
    {
        if (IsServer)
        {
            GrantXPToAllPlayersServerRpc(amount); // Direct if server
        }
        else
        {
            GrantXPToAllPlayersServerRpc(amount); // Request if client
        }
    }
}