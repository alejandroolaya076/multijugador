using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager Instance { get; private set; }

    private NetworkRunner _lobbyRunner;
    private NetworkRunner _gameRunner;

    public List<SessionInfo> SesionesDisponibles { get; private set; } = new();
    public event Action OnSesionesActualizadas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
        public NetworkRunner GetGameRunner()
    {
        return _gameRunner;
    }

    public async void IniciarLobby()
    {
        if (_lobbyRunner != null) return;

        _lobbyRunner = new GameObject("LobbyRunner").AddComponent<NetworkRunner>();
        DontDestroyOnLoad(_lobbyRunner.gameObject);
        _lobbyRunner.ProvideInput = false;
        _lobbyRunner.AddCallbacks(this);

        var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
            Debug.LogError($"Error al unirse al lobby: {result.ShutdownReason}");
        else
            Debug.Log("Conectado al lobby correctamente");
    }

    public async void CrearSesion(string nombreSala, int maxJugadores)
    {
        Debug.Log($"Iniciando creación de sala: {nombreSala}");
        Debug.Log("LobbyRunner vivo: " + (_lobbyRunner != null ? "SI" : "NO"));

        if (_gameRunner != null)
        {
            await _gameRunner.Shutdown();
            Destroy(_gameRunner.gameObject);
            _gameRunner = null;
        }

        _gameRunner = new GameObject("GameRunner").AddComponent<NetworkRunner>();
        DontDestroyOnLoad(_gameRunner.gameObject);
        _gameRunner.ProvideInput = true;
        _gameRunner.AddCallbacks(this);

        var startArgs = new StartGameArgs()
        {
            GameMode    = GameMode.AutoHostOrClient,
            SessionName = nombreSala,
            PlayerCount = maxJugadores,
            Scene = SceneRef.FromIndex(4)
        };

        var result = await _gameRunner.StartGame(startArgs);

        if (result.Ok)
        {
            Debug.Log($"Sala '{nombreSala}' creada exitosamente");
            Debug.Log("LobbyRunner después de crear: " + (_lobbyRunner != null ? "SI" : "NO"));
        }
        else
        {
            Debug.LogError($"Error al crear sala: {result.ShutdownReason}");
        }
    }

    public async void UnirseASesion(string nombreSala)
    {
        Debug.Log($"Uniéndose a sala: {nombreSala}");

        if (_gameRunner != null)
        {
            await _gameRunner.Shutdown();
            Destroy(_gameRunner.gameObject);
            _gameRunner = null;
        }

        _gameRunner = new GameObject("GameRunner").AddComponent<NetworkRunner>();
        DontDestroyOnLoad(_gameRunner.gameObject);
        _gameRunner.ProvideInput = true;
        _gameRunner.AddCallbacks(this);

        var startArgs = new StartGameArgs()
        {
            GameMode    = GameMode.AutoHostOrClient,
            SessionName = nombreSala,
            Scene       = SceneRef.FromIndex(4)
        };

        var result = await _gameRunner.StartGame(startArgs);

        if (result.Ok)
            Debug.Log($"Unido a sala '{nombreSala}' exitosamente");
        else
            Debug.LogError($"Error al unirse: {result.ShutdownReason}");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Sesiones actualizadas: {sessionList.Count} sala(s)");
        SesionesDisponibles = sessionList;
        OnSesionesActualizadas?.Invoke();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}
    public void OnInput(NetworkRunner runner, NetworkInput input) {}
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        => Debug.Log($"Runner apagado: {shutdownReason}");
    public void OnConnectedToServer(NetworkRunner runner)
        => Debug.Log("Conectado al servidor");
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        => Debug.LogWarning($"Desconectado: {reason}");
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        => Debug.LogError($"Conexión fallida: {reason}");
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {}
    public void OnSceneLoadDone(NetworkRunner runner) {}
    public void OnSceneLoadStart(NetworkRunner runner) {}
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
}