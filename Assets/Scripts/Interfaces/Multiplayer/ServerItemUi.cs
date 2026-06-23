using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ServerItemUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Colores")]
    public Color colorNormal       = new Color(0.08f, 0.08f, 0.15f);
    public Color colorHover        = new Color(0.05f, 0.15f, 0.20f);
    public Color colorSeleccionado = new Color(0.0f, 0.25f, 0.30f);

    public Color colorBordeNormal       = new Color(0.2f, 0.2f, 0.3f);
    public Color colorBordeSeleccionado = new Color(0.0f, 0.8f, 0.8f);

    [Header("Texto jugadores llenos")]
    public Color colorJugadoresNormal = Color.white;
    public Color colorJugadoresLleno  = new Color(1f, 0.3f, 0.3f);

    [Header("Referencias texto")]
    public TMP_Text txtNombre;
    public TMP_Text txtJugadores;

    private Image fondo;
    private Outline borde;
    private bool seleccionado = false;
    private System.Action _onSeleccionado;

    private static ServerItemUI itemActual;

    void Awake()
    {
        fondo = GetComponent<Image>();
        borde = GetComponent<Outline>();
        if (borde == null) borde = gameObject.AddComponent<Outline>();

        borde.effectColor    = colorBordeNormal;
        borde.effectDistance = new Vector2(2, -2);

        PonerEstadoNormal();
    }

    public void Setup(string nombre, int jugadores, int maxJugadores, System.Action onSeleccionado)
    {
        _onSeleccionado = onSeleccionado;

        if (txtNombre != null) txtNombre.text = nombre;
        if (txtJugadores != null) txtJugadores.text = $"{jugadores}/{maxJugadores}";

        ActualizarColorJugadores();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!seleccionado) fondo.color = colorHover;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!seleccionado) PonerEstadoNormal();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (itemActual != null && itemActual != this)
            itemActual.Deseleccionar();

        seleccionado = true;
        itemActual   = this;

        fondo.color          = colorSeleccionado;
        borde.effectColor    = colorBordeSeleccionado;
        borde.effectDistance = new Vector2(3, -3);

        _onSeleccionado?.Invoke();
    }

    public void Deseleccionar()
    {
        seleccionado = false;
        PonerEstadoNormal();
    }

    void PonerEstadoNormal()
    {
        fondo.color           = colorNormal;
        borde.effectColor     = colorBordeNormal;
        borde.effectDistance  = new Vector2(2, -2);
    }

    void ActualizarColorJugadores()
    {
        if (txtJugadores == null) return;

        string[] partes = txtJugadores.text.Split('/');
        if (partes.Length == 2 &&
            int.TryParse(partes[0], out int actual) &&
            int.TryParse(partes[1], out int maximo))
        {
            txtJugadores.color = (actual >= maximo) ? colorJugadoresLleno : colorJugadoresNormal;
        }
    }
}