using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ServerItemUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Colores")]
    public Color colorNormal     = new Color(0.08f, 0.08f, 0.15f); // fondo oscuro
    public Color colorHover      = new Color(0.05f, 0.15f, 0.20f); // azul tenue
    public Color colorSeleccionado = new Color(0.0f, 0.25f, 0.30f); // cian oscuro

    public Color colorBordeNormal      = new Color(0.2f, 0.2f, 0.3f);
    public Color colorBordeSeleccionado = new Color(0.0f, 0.8f, 0.8f); // cian

    [Header("Texto jugadores llenos")]
    public Color colorJugadoresNormal = Color.white;
    public Color colorJugadoresLleno  = new Color(1f, 0.3f, 0.3f); // rojo

    private Image fondo;
    private Outline borde;
    private TMP_Text txtJugadores;
    private bool seleccionado = false;

    // Referencia estática para saber cuál está seleccionado
    private static ServerItemUI itemActual;

    void Start()
    {
        fondo  = GetComponent<Image>();
        borde  = GetComponent<Outline>();

        // Si no tiene Outline, lo agrega automáticamente
        if (borde == null)
            borde = gameObject.AddComponent<Outline>();

        txtJugadores = transform.Find("TxtJugadores").GetComponent<TMP_Text>();

        // Configura el borde inicial
        borde.effectColor     = colorBordeNormal;
        borde.effectDistance  = new Vector2(2, -2);

        ActualizarColorJugadores();
        PonerEstadoNormal();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!seleccionado)
            fondo.color = colorHover;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!seleccionado)
            PonerEstadoNormal();
    }

    public void OnPointerClick(PointerEventData e)
    {
        // Deselecciona el anterior
        if (itemActual != null && itemActual != this)
            itemActual.Deseleccionar();

        seleccionado = true;
        itemActual   = this;

        fondo.color       = colorSeleccionado;
        borde.effectColor = colorBordeSeleccionado;
        borde.effectDistance = new Vector2(3, -3);
    }

    public void Deseleccionar()
    {
        seleccionado = false;
        PonerEstadoNormal();
    }

    void PonerEstadoNormal()
    {
        fondo.color       = colorNormal;
        borde.effectColor = colorBordeNormal;
        borde.effectDistance = new Vector2(2, -2);
    }

    void ActualizarColorJugadores()
    {
        if (txtJugadores == null) return;

        // Lee el texto "6/20" y compara los números
        string[] partes = txtJugadores.text.Split('/');
        if (partes.Length == 2 &&
            int.TryParse(partes[0], out int actual) &&
            int.TryParse(partes[1], out int maximo))
        {
            txtJugadores.color = (actual >= maximo) ? colorJugadoresLleno : colorJugadoresNormal;
        }
    }
}