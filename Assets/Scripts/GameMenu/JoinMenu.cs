using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;  // Add this line for UnityTransport
public class JoinMenu : MonoBehaviour
{
    private NetworkManager m_NetworkManager;
    private string m_ServerIP = "127.0.0.1"; // Default to localhost

    [SerializeField] private InputField ipInputField; // Assign in Inspector

    private void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        
        // If using UI InputField
        if (ipInputField != null)
        {
            ipInputField.text = m_ServerIP;
            ipInputField.onEndEdit.AddListener(UpdateIPAddress);
        }
    }

    private void UpdateIPAddress(string newIP)
    {
        m_ServerIP = newIP;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            StartButtons();
            
            // IP input field (fallback if UI InputField isn't used)
            GUILayout.Label("Server IP:");
            m_ServerIP = GUILayout.TextField(m_ServerIP, GUILayout.Width(200));
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    private void StartButtons()
    {
        if (GUILayout.Button("Host")) 
        {
            SetTransportIP();
            m_NetworkManager.StartHost();
        }
        
        if (GUILayout.Button("Client")) 
        {
            SetTransportIP();
            m_NetworkManager.StartClient();
        }
        
        if (GUILayout.Button("Server")) 
        {
            SetTransportIP();
            m_NetworkManager.StartServer();
        }
    }

    private void SetTransportIP()
    {
        // For Unity Transport (recommended)
        if (m_NetworkManager.TryGetComponent<UnityTransport>(out var transport))
        {
            transport.ConnectionData.Address = m_ServerIP;
        }
    }

    private void StatusLabels()
    {
        var mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("Connected to: " + m_ServerIP);
    }
}