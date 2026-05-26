using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoginManager : MonoBehaviour
{
     [Header("Sistemas")]
    public LoginSystem loginSystem;       
    public RegisterSystem registerSystem;
    [Header("Login")]
    public TMP_InputField inputUsuario;
    public TMP_InputField inputPassword;

    [Header("Register")]
    public TMP_InputField inputNuevoUsuario;
    public TMP_InputField inputEmail;
    public TMP_InputField inputNuevaPassword;
    public TMP_InputField inputConfirmarPassword;

    [Header("Panel de alerta")]
    public GameObject panelAlerta;
    public TMP_Text txtMensajeAlerta;

    // ─── LOGIN ───────────────────────────────────────────

    public void SubmitLogin()
    {
        string usuario  = inputUsuario.text.Trim();
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(usuario))
        {
            MostrarAlerta("El nombre de usuario\nno puede estar vacío.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            MostrarAlerta("Ingresa tu contraseña.");
            return;
        }

        if (password.Length < 6)
        {
            MostrarAlerta("La contraseña debe tener\nal menos 6 caracteres.");
            return;
        }

        // Todo válido — aquí irá la llamada al servidor después
        loginSystem.Login(usuario, password);
    }

    // ─── REGISTER ────────────────────────────────────────

    public void SubmitRegister()
    {
        string usuario          = inputNuevoUsuario.text.Trim();
        string email            = inputEmail.text.Trim();
        string password         = inputNuevaPassword.text;
        string confirmarPassword = inputConfirmarPassword.text;

        if (string.IsNullOrEmpty(usuario))
        {
            MostrarAlerta("El nombre de usuario\nno puede estar vacío.");
            return;
        }

        if (usuario.Length < 3)
        {
            MostrarAlerta("El usuario debe tener\nal menos 3 caracteres.");
            return;
        }

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            MostrarAlerta("Ingresa un email válido.");
            return;
        }

        if (password.Length < 6)
        {
            MostrarAlerta("La contraseña debe tener\nal menos 6 caracteres.");
            return;
        }

        if (password != confirmarPassword)
        {
            MostrarAlerta("Las contraseñas\nno coinciden.");
            return;
        }

        // Todo válido — aquí irá la llamada al servidor después
        registerSystem.Register(usuario, email, password, confirmarPassword);
    }

    // ─── ALERTA ───────────────────────────────────────────

    public void MostrarAlerta(string mensaje)
    {
        txtMensajeAlerta.text = mensaje;
        panelAlerta.SetActive(true);
        StartCoroutine(CerrarAlertaAutomatico());
    }

    // Se cierra solo después de 3 segundos
    IEnumerator CerrarAlertaAutomatico()
    {
        yield return new WaitForSeconds(3f);
        CerrarAlerta();
    }

    // Llama esto desde el botón OK del panel
    public void CerrarAlerta()
    {
        panelAlerta.SetActive(false);
    }
}