using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILogin : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("Buttons")]
    public Button signInButton;
    public Button googleButton;       

    [Header("Feedback")]
    public TMP_Text errorText;
    public GameObject loadingIndicator;

    [Header("Systems")]
    public LoginSystem loginSystem;
    // -------------------------------------------------------
    void Awake()
    {
        if (signInButton != null)
            signInButton.onClick.AddListener(OnClickSignIn);
        else
            Debug.LogError(" UILogin: 'Sign In Button' no asignado en el Inspector.");

        if (googleButton != null)
            googleButton.onClick.AddListener(OnClickGoogle);  // ← NUEVO
        else
            Debug.LogWarning(" UILogin: 'Google Button' no asignado en el Inspector.");


        if (errorText != null)        errorText.gameObject.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    // -------------------------------------------------------
    private void OnClickSignIn()
{
    Debug.Log(" BOTON SUBMIT PRESIONADO");

    if (usernameInput == null || passwordInput == null)
    {
        Debug.LogError(" InputFields nulos");
        return;
    }

    string username = usernameInput.text.Trim();
    string password = passwordInput.text;

    Debug.Log($"[Login] Usuario: '{username}' | Pass length: {password.Length}");

    if (string.IsNullOrWhiteSpace(username))
    {
        Debug.LogError(" Username vacío");
        ShowError("Ingresa tu nombre de usuario.");
        return;
    }
    if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
    {
        Debug.LogError($" Password inválido, length: {password.Length}");
        ShowError("La contraseña debe tener al menos 6 caracteres.");
        return;
    }
    if (loginSystem == null)
    {
        Debug.LogError(" LoginSystem es null");
        return;
    }

    Debug.Log(" Llamando loginSystem.Login()");
    HideError();
    SetLoading(true);
    loginSystem.Login(username, password);
}
    //  Login con Google
    private void OnClickGoogle()
    {
        if (loginSystem == null)
        {
            Debug.LogError(" UILogin: 'Login System' no asignado en el Inspector.");
            return;
        }

        HideError();
        SetLoading(true);
        loginSystem.LoginWithGoogle();
    }


   
    //  CALLBACKS desde LoginSystem
   
    public void OnLoginSuccess()
    {
        SetLoading(false);
        ClearFields();
        Debug.Log($" Bienvenido, {LoginSystem.Username}!");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    public void OnLoginFailed(string message)
    {
        SetLoading(false);
        ShowError(message);
        Debug.LogWarning($" Login fallido: {message}");
    }

    
    //  HELPERS
    
    public void ClearFields()
    {
        if (usernameInput != null) usernameInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
    }

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (errorText == null) return;
        errorText.gameObject.SetActive(false);
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(isLoading);
        if (signInButton     != null) signInButton.interactable     = !isLoading;
        if (googleButton     != null) googleButton.interactable     = !isLoading; 
    }
}