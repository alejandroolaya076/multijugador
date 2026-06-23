using Fusion;
using UnityEngine;
using System.Collections;
using System.Linq;

// ─────────────────────────────────────────────────────────────────────────────
// RoundManager: controla el flujo de rondas al estilo Street Fighter
// - Al mejor de 3 rondas
// - Timer de 99 segundos por ronda
// - Resetea posiciones y vida entre rondas
// - Notifica a la UI via eventos estáticos
// ─────────────────────────────────────────────────────────────────────────────
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private int   _rondasParaGanar = 2;   // primero en ganar 2 gana el match
    [SerializeField] private float _tiempoRonda     = 99f;
    [SerializeField] private float _delayEntreRondas = 3f;
    [SerializeField] private float _delayInicioRonda = 2f; // tiempo antes de que empiece

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnP1;
    [SerializeField] private Transform _spawnP2;

    // ── Estado sincronizado ───────────────────────────────────────────
    [Networked] public int   RondaActual   { get; private set; }
    [Networked] public int   VictoriasP1   { get; private set; }
    [Networked] public int   VictoriasP2   { get; private set; }
    [Networked] public float Timer         { get; private set; }
    [Networked] public bool  RondaActiva   { get; private set; }
    [Networked] public bool  MatchTerminado{ get; private set; }

    // ── Eventos para la UI (se disparan en todos los clientes via RPC) ─
    public delegate void MensajeRonda(string mensaje, float duracion);
    public static event MensajeRonda OnMensaje;
    public static event System.Action OnRondaInicia;
    public static event System.Action OnMatchTerminado;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Spawned()
{
    if (!Object.HasStateAuthority) return;
    StartCoroutine(EsperarJugadores());
}

private IEnumerator EsperarJugadores()
{
    RPC_MostrarMensaje("Esperando jugadores...", 99f);
    
    // Espera hasta que haya exactamente 2 jugadores conectados
    while (Runner.ActivePlayers.Count() < 2)
        yield return new WaitForSeconds(0.5f);

    RPC_MostrarMensaje("", 0f); // limpia el mensaje
    IniciarRonda();
}

    // ── Tick de red ───────────────────────────────────────────────────
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!RondaActiva || MatchTerminado) return;

        Timer -= Runner.DeltaTime;

        if (Timer <= 0f)
        {
            Timer = 0f;
            TiempoAgotado();
        }
    }

    // ── Flujo principal ───────────────────────────────────────────────

    private void IniciarRonda()
    {
        RondaActual++;
        Timer      = _tiempoRonda;
        RondaActiva = false; // se activa después del delay

        RPC_MostrarMensaje($"RONDA {RondaActual}", _delayInicioRonda);
        StartCoroutine(EsperarInicioRonda());
    }

    private IEnumerator EsperarInicioRonda()
    {
        // Resetear jugadores al inicio de cada ronda
        ResetearJugadores();

        yield return new WaitForSeconds(_delayInicioRonda);

        RondaActiva = true;
        RPC_InicioRonda();
    }

    // Llamado desde PlayerController cuando un jugador muere
    public void OnPlayerDied(PlayerRef jugadorMuerto)
    {
        if (!Object.HasStateAuthority) return;
        if (!RondaActiva) return;

        RondaActiva = false;

        // Determinar ganador de la ronda
        bool p1Murio = EsP1(jugadorMuerto);

        if (p1Murio) VictoriasP2++;
        else         VictoriasP1++;

        string ganadorRonda = p1Murio ? "JUGADOR 2" : "JUGADOR 1";
        RPC_MostrarMensaje($"{ganadorRonda}\nGANA LA RONDA", _delayEntreRondas);

        // Verificar si alguien ganó el match
        if (VictoriasP1 >= _rondasParaGanar || VictoriasP2 >= _rondasParaGanar)
        {
            MatchTerminado = true;
            string ganadorMatch = VictoriasP1 >= _rondasParaGanar ? "JUGADOR 1" : "JUGADOR 2";
            RPC_MostrarMensaje($"{ganadorMatch}\nGANA EL MATCH", _delayEntreRondas + 2f);
            StartCoroutine(TerminarMatch());
        }
        else
        {
            StartCoroutine(SiguienteRonda());
        }
    }

    private void TiempoAgotado()
    {
        if (!RondaActiva) return;
        RondaActiva = false;

        // Gana quien tenga más vida
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController p1 = null, p2 = null;

        foreach (var j in jugadores)
        {
            if (j.PlayerIndex == 0) p1 = j;
            if (j.PlayerIndex == 1) p2 = j;
        }

        RPC_MostrarMensaje("¡TIEMPO!", _delayEntreRondas);

        if (p1 != null && p2 != null)
        {
            if (p1.vida > p2.vida)
            {
                VictoriasP1++;
                RPC_MostrarMensaje("JUGADOR 1\nGANA LA RONDA", _delayEntreRondas);
            }
            else if (p2.vida > p1.vida)
            {
                VictoriasP2++;
                RPC_MostrarMensaje("JUGADOR 2\nGANA LA RONDA", _delayEntreRondas);
            }
            else
            {
                // Empate: ninguno gana la ronda
                RPC_MostrarMensaje("EMPATE", _delayEntreRondas);
            }
        }

        if (VictoriasP1 >= _rondasParaGanar || VictoriasP2 >= _rondasParaGanar)
        {
            MatchTerminado = true;
            StartCoroutine(TerminarMatch());
        }
        else
        {
            StartCoroutine(SiguienteRonda());
        }
    }

    private IEnumerator SiguienteRonda()
    {
        yield return new WaitForSeconds(_delayEntreRondas);
        IniciarRonda();
    }

    private IEnumerator TerminarMatch()
    {
        yield return new WaitForSeconds(_delayEntreRondas + 2f);
        RPC_MatchTerminado();
    }

    // ── Reset de jugadores ────────────────────────────────────────────

    private void ResetearJugadores()
    {
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var j in jugadores)
        {
            if (!j.Object.HasStateAuthority) continue;

            j.vida    = j.vidaMaxima;
            j.muerto  = false;

            // Resetear posición
            if (j.PlayerIndex == 0 && _spawnP1 != null)
                j.transform.position = _spawnP1.position;
            else if (j.PlayerIndex == 1 && _spawnP2 != null)
                j.transform.position = _spawnP2.position;

            // Notificar reset visual a todos
            j.RPC_ResetearEstado();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private bool EsP1(PlayerRef player)
    {
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var j in jugadores)
        {
            if (j.Object.InputAuthority == player)
                return j.PlayerIndex == 0;
        }
        return false;
    }

    // ── RPCs: sincronizan mensajes a todos los clientes ───────────────

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MostrarMensaje(string mensaje, float duracion)
    {
        OnMensaje?.Invoke(mensaje, duracion);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InicioRonda()
    {
        OnRondaInicia?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MatchTerminado()
    {
        OnMatchTerminado?.Invoke();
    }
}