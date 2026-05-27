using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// HUDMultijugador: controla toda la UI de combate
// - Barras de vida P1 y P2
// - Timer de ronda
// - Iconos de rondas ganadas (como Street Fighter)
// - Mensajes de ronda ("RONDA 1", "KO", "JUGADOR 1 GANA")
// ─────────────────────────────────────────────────────────────────────────────
public class HUDMultijugador : MonoBehaviour
{
    [Header("Barras de Vida")]
    [SerializeField] private Image _rellenoP1;  // tu Image con fillAmount
    [SerializeField] private Image _rellenoP2;

    [Header("Colores de vida")]
    [SerializeField] private Color _colorAlto  = Color.green;
    [SerializeField] private Color _colorMedio = Color.yellow;
    [SerializeField] private Color _colorBajo  = Color.red;

    [Header("Timer")]
    [SerializeField] private TMP_Text _timerText;

    [Header("Iconos de rondas ganadas")]
    [SerializeField] private Image[] _iconosRondaP1; // 2 iconos (para ganar al mejor de 3)
    [SerializeField] private Image[] _iconosRondaP2;
    [SerializeField] private Color   _colorGanado = Color.yellow;
    [SerializeField] private Color   _colorVacio  = Color.gray;

    [Header("Mensaje de ronda")]
    [SerializeField] private GameObject _panelMensaje;
    [SerializeField] private TMP_Text   _textoMensaje;

    [Header("Nombres")]
    [SerializeField] private TMP_Text _nombreP1;
    [SerializeField] private TMP_Text _nombreP2;

    private PlayerController _p1;
    private PlayerController _p2;

    void OnEnable()
    {
        RoundManager.OnMensaje       += MostrarMensaje;
        RoundManager.OnRondaInicia   += OcultarMensaje;
        RoundManager.OnMatchTerminado += OnMatchTerminado;
    }

    void OnDisable()
    {
        RoundManager.OnMensaje       -= MostrarMensaje;
        RoundManager.OnRondaInicia   -= OcultarMensaje;
        RoundManager.OnMatchTerminado -= OnMatchTerminado;
    }

    void Start()
    {
        if (_panelMensaje != null) _panelMensaje.SetActive(false);
        if (_nombreP1 != null) _nombreP1.text = "JUGADOR 1";
        if (_nombreP2 != null) _nombreP2.text = "JUGADOR 2";
    }

    void Update()
    {
        BuscarJugadores();
        ActualizarBarrasVida();
        ActualizarTimer();
        ActualizarIconosRonda();
    }

    // ── Buscar jugadores spawneados por Fusion ────────────────────────

    private void BuscarJugadores()
    {
        if (_p1 != null && _p2 != null) return;

        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var j in jugadores)
        {
            if (j.Object == null || !j.Object.IsValid) continue;
            if (j.PlayerIndex == 0) _p1 = j;
            if (j.PlayerIndex == 1) _p2 = j;
        }
    }

    // ── Barras de vida ────────────────────────────────────────────────

    private void ActualizarBarrasVida()
    {
        ActualizarBarra(_p1, _rellenoP1);
        ActualizarBarra(_p2, _rellenoP2);
    }

    private void ActualizarBarra(PlayerController jugador, Image relleno)
    {
        if (jugador == null || relleno == null) return;

        float pct = jugador.vidaMaxima > 0
            ? (float)jugador.vida / jugador.vidaMaxima
            : 0f;

        relleno.fillAmount = Mathf.Clamp01(pct);

        if      (pct > 0.5f)  relleno.color = _colorAlto;
        else if (pct > 0.25f) relleno.color = _colorMedio;
        else                  relleno.color = _colorBajo;
    }

    // ── Timer ─────────────────────────────────────────────────────────

    private void ActualizarTimer()
    {
        if (_timerText == null || RoundManager.Instance == null) return;
        if (RoundManager.Instance.Object == null || !RoundManager.Instance.Object.IsValid) return; // ← AGREGA ESTO

        int segundos = Mathf.CeilToInt(RoundManager.Instance.Timer);
        _timerText.text = segundos.ToString();
        _timerText.color = segundos <= 10 ? Color.red : Color.white;
    }

    // ── Iconos de rondas ganadas ──────────────────────────────────────

    private void ActualizarIconosRonda()
    {
        if (RoundManager.Instance == null) return;
        if (RoundManager.Instance.Object == null || !RoundManager.Instance.Object.IsValid) return; // ← AGREGA ESTO

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

    // ── Mensajes ──────────────────────────────────────────────────────

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
        // Aquí puedes cargar una pantalla de resultados
        Debug.Log("Match terminado");
    }
}