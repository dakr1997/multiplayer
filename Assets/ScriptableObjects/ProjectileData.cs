using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileData", menuName = "Tower Defense/Projectile Data")]
public class ProjectileData : ScriptableObject
{
    [Header("Basic Info")]
    public string projectileName = "Projectile";
    public Sprite projectileSprite;
    
    [Header("Movement")]
    public float speed = 10f;
    public float maxDistance = 20f;
    
    [Header("Damage")]
    public float damage = 10f;
    public bool piercing = false;
    public int maxTargets = 1;
    
    [Header("Visuals")]
    public GameObject hitEffectPrefab;
    public TrailRenderer trailEffect;
}