// Location: Core/Interfaces/IDamageable.cs
namespace Core.Interfaces
{
    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object
        /// </summary>
        void TakeDamage(float amount, string source = null);
        
        /// <summary>
        /// Heal this object
        /// </summary>
        void Heal(float amount);
        
        /// <summary>
        /// Current health value
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Maximum health value
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// Whether the object is still alive
        /// </summary>
        bool IsAlive { get; }
    }
}