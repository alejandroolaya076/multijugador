using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    public bool   isHost     = true;
    public string roomName   = "EclipseraTest";
    public int    maxPlayers = 2;

    async void Start()
{
    #if UNITY_EDITOR
        bool host = isHost; // Editor → Host (isHost = true en Inspector)
    #else
        bool host = false;  // Build → siempre Client
    #endif

    if (host)
    {
        Debug.Log("Iniciando como HOST...");
        await GameNetworkManager.Instance.HostGame(roomName, maxPlayers);
    }
    else
    {
        Debug.Log("Conectando como CLIENT...");
        await GameNetworkManager.Instance.JoinGame(roomName);
    }
}
}