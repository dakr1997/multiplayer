using UnityEngine;
using Unity.Netcode;

public class ExpBubble : PoolableNetworkObject
{
    [Header("Settings")]
    [SerializeField] private float magnetSpeed = 5f;
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private float magnetRadius = 5f;
    [SerializeField] private float lifetimeSeconds = 30f;
    [SerializeField] private float clientPredictionStrength = 1.5f; // Higher = faster catch-up
    
    // Network variables - keep these minimal
    public NetworkVariable<int> expAmount = new NetworkVariable<int>(
        10, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    public NetworkVariable<ulong> targetPlayerNetId = new NetworkVariable<ulong>(
        0, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // State
    private Transform targetPlayer;
    private bool isCollected = false;
    private float spawnTime;
    
    // Client prediction
    private Transform clientTargetPlayer;
    private Vector3 clientPredictionVelocity;
    private Vector3 lastKnownPosition;
    private float lastPositionUpdateTime;
    
    public void Initialize(Vector3 position, int xpValue)
    {
        if (IsServer)
        {
            transform.position = position;
            expAmount.Value = xpValue;
            isCollected = false;
            targetPlayer = null;
            spawnTime = Time.time;
            targetPlayerNetId.Value = 0;
            
            Debug.Log($"ExpBubble initialized at {position} with {xpValue} XP");
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Client tracking variables
        lastKnownPosition = transform.position;
        lastPositionUpdateTime = Time.time;
        
        // Subscribe to network variable changes
        if (IsClient && !IsServer)
        {
            targetPlayerNetId.OnValueChanged += OnTargetPlayerChanged;
        }
    }
    
    public override void OnSpawn()
    {
        base.OnSpawn();
        isCollected = false;
        targetPlayer = null;
        spawnTime = Time.time;
        clientTargetPlayer = null;
        clientPredictionVelocity = Vector3.zero;
    }
    
    private void OnTargetPlayerChanged(ulong previousId, ulong newId)
    {
        if (!IsServer && newId != 0)
        {
            // Find the player on the client side
            FindClientPlayer(newId);
        }
    }
    
    private void FindClientPlayer(ulong playerId)
    {
        clientTargetPlayer = null;
        
        if (NetworkManager.Singleton != null && 
            NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client) && 
            client.PlayerObject != null)
        {
            clientTargetPlayer = client.PlayerObject.transform;
            Debug.Log($"Client found target player with ID {playerId}");
        }
    }
    
    private void Update()
    {
        if (IsServer)
        {
            ServerUpdate();
        }
        else if (IsClient)
        {
            ClientUpdate();
        }
    }
    
    private void ServerUpdate()
    {
        // Check lifetime
        if (Time.time - spawnTime > lifetimeSeconds)
        {
            ReturnToPool();
            return;
        }
        
        if (isCollected) return;
        
        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, targetPlayer.position);
        
        // Apply magnetic effect - simple and direct
        if (distance <= magnetRadius)
        {
            float strength = 1 - (distance / magnetRadius);
            
            // Move towards player with speed based on distance
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPlayer.position, 
                magnetSpeed * strength * Time.deltaTime
            );
            
            // Update target player ID for clients
            if (targetPlayer.TryGetComponent<NetworkObject>(out var netObj))
            {
                targetPlayerNetId.Value = netObj.OwnerClientId;
            }
        }
        
        // Check pickup
        if (distance <= pickupRadius)
        {
            CollectBubble();
        }
    }
    
    private void ClientUpdate()
    {
        // Skip if we're the server (already handled in ServerUpdate)
        if (IsServer) return;
        
        // Track position updates from server
        if (transform.position != lastKnownPosition)
        {
            lastKnownPosition = transform.position;
            lastPositionUpdateTime = Time.time;
        }
        
        // If we have a target and we're in the magnetic radius, apply client prediction
        if (clientTargetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, clientTargetPlayer.position);
            
            if (distance <= magnetRadius)
            {
                // Calculate strength based on distance
                float strength = 1 - (distance / magnetRadius);
                
                // Calculate predicted position
                Vector3 idealPosition = Vector3.MoveTowards(
                    transform.position,
                    clientTargetPlayer.position,
                    magnetSpeed * strength * Time.deltaTime * clientPredictionStrength
                );
                
                // Smoothly move toward the predicted position (client-side only visualization)
                transform.position = idealPosition;
            }
        }
    }
    
    private void FindNearestPlayer()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in FindNearestPlayer!");
            return;
        }
        
        float closestDistance = float.MaxValue;
        targetPlayer = null;
        ulong targetId = 0;
        
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
                    targetId = client.ClientId;
                }
            }
        }
        
        // Update the network variable with the new target ID
        if (targetPlayer != null && targetId != targetPlayerNetId.Value)
        {
            targetPlayerNetId.Value = targetId;
        }
    }
    
    private void CollectBubble()
    {
        if (!IsServer || isCollected) return;
        
        isCollected = true;
        
        // Award XP
        if (XPManager.Instance == null)
        {
            Debug.LogError("XPManager.Instance is null in CollectBubble!");
            ReturnToPool(0.5f);
            return;
        }
        
        XPManager.Instance.AwardXPToAll(expAmount.Value);
        Debug.Log($"XP Bubble collected: Awarded {expAmount.Value} XP to all players");
        
        // Play effects
        PlayCollectionEffectsClientRpc();
        
        // Return to pool
        ReturnToPool(0.0f);
    }
    
    [ClientRpc]
    private void PlayCollectionEffectsClientRpc()
    {
        // Play collection effects
        // You could add particle effects, sound, etc.
    }
    
    public override void OnDespawn()
    {
        base.OnDespawn();
        isCollected = false;
        targetPlayer = null;
        clientTargetPlayer = null;
        clientPredictionVelocity = Vector3.zero;
    }
}