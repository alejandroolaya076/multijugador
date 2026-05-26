using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;
// ============================================================
//  AudioPanel.cs
//  Adjunta este script al GameObject del panel de audio.
//  Requiere: Unity UI + TextMeshPro + Audio Mixer
// ============================================================

public class AudioPanel : MonoBehaviour
{
    // ---------------------------------------------------------
    // Referencias al Audio Mixer
    // ---------------------------------------------------------
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    // Nombres de los parámetros expuestos en el Mixer:
    // (clic derecho sobre el volumen en el Mixer → Expose parameter)
    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_MUSIC  = "MusicVolume";
    private const string PARAM_SFX    = "SFXVolume";

    // ---------------------------------------------------------
    // Sliders
    // ---------------------------------------------------------
    [Header("Sliders")]
    public Slider sliderMaster;
    public Slider sliderMusic;
    public Slider sliderSFX;

    // ---------------------------------------------------------
    // Labels de valor (ej: "80%")
    // ---------------------------------------------------------
    [Header("Value Labels (opcional)")]
    public TMP_Text labelMaster;
    public TMP_Text labelMusic;
    public TMP_Text labelSFX;

    // ---------------------------------------------------------
    // Toggle de mute
    // ---------------------------------------------------------
    [Header("Toggle Mute All (opcional)")]
    public Toggle toggleMute;

    // ---------------------------------------------------------
    // Botones del pie
    // ---------------------------------------------------------
    [Header("Botones")]
    public Button saveButton;
    public Button defaultButton;
    public Button exitButton;

    // ---------------------------------------------------------
    // Valores por defecto (0 a 1)
    // ---------------------------------------------------------
    private const float DEFAULT_MASTER = 0.8f;
    private const float DEFAULT_MUSIC  = 0.6f;
    private const float DEFAULT_SFX    = 1.0f;

    // PlayerPrefs keys
    private const string KEY_MASTER = "audio_master";
    private const string KEY_MUSIC  = "audio_music";
    private const string KEY_SFX    = "audio_sfx";
    private const string KEY_MUTE   = "audio_mute";

    // Estado previo al mute para restaurar
    private float _preMutemaster, _preMuteMusic, _preMuteSFX;
    private bool  _isMuted = false;

    // =========================================================
    // Unity lifecycle
    // =========================================================

    private void Awake()
    {
        // Configurar sliders entre 0 y 1
        SetupSliderRange(sliderMaster);
        SetupSliderRange(sliderMusic);
        SetupSliderRange(sliderSFX);

        // Cargar valores guardados
        LoadValues();

        // Conectar eventos
        sliderMaster.onValueChanged.AddListener(OnMasterChanged);
        sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        sliderSFX.onValueChanged.AddListener(OnSFXChanged);

        if (toggleMute != null)
            toggleMute.onValueChanged.AddListener(OnMuteChanged);

        if (saveButton    != null) saveButton.onClick.AddListener(SaveValues);
        if (defaultButton != null) defaultButton.onClick.AddListener(ResetToDefaults);
        if (exitButton    != null) exitButton.onClick.AddListener(OnExit);
    }

    // =========================================================
    // Callbacks de sliders
    // =========================================================

    private void OnMasterChanged(float value)
    {
        ApplyVolume(PARAM_MASTER, value);
        UpdateLabel(labelMaster, value);
    }

    private void OnMusicChanged(float value)
    {
        ApplyVolume(PARAM_MUSIC, value);
        UpdateLabel(labelMusic, value);
    }

    private void OnSFXChanged(float value)
    {
        ApplyVolume(PARAM_SFX, value);
        UpdateLabel(labelSFX, value);
    }

    // =========================================================
    // Mute
    // =========================================================

    private void OnMuteChanged(bool muted)
    {
        _isMuted = muted;

        if (muted)
        {
            // Guardar valores actuales y silenciar
            _preMutemaster = sliderMaster.value;
            _preMuteMusic  = sliderMusic.value;
            _preMuteSFX    = sliderSFX.value;

            ApplyVolume(PARAM_MASTER, 0f);
            ApplyVolume(PARAM_MUSIC,  0f);
            ApplyVolume(PARAM_SFX,    0f);

            // Deshabilitar sliders visualmente
            sliderMaster.interactable = false;
            sliderMusic.interactable  = false;
            sliderSFX.interactable    = false;
        }
        else
        {
            // Restaurar
            ApplyVolume(PARAM_MASTER, _preMutemaster);
            ApplyVolume(PARAM_MUSIC,  _preMuteMusic);
            ApplyVolume(PARAM_SFX,    _preMuteSFX);

            sliderMaster.interactable = true;
            sliderMusic.interactable  = true;
            sliderSFX.interactable    = true;
        }
    }

    // =========================================================
    // Aplicar volumen al Mixer
    // El AudioMixer trabaja en decibeles: convertimos 0-1 a dB
    // Fórmula: dB = log10(value) * 20   (0.001 mínimo para evitar -infinito)
    // =========================================================

    private void ApplyVolume(string parameter, float linearValue)
    {
        if (audioMixer == null) return;

        float dB = linearValue > 0.001f
            ? Mathf.Log10(linearValue) * 20f
            : -80f; // silencio total

        audioMixer.SetFloat(parameter, dB);
    }

    // =========================================================
    // Guardar / Cargar / Resetear
    // =========================================================

    public void SaveValues()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, sliderMaster.value);
        PlayerPrefs.SetFloat(KEY_MUSIC,  sliderMusic.value);
        PlayerPrefs.SetFloat(KEY_SFX,    sliderSFX.value);
        PlayerPrefs.SetInt(KEY_MUTE, _isMuted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("[AudioPanel] Valores guardados.");
    }

    private void LoadValues()
    {
        float master = PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER);
        float music  = PlayerPrefs.GetFloat(KEY_MUSIC,  DEFAULT_MUSIC);
        float sfx    = PlayerPrefs.GetFloat(KEY_SFX,    DEFAULT_SFX);
        bool  muted  = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        // Aplicar a sliders (dispara onValueChanged automáticamente)
        sliderMaster.value = master;
        sliderMusic.value  = music;
        sliderSFX.value    = sfx;

        if (toggleMute != null)
            toggleMute.isOn = muted;
    }

    public void ResetToDefaults()
    {
        sliderMaster.value = DEFAULT_MASTER;
        sliderMusic.value  = DEFAULT_MUSIC;
        sliderSFX.value    = DEFAULT_SFX;

        if (toggleMute != null)
            toggleMute.isOn = false;

        Debug.Log("[AudioPanel] Valores reseteados.");
    }

    private void OnExit()
    {
        LoadValues(); // descarta cambios no guardados
        SceneManager.LoadScene("Menu");
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void SetupSliderRange(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
    }

    private void UpdateLabel(TMP_Text label, float value)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // =========================================================
    // Acceso público
    // =========================================================

    /// <summary>Singleton opcional — igual que ControlsPanel.</summary>
    public static AudioPanel Instance { get; private set; }
    private void OnEnable()  { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }
}
