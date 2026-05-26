using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class LoginSystem : MonoBehaviour
{
    [Header("Supabase Config")]
    public string supabaseUrl = "https://ihwughhiqdoiwkctbdcr.supabase.co";
    public string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imlod3VnaGhpcWRvaXdrY3RiZGNyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI3ODY2MTUsImV4cCI6MjA4ODM2MjYxNX0.dtKkp9l2G1hG4cLKYADZr93c0ZHKbjbgLVfyibXYkCg";

    [Header("UI")]
    public UILogin uiLogin;

    public static string AccessToken { get; private set; }
    public static string UserId      { get; private set; }
    public static string Username    { get; private set; }
    public static int    Level       { get; private set; }
    public static int    Coins       { get; private set; }
    public static bool   IsLoggedIn  => !string.IsNullOrEmpty(AccessToken);

    // Google OAuth
    private GoogleAuthListener _googleListener;
    private const string GOOGLE_REDIRECT = "https://alejandroolaya076.github.io/google/";

    void Awake()
    {
        UnityMainThreadDispatcher.Instance();
    }


    
    //  LOGIN NORMAL
    

    public void Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            if (uiLogin != null) uiLogin.OnLoginFailed("Ingresa tu nombre de usuario.");
            return;
        }
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            if (uiLogin != null) uiLogin.OnLoginFailed("Contraseña inválida.");
            return;
        }

        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        Debug.Log($"[RPC] Buscando email para: {username}");

        string rpcUrl  = $"{supabaseUrl}/rest/v1/rpc/get_email_by_username";
        string rpcJson = $"{{\"p_username\":\"{EscapeJson(username)}\"}}";
        string email   = null;

        using (UnityWebRequest rpcReq = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(rpcJson);
            rpcReq.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            rpcReq.downloadHandler = new DownloadHandlerBuffer();
            rpcReq.SetRequestHeader("Content-Type",  "application/json");
            rpcReq.SetRequestHeader("apikey",        supabaseKey);
            rpcReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");

            yield return rpcReq.SendWebRequest();

            string rpcResponse = rpcReq.downloadHandler.text;
            Debug.Log($"[RPC] Respuesta: {rpcResponse}");

            try
            {
                if (rpcReq.result != UnityWebRequest.Result.Success)
                    throw new Exception($"HTTP {rpcReq.responseCode} – {rpcReq.error}");

                email = rpcResponse.Trim().Trim('"');

                if (string.IsNullOrEmpty(email) || email == "null")
                    throw new Exception("Usuario no encontrado.");

                Debug.Log($"[RPC] Email obtenido para '{username}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RPC] {ex.Message}");
                if (uiLogin != null) uiLogin.OnLoginFailed("Usuario no encontrado.");
                yield break;
            }
        }

        Debug.Log("[Auth] Autenticando...");

        string authUrl  = $"{supabaseUrl}/auth/v1/token?grant_type=password";
        string authJson = $"{{" +
                          $"\"email\":\"{EscapeJson(email)}\"," +
                          $"\"password\":\"{EscapeJson(password)}\"" +
                          $"}}";

        string userId      = null;
        string accessToken = null;

        using (UnityWebRequest authReq = new UnityWebRequest(authUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(authJson);
            authReq.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            authReq.downloadHandler = new DownloadHandlerBuffer();
            authReq.SetRequestHeader("Content-Type",  "application/json");
            authReq.SetRequestHeader("apikey",        supabaseKey);
            authReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");

            yield return authReq.SendWebRequest();

            string response = authReq.downloadHandler.text;
            Debug.Log($"[Auth] Respuesta raw: {response}");

            try
            {
                if (authReq.result != UnityWebRequest.Result.Success)
                    throw new Exception($"HTTP {authReq.responseCode} – {authReq.error}\n{response}");

                accessToken = ExtractJsonField(response, "access_token");
                userId      = ExtractNestedField(response, "user", "id");

                if (string.IsNullOrEmpty(accessToken))
                    throw new Exception("Contraseña incorrecta.");

                if (string.IsNullOrEmpty(userId))
                    throw new Exception("No se pudo obtener el ID del usuario.");

                Debug.Log($"[Auth] Sesión iniciada. ID: {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] {ex.Message}");
                if (uiLogin != null) uiLogin.OnLoginFailed("Usuario o contraseña incorrectos.");
                yield break;
            }
        }

        Debug.Log("[DB] Cargando perfil...");

        string dbUrl = $"{supabaseUrl}/rest/v1/players?id=eq.{userId}&select=*";

        using (UnityWebRequest dbReq = UnityWebRequest.Get(dbUrl))
        {
            dbReq.SetRequestHeader("apikey",        supabaseKey);
            dbReq.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            dbReq.SetRequestHeader("Accept",        "application/json");

            yield return dbReq.SendWebRequest();

            string dbResponse = dbReq.downloadHandler.text;
            Debug.Log($"[DB] Perfil raw: {dbResponse}");

            try
            {
                if (dbReq.result != UnityWebRequest.Result.Success)
                    throw new Exception($"HTTP {dbReq.responseCode} – {dbReq.error}\n{dbResponse}");

                string loadedUsername = ExtractJsonField(dbResponse, "username");
                string levelStr       = ExtractJsonField(dbResponse, "level");
                string coinsStr       = ExtractJsonField(dbResponse, "coins");

                if (string.IsNullOrEmpty(loadedUsername))
                    throw new Exception("Perfil no encontrado en la tabla players.");

                AccessToken = accessToken;
                UserId      = userId;
                Username    = loadedUsername;
                Level       = int.TryParse(levelStr, out int lvl)  ? lvl   : 1;
                Coins       = int.TryParse(coinsStr, out int coins) ? coins : 0;

                Debug.Log($"[DB] Login exitoso → {Username} | Nivel: {Level} | Monedas: {Coins}");
                if (uiLogin != null) uiLogin.OnLoginSuccess();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DB] {ex.Message}");
                if (uiLogin != null) uiLogin.OnLoginFailed(ex.Message);
            }
        }
    }


    //  LOGIN CON GOOGLE


    public void LoginWithGoogle()
    {
        string oauthUrl = $"{supabaseUrl}/auth/v1/authorize" +
                        $"?provider=google" +
                        $"&redirect_to={Uri.EscapeDataString(GOOGLE_REDIRECT)}" +
                        $"&prompt=select_account";

        _googleListener = new GoogleAuthListener();

        _googleListener.OnTokenReceived = (accessToken, refreshToken) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                StartCoroutine(LoadProfileAfterGoogleLogin(accessToken))
            );
        };

        _googleListener.OnError = (error) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.LogError($"[Google] {error}");
                if (uiLogin != null) uiLogin.OnLoginFailed("Error al iniciar sesión con Google.");
            });
        };

        _googleListener.StartListening();
        Application.OpenURL(oauthUrl);
        Debug.Log("[Google] Navegador abierto para autenticación.");
    }

    private IEnumerator LoadProfileAfterGoogleLogin(string accessToken)
{
    Debug.Log("[Google] Token recibido, cargando perfil...");

    string userUrl = $"{supabaseUrl}/auth/v1/user";
    string userId  = null;
    string email   = null;

    using (UnityWebRequest req = UnityWebRequest.Get(userUrl))
    {
        req.SetRequestHeader("apikey",        supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        yield return req.SendWebRequest();

        string response = req.downloadHandler.text;
        Debug.Log($"[Google] User response: {response}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            if (uiLogin != null) uiLogin.OnLoginFailed("Error al obtener datos de Google.");
            yield break;
        }

        userId = ExtractJsonField(response, "id");
        email  = ExtractJsonField(response, "email");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            if (uiLogin != null) uiLogin.OnLoginFailed("Error al obtener datos de Google.");
            yield break;
        }
    }

    string dbUrl = $"{supabaseUrl}/rest/v1/players?id=eq.{userId}&select=*";

    using (UnityWebRequest dbReq = UnityWebRequest.Get(dbUrl))
    {
        dbReq.SetRequestHeader("apikey",        supabaseKey);
        dbReq.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        dbReq.SetRequestHeader("Accept",        "application/json");

        yield return dbReq.SendWebRequest();

        string dbResponse = dbReq.downloadHandler.text;
        Debug.Log($"[Google] DB response: {dbResponse}");

        string loadedUsername = ExtractJsonField(dbResponse, "username");
        bool usernameIsNull   = string.IsNullOrEmpty(loadedUsername) || loadedUsername == "null";

        bool noProfile = dbResponse.Trim() == "[]" ||
                         dbResponse.Trim() == "null" ||
                         string.IsNullOrEmpty(dbResponse.Trim());

        if (noProfile || usernameIsNull) 
        {
            yield return StartCoroutine(UpsertGoogleProfile(accessToken, userId, email));
        }
        else
        {
            AccessToken = accessToken;
            UserId      = userId;
            Username    = loadedUsername;
            Level       = int.TryParse(ExtractJsonField(dbResponse, "level"), out int lvl)  ? lvl   : 1;
            Coins       = int.TryParse(ExtractJsonField(dbResponse, "coins"), out int coins) ? coins : 0;

            Debug.Log($"[Google] Login exitoso → {Username} | Nivel: {Level} | Monedas: {Coins}");
            if (uiLogin != null) uiLogin.OnLoginSuccess();
        }
    }
}

