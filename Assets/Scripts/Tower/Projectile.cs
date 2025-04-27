using UnityEngine;
using Unity.Netcode;

public class Projectile : PoolableNetworkObject
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float maxRadius = 10f;
    public LayerMask collisionLayers;
    
    private Vector3 startPos;
    private Vector3 direction;
    private float damage;
    private string source;
    private Collider2D projectileCollider;

    public void Initialize(Vector3 direction, float damage, string source)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.source = source;
        startPos = transform.position;
        
        // Reset any potential velocity if using rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            projectileCollider = GetComponent<Collider2D>();
            var playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider2D>();
            if (playerCollider != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, playerCollider);
            }
        }
    }

    void Update()
    {
        if (!IsServer) return;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, startPos) > maxRadius)
        {
            ReturnToPool(0.2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            DamageHelper.ApplyDamage(other.gameObject, damage, source);
            ReturnToPool(0.2f);
        }
    }
}