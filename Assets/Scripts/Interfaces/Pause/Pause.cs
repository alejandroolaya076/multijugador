using UnityEngine;
using UnityEngine.InputSystem;

public class Pausa : MonoBehaviour
{

    public GameObject menuPausa;
    public bool juegoPausado = false ;

    private PlayerInput playerInput;
    private InputAction pauseAction ;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            pauseAction = playerInput.actions["Pause"];
        }
        else
        {
            pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
            pauseAction.Enable();
        }
    }

    void OnEnable()
    {
        pauseAction.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        pauseAction.performed -= OnPausePerformed;

        
        if (playerInput == null)
        {
            pauseAction.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (juegoPausado)
            Reanudar();
        else
            Pausar();
    
    }

    public void Reanudar()
    {
        menuPausa.SetActive(false);
        Time.timeScale = 1;
        juegoPausado = false;
    }

    public void Pausar()
    {
        menuPausa.SetActive(true);
        Time.timeScale = 0;
        juegoPausado = true;
    }
}