using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service locator for game services.
/// </summary>
public static class GameServices
{
    private static Dictionary<Type, object> _services = new Dictionary<Type, object>();
    
    /// <summary>
    /// Register a service.
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"Service of type {type.Name} already registered! Replacing previous instance.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
            Debug.Log($"Service of type {type.Name} registered successfully.");
        }
    }
    
    /// <summary>
    /// Get a service.
    /// </summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);
        
        if (_services.TryGetValue(type, out object service))
        {
            return (T)service;
        }
        
        Debug.LogWarning($"Service of type {type.Name} not registered!");
        return null;
    }
    
    /// <summary>
    /// Try to get a service with retries.
    /// </summary>
    public static System.Collections.IEnumerator GetAsync<T>(Action<T> callback, int maxRetries = 5, float retryDelay = 0.5f) where T : class
    {
        int attempts = 0;
        T service = null;
        
        while (service == null && attempts < maxRetries)
        {
            service = Get<T>();
            
            if (service == null)
            {
                Debug.Log($"Waiting for service {typeof(T).Name} to be available... (Attempt {attempts+1}/{maxRetries})");
                yield return new WaitForSeconds(retryDelay);
                attempts++;
            }
        }
        
        callback?.Invoke(service);
        
        if (service == null)
        {
            Debug.LogError($"Failed to get service {typeof(T).Name} after {maxRetries} attempts!");
        }
    }
    
    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }
    
    /// <summary>
    /// Unregister a service.
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
            Debug.Log($"Service of type {type.Name} unregistered.");
        }
    }
    
    /// <summary>
    /// Clear all services.
    /// </summary>
    public static void Clear()
    {
        _services.Clear();
    }
}