using UnityEngine;
using Fusion;

public class AutoConnect : MonoBehaviour
{
    async void Start()
    {
        // Esperar a que LobbyManager esté listo
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("LobbyManager no encontrado");
            return;
        }

        // Registrar el runner del lobby en GameNetworkManager
        NetworkRunner runner = LobbyManager.Instance.GetGameRunner();

        if (runner == null)
        {
            Debug.LogError("No hay GameRunner activo en LobbyManager");
            return;
        }

        GameNetworkManager.Instance.SetRunner(runner);
        Debug.Log("Runner registrado en GameNetworkManager desde LobbyManager");
    }
}