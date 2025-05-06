// Location: Core/Enemies/Base/PoolableEnemy.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Core.Interfaces;
using Core.Components;
using Core.Entities;
namespace Core.Enemies.Base
{
    /// <summary>
    /// Poolable implementation for enemy objects
    /// </summary>
    public class PoolableEnemy : PoolableNetworkObject
    {
        private EnemyEntity enemyEntity;
        private HealthComponent healthComponent;
        private EnemyAI aiComponent;
        private EnemyDamage damageComponent;
        
        private void Awake()
        {
            enemyEntity = GetComponent<EnemyEntity>();
            healthComponent = GetComponent<HealthComponent>();
            aiComponent = GetComponent<EnemyAI>();
            damageComponent = GetComponent<EnemyDamage>();
        }
        
        public override void OnSpawn()
        {
            base.OnSpawn();
            
            // Reset enemy state
            if (healthComponent != null)
            {
                // Reset health to max (will be scaled by multiplier during initialization)
                if (IsServer)
                {
                    healthComponent.Heal(healthComponent.MaxHealth);
                }
            }
            
            // Enable renderers that might have been disabled
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
            
            // Reset other components as needed
            if (aiComponent != null)
            {
                // Reset AI state if needed
            }
        }
        
        public override void OnDespawn()
        {
            base.OnDespawn();
            
            // Clean up any resources or references
            // For example, clear target lists, stop coroutines, etc.
        }
    }
}