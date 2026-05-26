using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class ReinoAnimado : MonoBehaviour
{
    [Header("Flotación")]
    public float alturaPulso = 15f;        // qué tanto sube y baja (píxeles)
    public float velocidadFlotacion = 1.2f; // qué tan rápido flota

    [Header("Parallax con mouse")]
    public float fuerzaParallax = 20f;     // qué tanto se desplaza con el mouse
    public float suavizadoParallax = 5f;   // qué tan suave sigue al mouse

    [Header("Pulso de luz")]
    public float brilloMinimo = 0.75f;     // brillo mínimo (0 a 1)
    public float brilloMaximo = 1f;        // brillo máximo
    public float velocidadPulso = 0.8f;    // qué tan rápido pulsa

    private RectTransform rt;
    private Image imagen;
    private Vector3 posicionBase;
    private Vector2 parallaxActual;
    private Vector2 parallaxObjetivo;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        imagen = GetComponent<Image>();
        posicionBase = rt.anchoredPosition3D;
    }

    void Update()
    {
        // --- FLOTACIÓN SUAVE ---
        float offsetFlotacion = Mathf.Sin(Time.time * velocidadFlotacion) * alturaPulso;

        // --- PARALLAX CON MOUSE ---
        // Convierte posición del mouse a rango -1 a 1
        float mouseX = (Mouse.current.position.x.value / Screen.width  - 0.5f) * 2f;
        float mouseY = (Mouse.current.position.y.value / Screen.height - 0.5f) * 2f;

        parallaxObjetivo = new Vector2(mouseX * fuerzaParallax, mouseY * fuerzaParallax);
        parallaxActual = Vector2.Lerp(parallaxActual, parallaxObjetivo, 
                                      Time.deltaTime * suavizadoParallax);

        // Aplica flotación + parallax a la posición
        rt.anchoredPosition3D = posicionBase 
            + new Vector3(parallaxActual.x, offsetFlotacion + parallaxActual.y, 0);

        // --- PULSO DE LUZ ---
        float pulso = Mathf.Lerp(brilloMinimo, brilloMaximo,
                                 (Mathf.Sin(Time.time * velocidadPulso) + 1f) / 2f);
        imagen.color = new Color(pulso, pulso, pulso, 1f);
    }
}