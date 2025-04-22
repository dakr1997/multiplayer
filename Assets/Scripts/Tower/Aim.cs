using UnityEngine;
using System.Collections.Generic;

public class Aim
{
    private List<Transform> targets = new List<Transform>();
    private Dictionary<Transform, Queue<Vector3>> positionHistory = new Dictionary<Transform, Queue<Vector3>>();
    private int positionSamples = 5; // Number of samples to track for velocity estimation

    public List<Transform> Targets => targets;

    public void AddTarget(Transform target)
    {
        if (!targets.Contains(target))
        {
            targets.Add(target);
            positionHistory[target] = new Queue<Vector3>();
        }
    }

    public void RemoveTarget(Transform target)
    {
        if (targets.Contains(target))
        {
            targets.Remove(target);
            positionHistory.Remove(target);
        }
    }

    private Vector3 CalculateVelocity(Transform target)
    {
        if (!positionHistory.ContainsKey(target))
            return Vector3.zero;

        Queue<Vector3> history = positionHistory[target];
        history.Enqueue(target.position);

        // Limit the number of samples
        if (history.Count > positionSamples)
            history.Dequeue();

        if (history.Count < 2)
            return Vector3.zero;

        // Calculate velocity based on position change
        Vector3 oldest = history.Peek();
        Vector3 current = target.position;
        return (current - oldest) / (history.Count - 1);
    }

    public Vector3 PredictTargetPosition(Transform target, float predictionTime)
    {
        Vector3 velocity = CalculateVelocity(target);
        return target.position + velocity * predictionTime;
    }

    public Vector3? GetPredictedTargetPosition(float predictionTime)
    {
        if (targets.Count == 0)
            return null;

        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;
        Vector3 predictedPosition = Vector3.zero;

        foreach (Transform target in targets)
        {
            Vector3 futurePosition = PredictTargetPosition(target, predictionTime);
            float distance = Vector3.Distance(futurePosition, target.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target;
                predictedPosition = futurePosition;
            }
        }

        return bestTarget != null ? predictedPosition : (Vector3?)null;
    }
}
