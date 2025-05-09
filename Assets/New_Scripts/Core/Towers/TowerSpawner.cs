// Location: Core/Towers/TowerSpawner.cs
using UnityEngine;
using Unity.Netcode;

namespace Core.Towers
{
    public class TowerSpawner : NetworkBehaviour
    {
        [SerializeField] GameObject towerPrefab;
        [SerializeField] Transform spawnPoint;
        
        // Reference to spawned tower
        private GameObject _spawnedTower;
        
        void Start()
        {
            // Only spawn tower if we're the server and network is active
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                SpawnTower();
            }
            else
            {
                Debug.Log("TowerSpawner: Waiting for network to be ready before spawning");
            }
        }
        
        void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }
        
        void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }
        
        void OnServerStarted()
        {
            // Network is now ready, spawn tower if we're the server
            if (NetworkManager.Singleton.IsServer)
            {
                SpawnTower();
            }
        }
        
        public void SpawnTower()
        {
            if (_spawnedTower != null)
            {
                Debug.LogWarning("Tower already spawned!");
                return;
            }
            
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            
            _spawnedTower = Instantiate(towerPrefab, position, Quaternion.identity);
            
            // Only spawn as networked object if network is active
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkObject networkObject = _spawnedTower.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                }
                else
                {
                    Debug.LogError("Tower prefab is missing NetworkObject component!");
                }
            }
        }
    }
}