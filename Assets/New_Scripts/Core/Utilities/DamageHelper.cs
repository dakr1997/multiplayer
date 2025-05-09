// Location: Core/Utilities/DamageHelper.cs
using UnityEngine;
using Core.Interfaces;
using Core.Components;

namespace Core.Utilities
{
    /// <summary>
    /// Static helper class for applying damage safely across the network
    /// </summary>
    public static class DamageHelper
    {
        /// <summary>
        /// Apply damage to a target if it's damageable and alive
        /// </summary>
        /// <param name="target">The GameObject to damage</param>
        /// <param name="amount">Amount of damage to apply</param>
        /// <param name="source">Optional source of the damage</param>
        /// <returns>True if damage was applied, false otherwise</returns>
        public static bool ApplyDamage(GameObject target, float amount, string source = null)
        {
            if (target == null)
                return false;
            
            // First check if the target has a HealthComponent
            HealthComponent healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                // Check if the target is still alive before applying damage
                if (!healthComponent.IsAlive)
                {
                    Debug.Log($"Target {target.name} is already dead, not applying damage");
                    return false;
                }
                
                // Apply damage through the HealthComponent
                healthComponent.TakeDamage(amount, source);
                return true;
            }
            
            // If no HealthComponent, try IDamageable interface
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Check if the target is still alive before applying damage
                if (!damageable.IsAlive)
                {
                    Debug.Log($"Target {target.name} is already dead, not applying damage");
                    return false;
                }
                
                // Apply damage through the IDamageable interface
                damageable.TakeDamage(amount, source);
                return true;
            }
            
            // No valid damage target found
            Debug.LogWarning($"Target {target.name} is not damageable.");
            return false;
        }
        
        /// <summary>
        /// Apply healing to a target if it's damageable
        /// </summary>
        /// <param name="target">The GameObject to heal</param>
        /// <param name="amount">Amount of healing to apply</param>
        /// <returns>True if healing was applied, false otherwise</returns>
        public static bool ApplyHealing(GameObject target, float amount)
        {
            if (target == null)
                return false;
            
            // First check if the target has a HealthComponent
            HealthComponent healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                // Only heal if the target is alive
                if (!healthComponent.IsAlive)
                {
                    Debug.Log($"Target {target.name} is dead, cannot heal");
                    return false;
                }
                
                // Apply healing through the HealthComponent
                healthComponent.Heal(amount);
                return true;
            }
            
            // If no HealthComponent, try IDamageable interface
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Only heal if the target is alive
                if (!damageable.IsAlive)
                {
                    Debug.Log($"Target {target.name} is dead, cannot heal");
                    return false;
                }
                
                // Apply healing through the IDamageable interface
                damageable.Heal(amount);
                return true;
            }
            
            // No valid healing target found
            Debug.LogWarning($"Target {target.name} is not healable.");
            return false;
        }
        
        /// <summary>
        /// Check if a target is alive
        /// </summary>
        /// <param name="target">The GameObject to check</param>
        /// <returns>True if the target is alive, false otherwise</returns>
        public static bool IsTargetAlive(GameObject target)
        {
            if (target == null)
                return false;
            
            // First check if the target has a HealthComponent
            HealthComponent healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                return healthComponent.IsAlive;
            }
            
            // If no HealthComponent, try IDamageable interface
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                return damageable.IsAlive;
            }
            
            // No valid target found, assume not alive for safety
            return false;
        }
    }
}