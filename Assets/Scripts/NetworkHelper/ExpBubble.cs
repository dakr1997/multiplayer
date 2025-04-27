using UnityEngine;
using Unity.Netcode;

public class ExpBubble : PoolableNetworkObject
{
    [Header("Settings")]
    public float magnetSpeed = 5f;
    public float pickupRadius = 1.5f;
    public float magnetRadius = 5f;
    public int expAmount = 10;

    private Transform targetPlayer;
    private bool isCollected = false;

    public void Initialize(Vector3 position, int expValue)
    {
        transform.position = position;
        expAmount = expValue;
        isCollected = false;
        targetPlayer = null;
    }

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
        
        // Award XP
        XPManager.Instance.AwardXPToAll(expAmount);
        
        // Return to pool
        ReturnToPool();
    }

    public override void OnNetworkDespawn()
    {
        // Reset state when returned to pool
        isCollected = false;
        targetPlayer = null;
        
        base.OnNetworkDespawn();
    }
}