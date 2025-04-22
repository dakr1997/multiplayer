using UnityEngine;
using Unity.Netcode;

public class ExpBubble : NetworkBehaviour
{
    public float magnetSpeed = 5f;
    public float pickupRadius = 1.5f; // How close the player must be to pick up the bubble
    public float magnetRadius = 5f; // Maximum radius for the magnet effect
    public int expAmount = 10; // Amount of EXP to give to the player

    private Transform targetPlayer;
    private bool isCollected = false;
    private NetworkObject netObj;

    private void Start()
    {
        netObj = GetComponent<NetworkObject>(); // Get the network object for despawning
    }

    private void Update()
    {
        if (isCollected) return;

        if (targetPlayer == null)
        {
            targetPlayer = FindClosestPlayer();
        }

        if (targetPlayer != null)
        {
            Vector3 direction = (targetPlayer.position - transform.position).normalized;

            // Calculate distance between the bubble and the player
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            // If within the magnet radius, calculate a weaker force based on distance
            if (distance <= magnetRadius)
            {
                // The closer the player is, the stronger the pull (magnetic strength decreases with distance)
                float strength = 1 - (distance / magnetRadius); // This makes the effect weaker with distance
                transform.position += direction * magnetSpeed * strength * Time.deltaTime;
            }

            // If the player is within pickup radius, collect the bubble
            if (distance < pickupRadius)
            {
                CollectBubble();
            }
        }
    }

    private Transform FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject player in players)
        {

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }

        return closestPlayer;
    }

    private void CollectBubble()
    {
        Debug.Log("Collecting EXP bubble!");
        if (!IsServer) return; // Make sure the server handles the collection

        if (!netObj.IsSpawned)
        {
            Debug.LogError("Attempted to collect an object that is not spawned!");
            return;
        }

        // ✅ Define the players array
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("Found " + players.Length + " players.");
        // Loop through all players and give them EXP
        foreach (GameObject player in players)
        {
            Debug.Log("Giving EXP to player: " + player.name);
        }
        foreach (GameObject player in players)
        {
            PlayerExperience exp = player.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.GainEXPServerRpc(expAmount); // ServerRPC adds EXP and updates client UI
            }
        }

        netObj.Despawn();
        Destroy(gameObject);
    }

}
