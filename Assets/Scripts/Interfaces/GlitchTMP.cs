using UnityEngine;
using TMPro;
using System.Collections;

public class GlitchTMP : MonoBehaviour
{
    [Header("Componente")]
    public TMP_Text texto;

    [Header("Intensidad del glitch")]
    [Range(0f, 30f)] public float desplazamientoMax = 8f;
    [Range(0f, 1f)]  public float probabilidadGlitch = 0.04f;

    [Header("Aberración cromática")]
    public bool usarAberracion = true;
    [Range(0f, 20f)] public float separacionColor = 5f;

    private TMP_Text copiaRoja;
    private TMP_Text copiaAzul;
    private RectTransform rt;
    private Vector3 posicionOriginal;


    void Start()
    {
        if (texto == null)
        {
            texto = GetComponent<TMP_Text>();
            if (texto == null) return;
        }

        // Evita inicializar si este objeto ES una copia
        if (gameObject.name.Contains("_CopiaRoja") || gameObject.name.Contains("_CopiaAzul"))
            return;

        rt = texto.GetComponent<RectTransform>();
        posicionOriginal = rt.anchoredPosition3D;

        if (usarAberracion)
            CrearCapasColor();

        StartCoroutine(LoopGlitch());
    }

    void CrearCapasColor()
    {
        // Crea copia roja
        GameObject goRojo = Instantiate(texto.gameObject, texto.transform.parent);
        goRojo.name = texto.gameObject.name + "_CopiaRoja";
        // Elimina este script de la copia para que no se duplique
        Destroy(goRojo.GetComponent<GlitchTMP>());
        copiaRoja = goRojo.GetComponent<TMP_Text>();
        copiaRoja.color = new Color(1f, 0.2f, 0.2f, 0.55f);
        copiaRoja.text = texto.text;
        goRojo.transform.SetSiblingIndex(texto.transform.GetSiblingIndex());

        // Crea copia azul
        GameObject goAzul = Instantiate(texto.gameObject, texto.transform.parent);
        goAzul.name = texto.gameObject.name + "_CopiaAzul";
        Destroy(goAzul.GetComponent<GlitchTMP>());
        copiaAzul = goAzul.GetComponent<TMP_Text>();
        copiaAzul.color = new Color(0.2f, 0.4f, 1f, 0.55f);
        copiaAzul.text = texto.text;
        goAzul.transform.SetSiblingIndex(texto.transform.GetSiblingIndex());
    }

    IEnumerator LoopGlitch()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.05f, 0.3f));

            if (Random.value < probabilidadGlitch)
                yield return StartCoroutine(HacerGlitch());
            else
                MoverCapasColor(separacionColor * 0.3f);
        }
    }

    IEnumerator HacerGlitch()
    {
        int repeticiones = Random.Range(2, 6);
        for (int i = 0; i < repeticiones; i++)
        {
            float offsetX = Random.Range(-desplazamientoMax, desplazamientoMax);
            rt.anchoredPosition3D = posicionOriginal + new Vector3(offsetX, 0, 0);
            MoverCapasColor(separacionColor + Mathf.Abs(offsetX) * 0.5f);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
        }

        rt.anchoredPosition3D = posicionOriginal;
        MoverCapasColor(separacionColor * 0.3f);
    }

    void MoverCapasColor(float separacion)
    {
        if (copiaRoja == null || copiaAzul == null) return;
        copiaRoja.text = texto.text;
        copiaAzul.text = texto.text;

        copiaRoja.GetComponent<RectTransform>().anchoredPosition3D = posicionOriginal + new Vector3(-separacion, 0, 0);
        copiaAzul.GetComponent<RectTransform>().anchoredPosition3D = posicionOriginal + new Vector3( separacion, 0, 0);
    }

    void OnDestroy()
    {
        if (copiaRoja != null) Destroy(copiaRoja.gameObject);
        if (copiaAzul != null) Destroy(copiaAzul.gameObject);
    }
}