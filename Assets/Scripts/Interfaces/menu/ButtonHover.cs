using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Imágenes del botón")]
    public Sprite spriteNormal;
    public Sprite spriteHover;

    [Header("Texto del botón")]
    public TMPro.TMP_Text texto;

    [Header("Configuración")]
    public float velocidadTransicion = 8f;
    public Color colorTextoNormal = Color.white;
    public Color colorTextoHover  = new Color(0.4f, 1f, 1f); // cian claro

    public float escalaHover = 1.05f; // cuánto crece al hacer hover

    private Image imagen;
    private RectTransform rt;
    private bool estaHover = false;
    private float escalaObjetivo = 1f;
    private float escalaActual   = 1f;

    void Start()
    {
        imagen = GetComponent<Image>();
        rt     = GetComponent<RectTransform>();

        imagen.sprite = spriteNormal;
        if (texto != null)
            texto.color = colorTextoNormal;
    }

    void Update()
    {
        // Transición suave de escala
        escalaActual = Mathf.Lerp(escalaActual, escalaObjetivo, Time.deltaTime * velocidadTransicion);
        rt.localScale = Vector3.one * escalaActual;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        estaHover = true;
        imagen.sprite = spriteHover;
        escalaObjetivo = escalaHover;

        if (texto != null)
            StartCoroutine(LerpColorTexto(colorTextoHover));
    }

    public void OnPointerExit(PointerEventData e)
    {
        estaHover = false;
        imagen.sprite = spriteNormal;
        escalaObjetivo = 1f;

        if (texto != null)
            StartCoroutine(LerpColorTexto(colorTextoNormal));
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(AnimacionClick());
    }

    IEnumerator AnimacionClick()
    {
        // Pequeño "squish" al hacer clic
        escalaObjetivo = 0.95f;
        yield return new WaitForSeconds(0.08f);
        if (gameObject.activeInHierarchy)
            escalaObjetivo = estaHover ? escalaHover : 1f;
    }

    IEnumerator LerpColorTexto(Color objetivo)
    {
        Color inicio = texto.color;
        float t = 0f;
        while (t < 1f && gameObject.activeInHierarchy)
        {
            t += Time.deltaTime * velocidadTransicion;
            texto.color = Color.Lerp(inicio, objetivo, t);
            yield return null;
        }
        if (gameObject.activeInHierarchy)
            texto.color = objetivo;
    }
}