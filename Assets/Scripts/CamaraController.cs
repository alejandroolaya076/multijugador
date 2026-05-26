using UnityEngine;

public class CamaraController : MonoBehaviour
{
    public Transform objetivo;
    public float velocidadCamara = 0.025f;
    public Vector3 desplazamiento;

    void Update()
    {
        // Buscar el player spawneado por Fusion si aún no lo tenemos
        if (objetivo == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) objetivo = pc.transform;
        }
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posicionDeseada = new Vector3(
            objetivo.position.x + desplazamiento.x,
            objetivo.position.y + desplazamiento.y,
            desplazamiento.z
        );

        Vector3 posicionSuavizada = Vector3.Lerp(
            transform.position, posicionDeseada, velocidadCamara);
        transform.position = posicionSuavizada;
    }
}