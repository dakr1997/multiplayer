// Location: Core/Enemies/EnemyManager.cs
using System.Collections.Generic;
using UnityEngine;
using Core.Enemies.Base;

namespace Core.Enemies
{
    /// <summary>
    /// Manages enemy tracking and provides query methods for other systems
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        // Singleton pattern
        public static EnemyManager Instance { get; private set; }
        
        // Tracked enemies
        private readonly List<EnemyAI> activeEnemies = new List<EnemyAI>();
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }
        
        private void OnEnable()
        {
            // Subscribe to enemy events
            EnemyAI.OnEnemySpawned += RegisterEnemy;
            EnemyAI.OnEnemyDied += UnregisterEnemy;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from enemy events
            EnemyAI.OnEnemySpawned -= RegisterEnemy;
            EnemyAI.OnEnemyDied -= UnregisterEnemy;
        }
        
        /// <summary>
        /// Register an enemy with the manager
        /// </summary>
        public void RegisterEnemy(EnemyAI enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }
        
        /// <summary>
        /// Unregister an enemy from the manager
        /// </summary>
        public void UnregisterEnemy(EnemyAI enemy)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
            }
        }
        
        /// <summary>
        /// Get all active enemies
        /// </summary>
        public List<EnemyAI> GetAllActiveEnemies()
        {
            // Clean up any null references that might have occurred
            activeEnemies.RemoveAll(e => e == null);
            return new List<EnemyAI>(activeEnemies);
        }
        
        /// <summary>
        /// Get active enemies within range of a position
        /// </summary>
        public List<EnemyAI> GetActiveEnemiesInRange(Vector3 position, float range)
        {
            List<EnemyAI> enemiesInRange = new List<EnemyAI>();
            float rangeSqr = range * range; // Square the range to avoid square root calculations
            
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                
                // Use sqrMagnitude instead of Distance for better performance
                float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
                if (distanceSqr <= rangeSqr)
                {
                    enemiesInRange.Add(enemy);
                }
            }
            
            return enemiesInRange;
        }
        
        /// <summary>
        /// Get active enemies within range, sorted by distance (closest first)
        /// </summary>
        public List<EnemyAI> GetActiveEnemiesInRangeSorted(Vector3 position, float range)
        {
            List<EnemyAI> enemiesInRange = GetActiveEnemiesInRange(position, range);
            
            // Sort by distance from position
            enemiesInRange.Sort((a, b) => 
                Vector3.SqrMagnitude(a.transform.position - position)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - position)));
            
            return enemiesInRange;
        }
        
        /// <summary>
        /// Get the current number of active enemies
        /// </summary>
        public int GetActiveEnemyCount()
        {
            // Clean up any null references first
            activeEnemies.RemoveAll(e => e == null);
            return activeEnemies.Count;
        }
    }
}