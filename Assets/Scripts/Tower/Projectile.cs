using UnityEngine;
using Unity.Netcode;
using System.Collections; // For IEnumerator

public class Projectile : NetworkBehaviour // Inherit from NetworkBehaviour
{
    public float speed = 10f;
    public float maxRadius = 10f; // Max distance before despawn

    private Vector3 startPos;      // Starting position of the projectile
    private Vector3 direction;     // Direction of the projectile
    private float damage;          // Damage to be dealt
    private string source;         // Source of the projectile

    public void SetSource(string src)
    {
        source = src;
    }

    void Start()
    {
        startPos = transform.position; // Record starting position
                // Get the player's collider
        Collider2D playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider2D>();
        
        // Ignore the collision between the player's collider and this arrow's collider
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerCollider);
    }

    // Set the projectile's direction
    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    // Set the damage the projectile will deal
    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    void Update()
    {
        if (!IsServer) return; // Only process on the server

        // Move the projectile in the given direction
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Check if the projectile has traveled beyond its allowed radius
        if (Vector3.Distance(transform.position, startPos) > maxRadius)
        {
            StartCoroutine(DelayedDespawn(0.2f));  // Start the coroutine to despawn after a short delay
        }
    }

    // Trigger detection for collision with enemies
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Ensure this is handled on the server

        if (other.CompareTag("Enemy"))
        {
            DamageHelper.ApplyDamage(other.gameObject, damage, "Projectile");
            StartCoroutine(DelayedDespawn(0.2f));  // Despawn after a small delay when hitting an enemy
        }
    }

    // Coroutine that delays the despawn for visual sync
    private IEnumerator DelayedDespawn(float delay)
    {
        // Wait for the given delay time before despawning the projectile
        yield return new WaitForSeconds(delay);

        // Check if we're on the server and if the NetworkObject is spawned
        if (IsServer && NetworkObject.IsSpawned)
        {
            // Log for debugging
            Debug.Log("Despawning projectile after delay");

            // Now safely despawn the projectile
            NetworkObject.Despawn();
        }
        else
        {
            Debug.LogWarning("[Projectile] Tried to despawn, but it wasn't spawned or not on the server.");
        }
    }


    // Optional: Call this when the projectile is off-screen
    void OnBecameInvisible()
    {
        if (!IsServer) return; // Only run on the server
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedDespawn(0.2f));
        }
    }
}
