using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button reiniciarButton;
    public Button menuButton;

    private bool gameOverActivo = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (reiniciarButton != null)
        {
            reiniciarButton.onClick.AddListener(ReiniciarJuego);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(VolverAlMenu);
        }
    }

    void Update()
    {
        if (gameOverActivo && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ReiniciarJuego();
        }
        if (gameOverActivo && (Keyboard.current.mKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            VolverAlMenu();
        }
    }

    public void GameOver()
    {
        if (gameOverActivo) return;

        gameOverActivo = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = "¡Has Perdido!";
        }
    }

    public void ReiniciarJuego()
    {
        gameOverActivo = false; 
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); 

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}