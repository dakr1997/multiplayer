using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections;

public class Projectile : PoolableNetworkObject
{
    public float speed = 10f;
    public float maxRadius = 10f;
    
    private Vector3 startPos;
    private Vector3 direction;
    private float damage;
    private string source;

    public void Initialize(Vector3 direction, float damage, string source)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.source = source;
        startPos = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            var playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerCollider);
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

        if (other.CompareTag("Enemy"))
        {
            DamageHelper.ApplyDamage(other.gameObject, damage, "Projectile");
            ReturnToPool(0.2f);
        }
    }

    void OnBecameInvisible()
    {
        if (!IsServer) return;
        ReturnToPool(0.2f);
    }
}