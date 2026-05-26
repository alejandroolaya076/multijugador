using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameNetworkManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    public event Action<PlayerRef> OnPlayerConnected;
    public event Action<PlayerRef> OnPlayerDisconnected;
    public event Action OnMatchReady;

    public NetworkRunner Runner => _runner;
    public bool IsConnected => _runner != null && _runner.IsRunning;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // ─── Métodos públicos ────────────────────────────────────────────

    public async Task HostGame(string roomName, int maxPlayers = 2)
    {
        await StartFusion(GameMode.Host, roomName, maxPlayers);
    }

    public async Task JoinGame(string roomName)
    {
        await StartFusion(GameMode.Client, roomName, 2);
    }

    public async Task QuickJoin(int maxPlayers = 2)
    {
        await StartFusion(GameMode.AutoHostOrClient, "EclipseraRoom", maxPlayers);
    }

    public async Task Disconnect()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
            _runner = null;
        }
    }

    // ─── Lógica interna ──────────────────────────────────────────────

    private async Task StartFusion(GameMode mode, string roomName, int maxPlayers)
    {
        if (_runner != null)
            await _runner.Shutdown();

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(
            SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode     = mode,
            SessionName  = roomName,
            PlayerCount  = maxPlayers,
            Scene        = sceneInfo,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
            Debug.LogError($"Error al conectar: {result.ShutdownReason}");
        else
            Debug.Log($"Conectado como {mode} en sala '{roomName}'");
    }

    // ─── Callbacks de Fusion ─────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        int index = _spawnedPlayers.Count % Mathf.Max(_spawnPoints.Length, 1);
        Vector3 spawnPos = _spawnPoints.Length > 0
            ? _spawnPoints[index].position
            : new Vector3(index == 0 ? -3f : 3f, 0f, 0f);

        NetworkObject playerObj = runner.Spawn(
            _playerPrefab,
            spawnPos,
            Quaternion.identity,
            player
        );

        // Asignar índice al jugador (0 = P1, 1 = P2)
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null) pc.PlayerIndex = _spawnedPlayers.Count;

        _spawnedPlayers[player] = playerObj;
        OnPlayerConnected?.Invoke(player);

        if (_spawnedPlayers.Count >= runner.SessionInfo.MaxPlayers)
            OnMatchReady?.Invoke();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
        OnPlayerDisconnected?.Invoke(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"Sesión cerrada: {reason}");
        _spawnedPlayers.Clear();
    }

    // ─── Callbacks vacíos ────────────────────────────────────────────
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
    public void OnInput(NetworkRunner r, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float progress) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessions) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
}