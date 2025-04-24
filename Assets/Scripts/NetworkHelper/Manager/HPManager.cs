using Unity.Netcode;
using UnityEngine;

public class HPManager : NetworkBehaviour
{
    public static HPManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    [ServerRpc(RequireOwnership = false)]
    public void RequestTowerHpUpdateServerRpc(int amount)
    {
        // Make sure this runs only on the server
        if (!IsServer) return;
        // Update the tower HP on the server side (if needed)
        UpdateTowerHP(amount);
    }

    private void UpdateTowerHP(int newAmount)
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var towerHealth = client.PlayerObject?.GetComponent<TowerHealth>();
            if (towerHealth != null)
            {
                towerHealth.SetHP(newAmount);
            }
        }
        Debug.Log($"[Server] Tower HP updated to {newAmount}");
        
    }



}
