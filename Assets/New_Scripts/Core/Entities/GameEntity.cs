using UnityEngine;
using Unity.Netcode;
using Core.Components;

namespace Core.Entities
{
    /// <summary>
    /// Base component entity class for all game objects
    /// </summary>
    public class GameEntity : NetworkBehaviour
    {
        // Common components
        public HealthComponent Health { get; private set; }
        
        protected virtual void Awake()
        {
            // Cache component references
            Health = GetComponent<HealthComponent>();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Initialize components after network spawn
            InitializeComponents();
        }
        
        protected virtual void InitializeComponents()
        {
            // Override in derived classes to initialize components
        }
        
        // Utility to get or add components at runtime
        public T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }
    }
}