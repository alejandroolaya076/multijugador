using UnityEngine;
using UnityEngine.Rendering;

public class CamaraController : MonoBehaviour
{

    public Transform objetivo;

    public float velocidadCamara  = 0.025f;
    public Vector3 desplazamiento;

private void LateUpdate()
{
    Vector3 posicionDeseada = new Vector3(
        objetivo.position.x + desplazamiento.x,
        objetivo.position.y + desplazamiento.y,
        desplazamiento.z   // ← usa SOLO el desplazamiento Z, ignora el Z del jugador
    );

    Vector3 posicionSuavizada = Vector3.Lerp(transform.position, posicionDeseada, velocidadCamara);
    transform.position = posicionSuavizada;
}
}
