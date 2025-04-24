using UnityEngine;
using Unity.Netcode;

public class TowerSpawner : NetworkBehaviour
{
    // This script is attached to an empty GameObject in the scene
    // It spawns creatures at regular intervals
    // It spawns them at the spawnPoint's position and rotation
    // Assign the enemy prefab and spawn point in the Inspector

    public GameObject TowerPrefab; // Assign this in the Inspector
    public Transform spawnPoint;   // Assign this in the Inspector


    void Start()
    {
        SpawnTower();
    }


    void SpawnTower()
    {
        // Check if enemyPrefab and spawnPoint are assigned
        if (TowerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("Assign creaturePrefab and spawnPoint in the Inspector!");
            return;
        }

        // Instantiate the enemy and spawn it on the network
        GameObject Tower = Instantiate(
            TowerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Make sure the enemy has a NetworkObject component for networking
        NetworkObject networkObject = Tower.GetComponent<NetworkObject>();

        // Spawn the enemy across all clients (only on the server)
        if (networkObject != null)
        {
            networkObject.Spawn(); // This will spawn the object across the network
        }

        Debug.Log("Tower spawned at: " + spawnPoint.position);
    }
}
