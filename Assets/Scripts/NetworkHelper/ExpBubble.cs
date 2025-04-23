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
    private NetworkObject netObj;

    private void Start()
    {
        netObj = GetComponent<NetworkObject>();
    }

    private void Update()
    {
        if (isCollected || !IsServer) return;

        targetPlayer ??= FindClosestPlayer();

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            // Magnetic attraction
            if (distance <= magnetRadius)
            {
                float strength = 1 - (distance / magnetRadius);
                transform.position += (targetPlayer.position - transform.position).normalized * 
                                    (magnetSpeed * strength * Time.deltaTime);
            }

            // Collection check
            if (distance < pickupRadius)
            {
                CollectBubble();
            }
        }
    }

    private Transform FindClosestPlayer()
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player.transform;
            }
        }
        return closest;
    }

    private void CollectBubble()
    {
        if (!IsServer || isCollected) return;
        
        isCollected = true;
        ulong collectorId = targetPlayer.GetComponent<NetworkObject>().OwnerClientId;
        XPManager.Instance.AwardXPToAll(expAmount);
        netObj.Despawn();
    }
}