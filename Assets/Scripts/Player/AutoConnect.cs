using UnityEngine;

// Script temporal para probar la conexión sin UI
// Agrégalo al GameObject NetworkManager en la escena
// Cambia isHost a false en el segundo cliente (build)
public class AutoConnect : MonoBehaviour
{
    [Header("Configuración")]
    public bool   isHost   = true;        // true en editor, false en el build
    public string roomName = "EclipseraTest";
    public int    maxPlayers = 2;

    async void Start()
    {
        if (isHost)
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