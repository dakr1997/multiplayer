using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ExpBubblePool : NetworkBehaviour
{
    public static ExpBubblePool Instance;
    
    [SerializeField] private ExpBubble expBubblePrefab;
    [SerializeField] private Transform poolParent;
    [SerializeField] private int initialPoolSize = 10;

    private Queue<ExpBubble> pool = new Queue<ExpBubble>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBubble();
        }
    }

    private ExpBubble CreateNewBubble()
    {
        ExpBubble bubble = Instantiate(expBubblePrefab, poolParent);
        bubble.GetComponent<NetworkObject>().Spawn(); // Network spawn
        bubble.gameObject.SetActive(false);
        pool.Enqueue(bubble);
        return bubble;
    }

    public ExpBubble GetFromPool(Vector3 position)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("Pool empty, creating new ExpBubble");
            return CreateNewBubble();
        }

        ExpBubble bubble = pool.Dequeue();
        bubble.transform.position = position;
        bubble.ResetBubble();
        bubble.gameObject.SetActive(true);
        return bubble;
    }

    public void ReturnToPool(ExpBubble bubble)
    {
        bubble.gameObject.SetActive(false);
        bubble.transform.SetParent(poolParent);
        pool.Enqueue(bubble);
    }
}