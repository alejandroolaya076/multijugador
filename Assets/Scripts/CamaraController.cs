using UnityEngine;
using Fusion;

public class CamaraController : MonoBehaviour
{
    public float velocidadCamara = 0.025f;
    public Vector3 desplazamiento;

    private Transform objetivo;

    void Update()
    {
        if (objetivo == null)
        {
            // Busca SOLO el jugador local (el que tiene input authority)
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (pc.Object != null && pc.Object.HasInputAuthority)
                {
                    objetivo = pc.transform;
                    break;
                }
            }
        }
    }

    private void LateUpdate()
{
    if (objetivo == null) return;

    // Usar rb.position en lugar de transform.position
    // para leer la posición física real sin el jitter de Unity
    Rigidbody2D rbObjetivo = objetivo.GetComponent<Rigidbody2D>();
    Vector2 posFisica = rbObjetivo != null ? rbObjetivo.position : (Vector2)objetivo.position;

    Vector3 posicionDeseada = new Vector3(
        posFisica.x + desplazamiento.x,
        posFisica.y + desplazamiento.y,
        desplazamiento.z
    );

    transform.position = Vector3.Lerp(
        transform.position, posicionDeseada, velocidadCamara);
}
}