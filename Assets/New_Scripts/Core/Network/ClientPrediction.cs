using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Netcode.Components;
namespace Core.Network
{
    public struct TransformState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public uint InputSequence;
        public double Timestamp;
    }
    
    public class ClientPrediction : MonoBehaviour
    {
        [SerializeField] private float interpolationBackTime = 0.1f; // 100ms buffer
        [SerializeField] private int maxBufferSize = 30;
        
        private Queue<TransformState> stateBuffer = new Queue<TransformState>();
        private TransformState[] stateHistory = new TransformState[64]; // Circular buffer
        private int historyIndex = 0;
        private uint nextInputSequence = 0;
        
        // Reference to network transform
        private NetworkTransform networkTransform;
        private Rigidbody2D rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            networkTransform = GetComponent<NetworkTransform>();
        }
        
        /// <summary>
        /// Process player input locally and store for reconciliation
        /// </summary>
        public void ProcessLocalInput(Vector2 inputVector, float speed)
        {
            if (rb == null) return;
            
            // Apply input locally immediately
            rb.linearVelocity = inputVector * speed;
            
            // Store state for reconciliation
            stateHistory[historyIndex] = new TransformState
            {
                Position = transform.position,
                Rotation = transform.rotation,
                InputSequence = nextInputSequence,
                Timestamp = Time.timeAsDouble
            };
            
            historyIndex = (historyIndex + 1) % stateHistory.Length;
            nextInputSequence++;
            
            // Send input to server (you'd implement a ServerRpc here)
        }
        
        /// <summary>
        /// Update from server - reconcile client prediction
        /// </summary>
        public void ReconcileWithServer(Vector3 serverPosition, Quaternion serverRotation, uint lastProcessedInput)
        {
            // Find the state that matches the last processed input
            for (int i = 0; i < stateHistory.Length; i++)
            {
                if (stateHistory[i].InputSequence == lastProcessedInput)
                {
                    // Calculate error between server and client prediction
                    Vector3 error = serverPosition - stateHistory[i].Position;
                    
                    if (error.sqrMagnitude > 0.0001f) // Only fix if error is significant
                    {
                        transform.position = serverPosition;
                        
                        // Re-apply inputs that came after the reconciled state
                        ReapplyInputs(i, lastProcessedInput);
                    }
                    
                    break;
                }
            }
        }
        
        private void ReapplyInputs(int fromIndex, uint fromSequence)
        {
            // Reapply all inputs that came after the reconciled state
            // This is simplified - you'd need to store actual inputs and their results
        }
        
        /// <summary>
        /// Add a server state to the buffer for interpolation
        /// </summary>
        public void AddServerState(Vector3 position, Quaternion rotation, double timestamp)
        {
            // For client interpolation between server states
            TransformState newState = new TransformState
            {
                Position = position,
                Rotation = rotation,
                Timestamp = timestamp
            };
            
            stateBuffer.Enqueue(newState);
            
            // Limit buffer size
            while (stateBuffer.Count > maxBufferSize)
            {
                stateBuffer.Dequeue();
            }
        }
        
        /// <summary>
        /// Interpolate between server states for smooth visuals
        /// </summary>
        public void InterpolateServerStates()
        {
            // This would be used for other network objects (not locally controlled)
            // Simplified - you'd interpolate based on timestamps
        }
    }
}