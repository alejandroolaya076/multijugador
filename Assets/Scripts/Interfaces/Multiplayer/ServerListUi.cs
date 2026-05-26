using UnityEngine;
using TMPro;

public class ServerListUI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform contenidoLista;    // el objeto Content del ScrollView
    public GameObject prefabServidor;   // el Prefab ServerItem

    void Start()
    {
        // Por ahora cargamos datos de prueba
        // Cuando hagamos el multijugador funcional, esto vendrá de la red
        CargarServidoresPrueba();
    }

    void CargarServidoresPrueba()
    {
        // Datos falsos para ver cómo queda visualmente
        string[] nombres = {
            "Pixel Arena 1",
            "Dusty Dunes Co-op",
            "Enchanted Forest",
            "Team Deathmatch",
            "Capture the Flag",
            "Boss Rush Mode"
        };

        string[] jugadores = { "6/20", "12/16", "5/16", "8/8", "3/20", "1/4" };

        for (int i = 0; i < nombres.Length; i++)
        {
            AgregarServidor(nombres[i], jugadores[i]);
        }
    }

    public void AgregarServidor(string nombre, string jugadores)
    {
        GameObject item = Instantiate(prefabServidor, contenidoLista);

        // Busca los TMP dentro del prefab por nombre
        // (ajusta los nombres según como los llames tú)
        TMP_Text txtNombre    = item.transform.Find("TxtNombre").GetComponent<TMP_Text>();
        TMP_Text txtJugadores = item.transform.Find("TxtJugadores").GetComponent<TMP_Text>();

        txtNombre.text    = nombre;
        txtJugadores.text = jugadores;
    }

    public void LimpiarLista()
    {
        foreach (Transform hijo in contenidoLista)
            Destroy(hijo.gameObject);
    }

    // Llama esto desde el botón REFRESH LIST
    public void RefrescarLista()
    {
        LimpiarLista();
        CargarServidoresPrueba(); // más adelante aquí irá la llamada a red
    }
}