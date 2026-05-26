using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
// ============================================================
//  VideoPanel.cs
// ============================================================

public class VideoPanel : MonoBehaviour
{
    // ---------------------------------------------------------
    // Resolución
    // ---------------------------------------------------------
    [Header("Resolución")]
    public TMP_Dropdown dropdownResolution;

    // ---------------------------------------------------------
    // Fullscreen
    // ---------------------------------------------------------
    [Header("Fullscreen")]
    public Toggle toggleFullscreen;

    // ---------------------------------------------------------
    // VSync
    // ---------------------------------------------------------
    [Header("VSync")]
    public Toggle toggleVSync;

    // ---------------------------------------------------------
    // Calidad
    // ---------------------------------------------------------
    [Header("Calidad")]
    public TMP_Dropdown dropdownQuality;

    // ---------------------------------------------------------
    // Brillo
    // ---------------------------------------------------------
    [Header("Brillo")]
    public Slider  sliderBrightness;
    public TMP_Text labelBrightness;

    // Si tienes un panel/imagen overlay para simular brillo:
    // asigna aquí una Image con color negro y alpha variable
    [Header("Brightness Overlay (opcional)")]
    public CanvasGroup brightnessOverlay;

    // ---------------------------------------------------------
    // Botones
    // ---------------------------------------------------------
    [Header("Botones")]
    public Button saveButton;
    public Button defaultButton;
    public Button exitButton;

    // ---------------------------------------------------------
    // PlayerPrefs keys
    // ---------------------------------------------------------
    private const string KEY_RES_INDEX  = "video_res_index";
    private const string KEY_FULLSCREEN = "video_fullscreen";
    private const string KEY_VSYNC      = "video_vsync";
    private const string KEY_QUALITY    = "video_quality";
    private const string KEY_BRIGHTNESS = "video_brightness";

    // ---------------------------------------------------------
    // Defaults
    // ---------------------------------------------------------
    private const bool  DEFAULT_FULLSCREEN = true;
    private const bool  DEFAULT_VSYNC      = true;
    private const int   DEFAULT_QUALITY    = 2;   // índice en QualitySettings
    private const float DEFAULT_BRIGHTNESS = 1f;  // 0.5 a 1.5

    // ---------------------------------------------------------
    // Estado interno
    // ---------------------------------------------------------
    private List<Resolution> _resolutions = new();

    // =========================================================
    // Unity lifecycle
    // =========================================================

    private void Awake()
    {
        BuildResolutionDropdown();
        BuildQualityDropdown();
        SetupBrightnessSlider();
        LoadValues();
        HookEvents();
    }

    // =========================================================
    // Build dropdowns
    // =========================================================

    private void BuildResolutionDropdown()
    {
        if (dropdownResolution == null) return;

        dropdownResolution.ClearOptions();
        _resolutions.Clear();

        // Unity devuelve resoluciones duplicadas (distintos refresh rates)
        // → filtramos para mostrar solo resoluciones únicas
        var seen = new HashSet<string>();
        Resolution[] all = Screen.resolutions;

        // Recorremos al revés para tener las más altas primero
        for (int i = all.Length - 1; i >= 0; i--)
        {
            string key = $"{all[i].width}x{all[i].height}";
            if (seen.Contains(key)) continue;
            seen.Add(key);
            _resolutions.Add(all[i]);
        }

        // Construir opciones de texto
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var r in _resolutions)
            options.Add(new TMP_Dropdown.OptionData($"{r.width} × {r.height}"));

        dropdownResolution.AddOptions(options);

