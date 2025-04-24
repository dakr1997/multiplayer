using UnityEngine;
using Unity.Netcode;

public class ExpBubble : NetworkBehaviour
{
    [Header("Settings")]
    public float magnetSpeed = 5f;
    public float pickupRadius = 1.5f;
    public float magnetRadius = 5f;
    public int expAmount = 10;

    private Transform targetPlayer;
    private bool isCollected = false;

    private void Update()
    {
        if (isCollected || !IsServer) return;

        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if (distance <= magnetRadius)
        {
            float strength = 1 - (distance / magnetRadius);
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPlayer.position, 
                magnetSpeed * strength * Time.deltaTime
            );
        }

        if (distance <= pickupRadius)
        {
            CollectBubble();
        }
    }

    private void FindNearestPlayer()
    {
        float closestDistance = float.MaxValue;
        targetPlayer = null;
        Debug.Log($"ConnectedClients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
        Debug.Log($"Client {client.ClientId} has PlayerObject: {client.PlayerObject != null}");
}
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float distance = Vector3.Distance(
                    transform.position, 
                    client.PlayerObject.transform.position
                );
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetPlayer = client.PlayerObject.transform;
                }
            }
        }
    }

    private void CollectBubble()
    {
        if (!IsServer || isCollected) return;
        
        isCollected = true;
        
        // Get the collector's client ID
        ulong collectorId = targetPlayer.GetComponent<NetworkObject>().OwnerClientId;
        Debug.Log($"[SERVER] Bubble collected by player {collectorId}");
        
        // Award XP to all Players
        XPManager.Instance.AwardXPToAll(expAmount);
        
        // Despawn the bubble
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}