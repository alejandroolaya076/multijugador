using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDMultijugador : MonoBehaviour
{
    [Header("Barras de Vida")]
    [SerializeField] private Image _rellenoP1;
    [SerializeField] private Image _rellenoP2;

    [Header("Colores de vida")]
    [SerializeField] private Color _colorAlto  = Color.green;
    [SerializeField] private Color _colorMedio = Color.yellow;
    [SerializeField] private Color _colorBajo  = Color.red;

    [Header("Timer")]
    [SerializeField] private TMP_Text _timerText;

    [Header("Iconos de rondas ganadas")]
    [SerializeField] private Image[] _iconosRondaP1;
    [SerializeField] private Image[] _iconosRondaP2;
    [SerializeField] private Color   _colorGanado = Color.yellow;
    [SerializeField] private Color   _colorVacio  = Color.gray;

    [Header("Mensaje de ronda")]
    [SerializeField] private GameObject _panelMensaje;
    [SerializeField] private TMP_Text   _textoMensaje;

    [Header("Nombres (encima de la barra de vida)")]
    [SerializeField] private TMP_Text _nombreP1;
    [SerializeField] private TMP_Text _nombreP2;

    private PlayerController _p1;
    private PlayerController _p2;
    private bool _jugadoresEncontrados = false;

    // Guardamos los nombres una vez que lleguen para no perderlos
    private string _nombreGuardadoP1 = "";
    private string _nombreGuardadoP2 = "";

    void OnEnable()
    {
        RoundManager.OnMensaje        += MostrarMensaje;
        RoundManager.OnRondaInicia    += OcultarMensaje;
        RoundManager.OnMatchTerminado += OnMatchTerminado;
    }

    void OnDisable()
    {
        RoundManager.OnMensaje        -= MostrarMensaje;
        RoundManager.OnRondaInicia    -= OcultarMensaje;
        RoundManager.OnMatchTerminado -= OnMatchTerminado;
    }

    void Start()
    {
        if (_panelMensaje != null) _panelMensaje.SetActive(false);
        if (_nombreP1 != null) _nombreP1.text = "";
        if (_nombreP2 != null) _nombreP2.text = "";
    }

    void Update()
    {
        if (!_jugadoresEncontrados)
            BuscarJugadores();

        ActualizarBarrasVida();
        ActualizarTimer();
        ActualizarIconosRonda();
    }

    // ── Buscar jugadores spawneados por Fusion ────────────────────────────────
    private void BuscarJugadores()
    {
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var j in jugadores)
        {
            if (j == null) continue;
            if (j.Object == null || !j.Object.IsValid) continue;

            if (j.PlayerIndex == 0 && _p1 == null) _p1 = j;
            if (j.PlayerIndex == 1 && _p2 == null) _p2 = j;
        }

        if (_p1 != null && _p2 != null)
            _jugadoresEncontrados = true;
    }

    // ── Barras de vida + nombres ──────────────────────────────────────────────
    private void ActualizarBarrasVida()
    {
        ActualizarBarra(_p1, _rellenoP1, _nombreP1, 0);
        ActualizarBarra(_p2, _rellenoP2, _nombreP2, 1);
    }

    private void ActualizarBarra(PlayerController jugador, Image relleno, TMP_Text nombreText, int index)
    {
        if (jugador == null || relleno == null) return;
        if (jugador.Object == null || !jugador.Object.IsValid) return;

        // Actualizar barra de vida
        float pct = jugador.vidaMaxima > 0
            ? (float)jugador.vida / jugador.vidaMaxima
            : 0f;

        relleno.fillAmount = Mathf.Clamp01(pct);

        if      (pct > 0.5f)  relleno.color = _colorAlto;
        else if (pct > 0.25f) relleno.color = _colorMedio;
        else                  relleno.color = _colorBajo;

        // Actualizar nombre — guardar cuando llegue y no perderlo
        if (nombreText != null)
        {
            string nombre = jugador.NombreJugador.ToString();

            if (index == 0 && !string.IsNullOrEmpty(nombre))
                _nombreGuardadoP1 = nombre;
            else if (index == 1 && !string.IsNullOrEmpty(nombre))
                _nombreGuardadoP2 = nombre;

            string nombreMostrar = index == 0 ? _nombreGuardadoP1 : _nombreGuardadoP2;
            if (!string.IsNullOrEmpty(nombreMostrar))
                nombreText.text = nombreMostrar;
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────
    private void ActualizarTimer()
    {
        if (_timerText == null || RoundManager.Instance == null) return;
        if (RoundManager.Instance.Object == null || !RoundManager.Instance.Object.IsValid) return;

        int segundos = Mathf.CeilToInt(RoundManager.Instance.Timer);
        _timerText.text  = segundos.ToString();
        _timerText.color = segundos <= 10 ? Color.red : Color.white;
    }

    // ── Iconos de rondas ganadas ──────────────────────────────────────────────
    private void ActualizarIconosRonda()
    {
        if (RoundManager.Instance == null) return;
        if (RoundManager.Instance.Object == null || !RoundManager.Instance.Object.IsValid) return;

        ActualizarIconos(_iconosRondaP1, RoundManager.Instance.VictoriasP1);
        ActualizarIconos(_iconosRondaP2, RoundManager.Instance.VictoriasP2);
    }

    private void ActualizarIconos(Image[] iconos, int victorias)
    {
        if (iconos == null) return;
        for (int i = 0; i < iconos.Length; i++)
        {
            if (iconos[i] == null) continue;
            iconos[i].color = i < victorias ? _colorGanado : _colorVacio;
        }
    }

    // ── Mensajes ──────────────────────────────────────────────────────────────
    private void MostrarMensaje(string mensaje, float duracion)
    {
        if (_panelMensaje == null) return;
        _textoMensaje.text = mensaje;
        _panelMensaje.SetActive(true);
        StartCoroutine(OcultarMensajeDespues(duracion));
    }

    private void OcultarMensaje()
    {
        if (_panelMensaje != null)
            _panelMensaje.SetActive(false);
    }

    private IEnumerator OcultarMensajeDespues(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (_panelMensaje != null)
            _panelMensaje.SetActive(false);
    }

    private void OnMatchTerminado()
    {
        Debug.Log("Match terminado");
    }

    public void ResetearJugadores()
    {
        _p1 = null;
        _p2 = null;
        _jugadoresEncontrados = false;
        _nombreGuardadoP1 = "";
        _nombreGuardadoP2 = "";
    }
}