using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class GameplaySettings : MonoBehaviour
{
    [Header("Language")]
    public TMP_Dropdown languageDropdown;

    [Header("FPS Cap")]
    public TMP_Dropdown fpsDropdown;

    // Opciones de idioma
    private string[] languages = { "English", "Español", "French", "Deutsch", "Português" };

    // Opciones de FPS
    private int[] fpsOptions = { 30, 60, 120, 144, 240, 0 }; // 0 = Unlimited

    void Start()
    {
        PopulateDropdowns();
        LoadSettings();
    }

    void PopulateDropdowns()
    {
        // Llenar idiomas
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string>(languages));

        // Llenar FPS
        fpsDropdown.ClearOptions();
        var fpsList = new System.Collections.Generic.List<string>();
        foreach (int fps in fpsOptions)
            fpsList.Add(fps == 0 ? "Unlimited" : fps.ToString());
        fpsDropdown.AddOptions(fpsList);
    }

    public void OnSave()
    {
        // Guardar idioma
        int langIndex = languageDropdown.value;
        PlayerPrefs.SetInt("LanguageIndex", langIndex);

        // Guardar y aplicar FPS
        int fpsIndex = fpsDropdown.value;
        PlayerPrefs.SetInt("FPSIndex", fpsIndex);
        Application.targetFrameRate = fpsOptions[fpsIndex];

        PlayerPrefs.Save();
        Debug.Log($"Guardado — Idioma: {languages[langIndex]}, FPS: {fpsOptions[fpsIndex]}");
    }

    public void OnDefault()
    {
        languageDropdown.value = 0;   // English
        fpsDropdown.value = 2;        // 120
    }

    public void OnExit()
    {
        SceneManager.LoadScene("Menu");
    }

    void LoadSettings()
    {
        languageDropdown.value = PlayerPrefs.GetInt("LanguageIndex", 0);

        int fpsIndex = PlayerPrefs.GetInt("FPSIndex", 2); // Default: 120
        fpsDropdown.value = fpsIndex;
        Application.targetFrameRate = fpsOptions[fpsIndex];
    }
}