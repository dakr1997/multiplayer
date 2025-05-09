using Unity.Netcode;
using UnityEngine;
using Core.GameState; 
using Core.Towers.MainTower;
using Core.Components;
using System.Collections;
using Core.Player.Components;
using Core.Player.UI;

namespace Core.Lobby
{
    public class GameSceneInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject _playerHUDPrefab;
        
        private void Start()
        {
            Debug.Log("Game Scene loaded - initializing");
            
            // Wait a short time to ensure network objects are ready
            StartCoroutine(DelayedInitialization());
        }
        
        private IEnumerator DelayedInitialization()
        {
            // Wait for network manager to be ready
            while (NetworkManager.Singleton == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // Wait a bit more for players to be created
            yield return new WaitForSeconds(0.5f);
            
            // Initialize game state if we're the server
            if (NetworkManager.Singleton.IsServer)
            {
                InitializeGameState();
            }
            
            // For clients, the HUD will be created by PlayerClientHandler
            // But we can help ensure it works by checking if it exists
            if (NetworkManager.Singleton.IsClient && _playerHUDPrefab != null)
            {
                // Wait for local player to exist
                float waitTime = 0f;
                while (NetworkManager.Singleton.LocalClient == null || 
                       NetworkManager.Singleton.LocalClient.PlayerObject == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                    
                    if (waitTime > 10.0f)
                    {
                        Debug.LogWarning("Timed out waiting for local player to spawn!");
                        break;
                    }
                }
                
                // Check if HUD already exists
                if (FindObjectOfType<PlayerHUDController>() == null)
                {
                    Debug.Log("No HUD found, contiuing...");
                }
            }
        }
        
        private void InitializeGameState()
        {
            // Set game state to Wave when the game scene is loaded
            var gameStateManager = GameManagement.GameServices.Get<GameStateManager>();
            if (gameStateManager != null)
            {
                gameStateManager.ChangeState(GameStateType.Wave);
                Debug.Log("Game state changed to Wave");
            }
            else
            {
                Debug.LogError("GameStateManager not found!");
            }
        }
    }
}