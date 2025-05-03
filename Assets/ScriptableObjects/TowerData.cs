using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Basic Info")]
    public string towerName = "Tower";
    public Sprite towerSprite;
    
    [Header("Stats")]
    public float maxHealth = 100f;
    public float attackRange = 5f;
    public float fireRate = 1f;
    
    [Header("Projectile")]
    public ProjectileData projectileData;
    
    // Remove the Transform shootPoint reference - it doesn't work in ScriptableObjects
    
    [Header("Visual")]
    public GameObject buildEffectPrefab;
    public GameObject destroyEffectPrefab;
}