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
            
            Debug.Log($"[PoolableEnemy] OnSpawn called for {gameObject.name}");
            
            // Reset enemy state through the entity
            if (enemyEntity != null && IsServer)
            {
                enemyEntity.ResetState();
            }
            else
            {
                // Fallback in case there's no entity component
                // Reset health component directly
                if (healthComponent != null && IsServer)
                {
                    healthComponent.ResetState();
                }
                
                // Reset AI component
                if (aiComponent != null)
                {
                    aiComponent.enabled = true;
                    // Reset AI state if it has a method for it
                    if (aiComponent.GetType().GetMethod("ResetState") != null)
                    {
                        aiComponent.SendMessage("ResetState", SendMessageOptions.DontRequireReceiver);
                    }
                }
                
                // Reset damage component
                if (damageComponent != null)
                {
                    damageComponent.enabled = true;
                }
                
                // Reset renderers
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = true;
                }
                
                // Reset colliders
                foreach (var collider in GetComponentsInChildren<Collider2D>())
                {
                    collider.enabled = true;
                }
                
                // Reset rigidbody
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }
        
        public override void OnDespawn()
        {
            base.OnDespawn();
            
            Debug.Log($"[PoolableEnemy] OnDespawn called for {gameObject.name}");
            
            // Clean up any resources or references
            // For example, clear target lists, stop coroutines, etc.
        }
    }
}