using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private bool _registered = false;

    void Update()
    {
        if (!_registered)
        {
            var runner = FindObjectOfType<NetworkRunner>();
            if (runner != null)
            {
                runner.AddCallbacks(this);
                _registered = true;
                Debug.Log("InputHandler registrado");
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new EclipseraInput();

    // Usar Keyboard del nuevo Input System
    var kb = UnityEngine.InputSystem.Keyboard.current;
    if (kb == null) return;

    data.MoveX = 0f;
    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  data.MoveX = -1f;
    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) data.MoveX =  1f;

    var buttons = new NetworkButtons();
    buttons.Set(EBtn.JUMP,   kb.spaceKey.isPressed || kb.wKey.isPressed);
    buttons.Set(EBtn.DASH,   kb.kKey.isPressed);
    buttons.Set(EBtn.ATTACK, kb.jKey.isPressed);
    buttons.Set(EBtn.CROUCH, kb.sKey.isPressed || kb.downArrowKey.isPressed);

    data.Buttons = buttons;
    input.Set(data);
}

    public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason reason) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
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