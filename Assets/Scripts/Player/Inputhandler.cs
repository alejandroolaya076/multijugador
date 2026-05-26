using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────────────────────────────────────
// InputHandler: captura los inputs de Unity Input System
// y los entrega a Fusion cada tick de red.
//
// Agregar este componente al mismo GameObject que GameNetworkManager,
// o al jugador local. Fusion lo llama automáticamente via OnInput().
// ─────────────────────────────────────────────────────────────────────────────
public class InputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    // Estado de botones capturado desde los callbacks del Input System
    // (se acumula entre ticks y se limpia después de entregarlo a Fusion)
    private float _moveX;
    private bool  _jumpPending;
    private bool  _dashPending;
    private bool  _attackPending;
    private bool  _crouchHeld;   // held = se mantiene mientras esté presionado

    // ─────────────────────────────────────────────────────────────────
    // Callbacks del Input System de Unity
    // (los mismos métodos que tenías en PlayerController)
    // ─────────────────────────────────────────────────────────────────
        void Start()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
            runner.AddCallbacks(this);
    }

    void OnDestroy()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
            runner.RemoveCallbacks(this);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveX = context.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) _jumpPending = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) _dashPending = true;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed) _attackPending = true;
    }

    public void CrouchStarted(InputAction.CallbackContext context)
    {
        if (context.started) _crouchHeld = true;
    }

    public void CrouchCanceled(InputAction.CallbackContext context)
    {
        if (context.canceled) _crouchHeld = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // OnInput: Fusion llama esto ~60 veces por segundo.
    // Aquí empaquetamos todo el estado en EclipseraInput
    // y lo enviamos al servidor.
    // ─────────────────────────────────────────────────────────────────
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data   = new EclipseraInput();
        data.MoveX = _moveX;

        var buttons = new NetworkButtons();
        buttons.Set(EBtn.JUMP,   _jumpPending);
        buttons.Set(EBtn.DASH,   _dashPending);
        buttons.Set(EBtn.ATTACK, _attackPending);
        buttons.Set(EBtn.CROUCH, _crouchHeld);

        data.Buttons = buttons;
        input.Set(data);

        _jumpPending   = false;
        _dashPending   = false;
        _attackPending = false;
    }

    // ─── Callbacks vacíos requeridos por INetworkRunnerCallbacks ─────
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
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }}