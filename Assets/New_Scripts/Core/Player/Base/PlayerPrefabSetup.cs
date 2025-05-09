// Location: Core/Player/Base/PlayerPrefabSetup.cs
using UnityEngine;
using Unity.Netcode;
using Core.Components;
using Core.Player.Components;
using Core.Player.Input;

namespace Core.Player.Base
{
    /// <summary>
    /// Utility script to set up player prefabs with required components
    /// </summary>
    [ExecuteInEditMode]
    public class PlayerPrefabSetup : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private bool autoAddRequiredComponents = true;
        
        [Header("Player Configuration")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("Network Configuration")]
        [SerializeField] private bool clientAuthoritative = true;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoAddRequiredComponents)
            {
                AddRequiredComponents();
            }
        }
        
        private void AddRequiredComponents()
        {
            // Add NetworkObject if missing
            if (GetComponent<NetworkObject>() == null)
            {
                gameObject.AddComponent<NetworkObject>();
                Debug.Log("Added NetworkObject component");
            }
            
            // Add Rigidbody2D if missing
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Debug.Log("Added Rigidbody2D component with appropriate settings");
            }
            
            // Add Network Transform if missing
            if (GetComponent<Unity.Netcode.Components.NetworkTransform>() == null)
            {
                if (clientAuthoritative)
                {
                    gameObject.AddComponent<PlayerNetworkTransform>();
                    Debug.Log("Added PlayerNetworkTransform component (client authoritative)");
                }
                else
                {
                    gameObject.AddComponent<Unity.Netcode.Components.NetworkTransform>();
                    Debug.Log("Added NetworkTransform component (server authoritative)");
                }
            }
            
            // Add health component if missing
            HealthComponent health = GetComponent<HealthComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
                // Just set properties directly - no SerializedObject in build
                var field = health.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(health, maxHealth);
                
                Debug.Log("Added HealthComponent with max health: " + maxHealth);
            }
            
            // Add PlayerEntity if missing
            if (GetComponent<PlayerEntity>() == null)
            {
                gameObject.AddComponent<PlayerEntity>();
                Debug.Log("Added PlayerEntity component");
            }
            
            // Add PlayerMovement if missing
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = gameObject.AddComponent<PlayerMovement>();
                // Set property directly - no SerializedObject in build
                var field = movement.GetType().GetField("moveSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(movement, moveSpeed);
                
                Debug.Log("Added PlayerMovement component with speed: " + moveSpeed);
            }
            
            // Add PlayerExperience if missing
            if (GetComponent<PlayerExperience>() == null)
            {
                gameObject.AddComponent<PlayerExperience>();
                Debug.Log("Added PlayerExperience component");
            }
            
            // Add PlayerInputHandler if missing
            if (GetComponent<PlayerInputHandler>() == null)
            {
                gameObject.AddComponent<PlayerInputHandler>();
                Debug.Log("Added PlayerInputHandler component");
            }
            
            // Add animator if missing
            if (GetComponent<Animator>() == null)
            {
                gameObject.AddComponent<Animator>();
                Debug.Log("Added Animator component (no controller assigned)");
            }
            
            // Add network animator if missing
            if (GetComponent<PlayerNetworkAnimator>() == null)
            {
                gameObject.AddComponent<PlayerNetworkAnimator>();
                Debug.Log("Added PlayerNetworkAnimator component");
            }
        }
#endif
    }
}