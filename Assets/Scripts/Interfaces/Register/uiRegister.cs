using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class UIRegister : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Buttons")]
    public Button createAccountButton;
    public Button signInWithGoogleButton;

    [Header("Systems")]
    public RegisterSystem registerSystem;

    // -------------------------------------------------------
    void Start()
    {
        // Asigna listeners a los botones
        createAccountButton.onClick.AddListener(OnClickCreateAccount);

        // Google aún no implementado
        signInWithGoogleButton.onClick.AddListener(OnClickGoogle);
    }

    // -------------------------------------------------------
    private void OnClickCreateAccount()
    {
        string username        = usernameInput.text.Trim();
        string email           = emailInput.text.Trim();
        string password        = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        registerSystem.Register(username, email, password, confirmPassword);
    }



    private void OnClickGoogle()
    {
        Debug.Log(" Google Sign-In aún no implementado.");
    }

    // -------------------------------------------------------
    /// <summary>
    /// Limpia todos los campos del formulario.
    /// Llámalo desde RegisterSystem cuando el registro sea exitoso.
    /// </summary>
    public void ClearFields()
    {
        usernameInput.text        = "";
        emailInput.text           = "";
        passwordInput.text        = "";
        confirmPasswordInput.text = "";
    }
}