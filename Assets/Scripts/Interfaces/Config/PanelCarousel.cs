using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PanelCarouselCircular : MonoBehaviour, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("Panels")]
    public List<RectTransform> panels;

    [Header("Configuración del Círculo")]
    public float radioX = 400f;          // radio horizontal del elipse
    public float radioY = 80f;           // radio vertical (da sensación de perspectiva)
    public float animationSpeed = 6f;

    [Header("Configuración Visual")]
    public float frontScale = 1.1f;
    public float backScale = 0.65f;
    public float frontAlpha = 1f;
    public float backAlpha = 0.45f;

    [Header("Interacción")]
    public float dragThreshold = 40f;
    public float scrollSensitivity = 1f;

    private int currentIndex = 0;
    private float dragStartX;

    // Ángulo actual de cada panel en el círculo (en grados)
    private float[] angles;
    private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

    // Cuántos grados ocupa cada slot
    private float slotAngle => 360f / panels.Count;

    void Start()
    {
        angles = new float[panels.Count];

        foreach (var panel in panels)
        {
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);
        }

        // Distribuir paneles uniformemente en el círculo
        for (int i = 0; i < panels.Count; i++)
            angles[i] = i * slotAngle;  // 0°, 90°, 180°, 270° para 4 paneles

        ApplyLayout(instant: true);
    }

    void Update()
    {
        ApplyLayout();
    }

    // ─── LAYOUT CIRCULAR ────────────────────────────────────────────────────────

    void ApplyLayout(bool instant = false)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            float rad = angles[i] * Mathf.Deg2Rad;

            // Posición en la elipse
            float x = Mathf.Sin(rad) * radioX;
            float y = -Mathf.Cos(rad) * radioY;   // negativo: 0° = arriba del círculo → frente
            Vector2 targetPos = new Vector2(x, y);

            // cos(angle): 1 = frente, -1 = detrás
            float depth = Mathf.Cos(rad);  // -1 a 1

            // Escala según profundidad
            float t = (depth + 1f) / 2f;  // normalizar a 0-1
            float targetScale = Mathf.Lerp(backScale, frontScale, t);

            // Alpha según profundidad
            float targetAlpha = Mathf.Lerp(backAlpha, frontAlpha, t);

            if (instant)
            {
                panels[i].anchoredPosition = targetPos;
                panels[i].localScale = Vector3.one * targetScale;
                canvasGroups[i].alpha = targetAlpha;
            }
            else
            {
                panels[i].anchoredPosition = Vector2.Lerp(
                    panels[i].anchoredPosition, targetPos, Time.deltaTime * animationSpeed);
                panels[i].localScale = Vector3.Lerp(
                    panels[i].localScale, Vector3.one * targetScale, Time.deltaTime * animationSpeed);
                canvasGroups[i].alpha = Mathf.Lerp(
                    canvasGroups[i].alpha, targetAlpha, Time.deltaTime * animationSpeed);
            }

            // Solo el del frente es interactuable
            bool isFront = (i == currentIndex);
            canvasGroups[i].interactable = isFront;
            canvasGroups[i].blocksRaycasts = isFront;

            // Sorting: más al frente = más arriba en jerarquía
            panels[i].SetSiblingIndex(Mathf.RoundToInt(t * (panels.Count - 1)));
        }
    }

    // ─── ROTACIÓN ───────────────────────────────────────────────────────────────

    void Rotate(int direction)
    {
        // direction: +1 = siguiente, -1 = anterior
        float step = slotAngle * direction;

        for (int i = 0; i < panels.Count; i++)
        {
            angles[i] -= step;

            // Mantener ángulos en 0-360
            angles[i] = (angles[i] % 360f + 360f) % 360f;
        }

        // Actualizar índice actual (el que queda más cerca de 0°)
        currentIndex = GetFrontIndex();
    }

    int GetFrontIndex()
    {
        int front = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < panels.Count; i++)
        {
            // Distancia angular a 0° (frente)
            float a = (angles[i] % 360f + 360f) % 360f;
            float dist = Mathf.Min(a, 360f - a);
            if (dist < minDist)
            {
                minDist = dist;
                front = i;
            }
        }
        return front;
    }

    // ─── EVENTOS ────────────────────────────────────────────────────────────────

    public void OnDrag(PointerEventData eventData)
    {
        // Preview suave mientras arrastra
        float delta = eventData.delta.x;
        float rotAmount = delta * 0.15f;   // sensibilidad del drag

        for (int i = 0; i < panels.Count; i++)
            angles[i] = (angles[i] - rotAmount % 360f + 360f) % 360f;

        currentIndex = GetFrontIndex();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap al panel más cercano al frente
        SnapToNearest();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData.scrollDelta.y > 0)
            Rotate(-1);
        else if (eventData.scrollDelta.y < 0)
            Rotate(1);
    }

    // ─── SNAP ───────────────────────────────────────────────────────────────────

    void SnapToNearest()
    {
        // Rotar hasta que el panel más cercano quede exactamente en 0°
        int front = GetFrontIndex();
        float frontAngle = angles[front];

        // Ángulo más corto hacia 0°
        float delta = frontAngle;
        if (delta > 180f) delta -= 360f;

        for (int i = 0; i < panels.Count; i++)
        {
            angles[i] = (angles[i] - delta % 360f + 360f) % 360f;
        }

        currentIndex = front;
    }

    // ─── BOTONES ────────────────────────────────────────────────────────────────

    public void Next() => Rotate(1);
    public void Previous() => Rotate(-1);
}