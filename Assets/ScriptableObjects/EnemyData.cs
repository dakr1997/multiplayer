using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Enemy";
    public Sprite enemySprite;
    
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public float damage = 10f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    
    [Header("Rewards")]
    public int experienceReward = 10;
    
    [Header("Visuals")]
    public GameObject deathEffectPrefab;
    
    //[Header("Movement")]
    public enum MovementType
    {
        Direct,
        Wander,
        Patrol
    }
    
    public MovementType movementType = MovementType.Direct;
    [Range(0f, 1f)] public float randomMovementWeight = 0.3f;
    [Range(0f, 1f)] public float targetSeekingWeight = 0.7f;
}