// Location: Core/Towers/Utilities/Aim.cs
using UnityEngine;
using System.Collections.Generic;

namespace Core.Towers.Utilities
{
    public class Aim
    {
        private List<Transform> targets = new List<Transform>();
        private Dictionary<Transform, Queue<Vector3>> positionHistory = new Dictionary<Transform, Queue<Vector3>>();
        private int positionSamples = 5; // Number of samples to track for velocity estimation

        public List<Transform> Targets => targets;

        public void AddTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogError("Attempting to add null target to Aim!");
                return;
            }
            
            if (!targets.Contains(target))
            {
                targets.Add(target);
                positionHistory[target] = new Queue<Vector3>();
                Debug.Log($"Target added to aim system: {target.name}");
            }
        }

        public void RemoveTarget(Transform target)
        {
            if (targets.Contains(target))
            {
                targets.Remove(target);
                positionHistory.Remove(target);
                Debug.Log($"Target removed from aim system: {target.name}");
            }
        }

        private Vector3 CalculateVelocity(Transform target)
        {
            if (target == null)
            {
                Debug.LogError("Attempting to calculate velocity for null target!");
                return Vector3.zero;
            }
            
            if (!positionHistory.ContainsKey(target))
            {
                Debug.LogWarning($"No position history for target {target.name}");
                return Vector3.zero;
            }

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
            Vector3 velocity = (current - oldest) / (history.Count - 1);
            
            return velocity;
        }

        public Vector3 PredictTargetPosition(Transform target, float predictionTime)
        {
            if (target == null)
            {
                Debug.LogError("Attempting to predict position for null target!");
                return Vector3.zero;
            }
            
            Vector3 velocity = CalculateVelocity(target);
            Vector3 predictedPos = target.position + velocity * predictionTime;
            return predictedPos;
        }

        public Vector3? GetPredictedTargetPosition(float predictionTime)
        {
            if (targets.Count == 0)
            {
                return null;
            }

            Transform bestTarget = null;
            float closestDistance = Mathf.Infinity;
            Vector3 predictedPosition = Vector3.zero;

            foreach (Transform target in targets)
            {
                if (target == null) continue;
                
                Vector3 futurePosition = PredictTargetPosition(target, predictionTime);
                float distance = Vector3.Distance(futurePosition, target.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = target;
                    predictedPosition = futurePosition;
                }
            }

            if (bestTarget != null)
            {
                return predictedPosition;
            }
            else
            {
                return null;
            }
        }
    }
}