        // Seleccionar la resolución actual por defecto
        Resolution current = Screen.currentResolution;
        int defaultIndex = 0;
        for (int i = 0; i < _resolutions.Count; i++)
        {
            if (_resolutions[i].width  == current.width &&
                _resolutions[i].height == current.height)
            {
                defaultIndex = i;
                break;
            }
        }
        dropdownResolution.value = defaultIndex;
        dropdownResolution.RefreshShownValue();
    }

    private void BuildQualityDropdown()
    {
        if (dropdownQuality == null) return;

        dropdownQuality.ClearOptions();

        // Usa los nombres definidos en Edit → Project Settings → Quality
        string[] names = QualitySettings.names;
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (string n in names)
            options.Add(new TMP_Dropdown.OptionData(n));

        dropdownQuality.AddOptions(options);
    }

    private void SetupBrightnessSlider()
    {
        if (sliderBrightness == null) return;
        sliderBrightness.minValue = 0.5f;  // oscuro
        sliderBrightness.maxValue = 1.5f;  // brillante
        sliderBrightness.value    = DEFAULT_BRIGHTNESS;
    }

    // =========================================================
    // Hook eventos
    // =========================================================

    private void HookEvents()
    {
        if (dropdownResolution != null)
            dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);

        if (toggleFullscreen != null)
            toggleFullscreen.onValueChanged.AddListener(OnFullscreenChanged);

        if (toggleVSync != null)
            toggleVSync.onValueChanged.AddListener(OnVSyncChanged);

        if (dropdownQuality != null)
            dropdownQuality.onValueChanged.AddListener(OnQualityChanged);

        if (sliderBrightness != null)
            sliderBrightness.onValueChanged.AddListener(OnBrightnessChanged);

        if (saveButton    != null) saveButton.onClick.AddListener(SaveValues);
        if (defaultButton != null) defaultButton.onClick.AddListener(ResetToDefaults);
        if (exitButton    != null) exitButton.onClick.AddListener(OnExit);
    }

    // =========================================================
    // Callbacks
    // =========================================================

    private void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _resolutions.Count) return;

        Resolution r = _resolutions[index];
        bool fs = toggleFullscreen != null && toggleFullscreen.isOn;

        Screen.SetResolution(r.width, r.height, fs);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreenMode = isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void OnVSyncChanged(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
    }

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);
        if (labelBrightness != null)
            labelBrightness.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // =========================================================
    // Brillo
    // Técnica: un CanvasGroup overlay negro cuyo alpha bajamos/subimos.
    // value < 1  → alpha positivo (oscurece)
    // value > 1  → no podemos "aclarar" con un overlay negro,
    //              pero sí podemos usar Screen.brightness en móvil,
    //              o un overlay blanco si lo prefieres.
    // =========================================================

    private void ApplyBrightness(float value)
    {
        // Opción A: overlay negro (soportado en todas las plataformas)
        if (brightnessOverlay != null)
        {
            // value 0.5 → alpha 0.5 (muy oscuro)
            // value 1.0 → alpha 0   (normal)
            // value 1.5 → alpha 0   (no podemos ir más brillante con overlay negro)
            brightnessOverlay.alpha = Mathf.Clamp01(1f - value);
        }

    }

    // =========================================================
    // Guardar / Cargar / Resetear
    // =========================================================

    public void SaveValues()
    {
        if (dropdownResolution != null)
            PlayerPrefs.SetInt(KEY_RES_INDEX, dropdownResolution.value);

        PlayerPrefs.SetInt(KEY_FULLSCREEN, (toggleFullscreen != null && toggleFullscreen.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(KEY_VSYNC,      (toggleVSync      != null && toggleVSync.isOn)      ? 1 : 0);

        if (dropdownQuality != null)
            PlayerPrefs.SetInt(KEY_QUALITY, dropdownQuality.value);

        if (sliderBrightness != null)
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS, sliderBrightness.value);

        PlayerPrefs.Save();
        Debug.Log("[VideoPanel] Valores guardados.");
    }

    private void LoadValues()
    {
        // Resolución
        if (dropdownResolution != null)
        {
            int savedIndex = PlayerPrefs.GetInt(KEY_RES_INDEX, dropdownResolution.value);
            savedIndex = Mathf.Clamp(savedIndex, 0, _resolutions.Count - 1);
            dropdownResolution.value = savedIndex;
            dropdownResolution.RefreshShownValue();
        }

        // Fullscreen
        if (toggleFullscreen != null)
            toggleFullscreen.isOn = PlayerPrefs.GetInt(KEY_FULLSCREEN, DEFAULT_FULLSCREEN ? 1 : 0) == 1;

        // VSync
        if (toggleVSync != null)
            toggleVSync.isOn = PlayerPrefs.GetInt(KEY_VSYNC, DEFAULT_VSYNC ? 1 : 0) == 1;

        // Calidad
        if (dropdownQuality != null)
        {
            int qi = PlayerPrefs.GetInt(KEY_QUALITY, DEFAULT_QUALITY);
            qi = Mathf.Clamp(qi, 0, QualitySettings.names.Length - 1);
            dropdownQuality.value = qi;
        }

        // Brillo
        if (sliderBrightness != null)
        {
            float b = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, DEFAULT_BRIGHTNESS);
            sliderBrightness.value = b; // dispara OnBrightnessChanged
        }
    }

    public void ResetToDefaults()
    {
        if (toggleFullscreen != null) toggleFullscreen.isOn  = DEFAULT_FULLSCREEN;
        if (toggleVSync      != null) toggleVSync.isOn       = DEFAULT_VSYNC;
        if (dropdownQuality  != null) dropdownQuality.value  = DEFAULT_QUALITY;
        if (sliderBrightness != null) sliderBrightness.value = DEFAULT_BRIGHTNESS;

        // Resolución: seleccionar la nativa actual
        BuildResolutionDropdown();

        Debug.Log("[VideoPanel] Valores reseteados.");
    }

    private void OnExit()
    {
        LoadValues();
        SceneManager.LoadScene("Menu");
    }

    // =========================================================
    // Singleton opcional
    // =========================================================
    public static VideoPanel Instance { get; private set; }
    private void OnEnable()  { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }
}
