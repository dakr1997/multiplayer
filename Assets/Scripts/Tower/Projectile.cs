using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections;

public class Projectile : NetworkBehaviour
{
    public float speed = 10f;
    public float maxRadius = 10f;
    
    private Vector3 startPos;
    private Vector3 direction;
    private float damage;
    private string source;
    
    // Reference to the object pool that manages this projectile
    private IObjectPool<Projectile> objectPool;
    
    // Public property to set the object pool reference
    public IObjectPool<Projectile> ObjectPool { set => objectPool = value; }

    public void Initialize(Vector3 direction, float damage, string source)
    {
        SetDirection(direction);
        SetDamage(damage);
        SetSource(source);
        startPos = transform.position; // Reset start position when reused
    }

    public void SetSource(string src)
    {
        source = src;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Get the player's collider (only on server)
            Collider2D playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerCollider);
        }
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    void Update()
    {
        if (!IsServer) return;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, startPos) > maxRadius)
        {
            StartCoroutine(DelayedRelease(0.2f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Enemy"))
        {
            DamageHelper.ApplyDamage(other.gameObject, damage, "Projectile");
            StartCoroutine(DelayedRelease(0.2f));
        }
    }

    private IEnumerator DelayedRelease(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer && NetworkObject.IsSpawned)
        {
            if (objectPool != null)
            {
                NetworkObject.Despawn(false);
                // The actual return to pool happens in OnNetworkDespawn
            }
            else
            {
                // Fallback if pool isn't available
                NetworkObject.Despawn();
                Destroy(gameObject);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (objectPool != null)
        {
            // Return to pool only if this was a pooled object
            objectPool.Release(this);
        }
        
        base.OnNetworkDespawn();
    }

    void OnBecameInvisible()
    {
        if (!IsServer) return;
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedRelease(0.2f));
        }
    }
}