private IEnumerator UpsertGoogleProfile(string accessToken, string userId, string email)
{
    string baseUsername = email.Split('@')[0].Replace(".", "_");
    string newUsername  = baseUsername + UnityEngine.Random.Range(100, 999);

    Debug.Log($"[Google] Upsert perfil para: {newUsername}");

    // Primero intentar PATCH (actualizar si ya existe)
    string patchUrl  = $"{supabaseUrl}/rest/v1/players?id=eq.{userId}";
    string patchJson = $"{{\"username\":\"{EscapeJson(newUsername)}\",\"level\":1,\"coins\":0}}";

    using (UnityWebRequest req = new UnityWebRequest(patchUrl, "PATCH"))
    {
        byte[] body = Encoding.UTF8.GetBytes(patchJson);
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("apikey",        supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.SetRequestHeader("Prefer",        "return=representation");

        yield return req.SendWebRequest();

        string patchResponse = req.downloadHandler.text;
        Debug.Log($"[Google] PATCH response: {patchResponse} | Code: {req.responseCode}");

        // Si PATCH actualizó algo retorna el row
        if ((req.result == UnityWebRequest.Result.Success || req.responseCode == 200)
            && patchResponse.Trim() != "[]" && !string.IsNullOrEmpty(patchResponse.Trim()))
        {
            AccessToken = accessToken;
            UserId      = userId;
            Username    = newUsername;
            Level       = 1;
            Coins       = 0;

            Debug.Log($"[Google] Perfil actualizado → {Username}");
            if (uiLogin != null) uiLogin.OnLoginSuccess();
            yield break;
        }
    }

    // Si PATCH no encontró nada, hacer POST insertar nuevo
    string insertUrl  = $"{supabaseUrl}/rest/v1/players";
    string insertJson = $"{{\"id\":\"{userId}\",\"username\":\"{EscapeJson(newUsername)}\",\"level\":1,\"coins\":0}}";

    using (UnityWebRequest req = new UnityWebRequest(insertUrl, "POST"))
    {
        byte[] body = Encoding.UTF8.GetBytes(insertJson);
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("apikey",        supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.SetRequestHeader("Prefer",        "return=representation");

        yield return req.SendWebRequest();

        string insertResponse = req.downloadHandler.text;
        Debug.Log($"[Google] INSERT response: {insertResponse} | Code: {req.responseCode}");

        if (req.result == UnityWebRequest.Result.Success || req.responseCode == 201)
        {
            AccessToken = accessToken;
            UserId      = userId;
            Username    = newUsername;
            Level       = 1;
            Coins       = 0;

            Debug.Log($"[Google] Perfil creado → {Username}");
            if (uiLogin != null) uiLogin.OnLoginSuccess();
        }
        else
        {
            Debug.LogError($"[Google] Error creando perfil: {insertResponse}");
            if (uiLogin != null) uiLogin.OnLoginFailed("No se pudo crear el perfil.");
        }
    }
}


    
    //  CERRAR SESIÓN
   
    public void Logout()
    {
        _googleListener?.Stop();
        AccessToken = null;
        UserId      = null;
        Username    = null;
        Level       = 0;
        Coins       = 0;
        Debug.Log("[Auth] Sesión cerrada.");
    }


   

  private string ExtractJsonField(string json, string field)
{
    if (string.IsNullOrEmpty(json)) return null;

    string working = json.Trim();
    if (working.StartsWith("["))
    {
        int firstObj = working.IndexOf('{');
        int lastObj  = working.LastIndexOf('}');
        if (firstObj < 0 || lastObj < 0) return null;
        working = working.Substring(firstObj, lastObj - firstObj + 1);
    }

    string keyWithQuotes = $"\"{field}\":\"";
    int start = working.IndexOf(keyWithQuotes, StringComparison.Ordinal);
    if (start >= 0)
    {
        start += keyWithQuotes.Length;
        int end = working.IndexOf('"', start);
        return end < 0 ? null : working.Substring(start, end - start);
    }

    string keyNoQuotes = $"\"{field}\":";
    start = working.IndexOf(keyNoQuotes, StringComparison.Ordinal);
    if (start >= 0)
    {
        start += keyNoQuotes.Length;
        int end = working.IndexOfAny(new char[] { ',', '}', ']' }, start);
        return end < 0 ? null : working.Substring(start, end - start).Trim();
    }

    return null;
}

private string ExtractNestedField(string json, string parent, string field)
{
    string parentKey = $"\"{parent}\":{{";
    int parentStart  = json.IndexOf(parentKey, StringComparison.Ordinal);
    if (parentStart < 0) return null;

    int objStart = parentStart + parentKey.Length - 1;
    int depth    = 1;
    int i        = objStart + 1;
    while (i < json.Length && depth > 0)
    {
        if (json[i] == '{') depth++;
        else if (json[i] == '}') depth--;
        i++;
    }
    string nested = json.Substring(objStart, i - objStart);
    return ExtractJsonField(nested, field);
}

private string EscapeJson(string value)
    => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}