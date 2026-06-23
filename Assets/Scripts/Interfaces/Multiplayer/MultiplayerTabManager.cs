using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerTabManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelJoinServer;
    [SerializeField] private GameObject panelCreateServer;

    [Header("Botones de Tab")]
    [SerializeField] private Button btnJoin;
    [SerializeField] private Button btnCreate;

    [Header("Botones de Acción")]
    [SerializeField] private Button btnConnect;
    [SerializeField] private Button btnRefresh;

    [Header("Lista de Salas")]
    [SerializeField] private Transform contenedorSalas;
    [SerializeField] private GameObject prefabServerItem;

    [Header("Formulario Crear Sala")]
    [SerializeField] private TMP_InputField inputNombre;
    [SerializeField] private TMP_InputField inputMaxJugadores;

    [Header("Feedback")]
    [SerializeField] private TMP_Text txtEstado;

    private string _salaSeleccionada;

    void Start()
    {
        Debug.Log("btnJoin: " + (btnJoin == null ? "NULL" : "OK"));
        Debug.Log("btnCreate: " + (btnCreate == null ? "NULL" : "OK"));
        Debug.Log("panelJoinServer: " + (panelJoinServer == null ? "NULL" : "OK"));
        btnJoin.onClick.AddListener(() => MostrarPanel(true));
        btnCreate.onClick.AddListener(() => MostrarPanel(false));
        btnConnect.onClick.AddListener(ConectarSala);
        btnRefresh.onClick.AddListener(RefrescarLista);

        MostrarPanel(true);
        // Iniciar lobby desde Start, cuando todo ya está listo
    if (LobbyManager.Instance != null)
    {
        LobbyManager.Instance.OnSesionesActualizadas += ActualizarLista;
        LobbyManager.Instance.IniciarLobby();
    }
    else
    {
        Debug.LogError("LobbyManager.Instance es NULL en Start — revisa el orden de ejecución");
    }

    }

   void OnEnable()
{
    // Solo re-suscribir si ya fue inicializado antes (re-activación del panel)
    if (LobbyManager.Instance != null)
        LobbyManager.Instance.OnSesionesActualizadas += ActualizarLista;
}

void OnDisable()
{
    if (LobbyManager.Instance != null)
        LobbyManager.Instance.OnSesionesActualizadas -= ActualizarLista;
}

    void MostrarPanel(bool mostrarJoin)
    {
        panelJoinServer.SetActive(mostrarJoin);
        panelCreateServer.SetActive(!mostrarJoin);
    }

    void ActualizarLista()
    {
        foreach (Transform hijo in contenedorSalas)
            Destroy(hijo.gameObject);

        foreach (var sesion in LobbyManager.Instance.SesionesDisponibles)
        {
            var item = Instantiate(prefabServerItem, contenedorSalas);
            var ui   = item.GetComponent<ServerItemUI>();
            string nombre = sesion.Name;
            ui.Setup(nombre, sesion.PlayerCount, sesion.MaxPlayers, () =>
            {
                _salaSeleccionada = nombre;
            });
        }
    }

    void RefrescarLista()
    {
        SetEstado("Refrescando...");
        ActualizarLista();
    }

    void ConectarSala()
    {
        if (string.IsNullOrEmpty(_salaSeleccionada))
        {
            SetEstado("Selecciona una sala primero.");
            return;
        }
        SetEstado($"Conectando a {_salaSeleccionada}...");
        LobbyManager.Instance.UnirseASesion(_salaSeleccionada);
    }

    public void CrearServidor()
    {
        string nombre = inputNombre.text.Trim();
        string maxStr = inputMaxJugadores.text.Trim();

        if (string.IsNullOrEmpty(nombre))
        {
            SetEstado("Escribe un nombre para la sala.");
            return;
        }

        int max = 4;
        if (!string.IsNullOrEmpty(maxStr)) int.TryParse(maxStr, out max);

        SetEstado($"Creando sala '{nombre}'...");
        LobbyManager.Instance.CrearSesion(nombre, max);
    }

    void SetEstado(string msg)
    {
        if (txtEstado != null) txtEstado.text = msg;
        Debug.Log("[Lobby] " + msg);
    }
}