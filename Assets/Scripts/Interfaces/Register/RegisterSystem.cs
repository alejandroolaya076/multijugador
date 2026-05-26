using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


//  Conecta Unity con Supabase Auth y tabla player
public class RegisterSystem : MonoBehaviour
{
    [Header("Supabase Config")]
    public string supabaseUrl = "https://ihwughhiqdoiwkctbdcr.supabase.co";
    public string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imlod3VnaGhpcWRvaXdrY3RiZGNyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI3ODY2MTUsImV4cCI6MjA4ODM2MjYxNX0.dtKkp9l2G1hG4cLKYADZr93c0ZHKbjbgLVfyibXYkCg";

    // TEST DE CONEXIÓN al arrancar la escena
    private void Start()
    {
        StartCoroutine(TestConnection());
        StartCoroutine(PingGoogle());
    }
IEnumerator PingGoogle()
{
    using UnityWebRequest req = UnityWebRequest.Get("https://www.google.com");
    yield return req.SendWebRequest();
    Debug.Log(req.result == UnityWebRequest.Result.Success 
        ? "✅ Internet OK" 
        : $"❌ Sin internet: {req.error}");
}
    private IEnumerator TestConnection()
    {
        string url = $"{supabaseUrl}/rest/v1/players?limit=1";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", supabaseKey);
        request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success ||
            request.responseCode == 200 || request.responseCode == 406)
        {
            Debug.Log("[Supabase] Conectado correctamente.");
        }
        else
        {
            Debug.LogError($"[Supabase] Error de conexión: {request.error} (HTTP {request.responseCode})");
        }
    }

    //  REGISTRO PÚBLICO
    
    public void Register(string username, string email, string password, string confirmPassword)
    {
        if (!ValidateFields(username, email, password, confirmPassword))
            return;

        StartCoroutine(RegisterCoroutine(username, email, password));
    }

    //  VALIDACIONES

    private bool ValidateFields(string username, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning(" El nombre de usuario está vacío.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            Debug.LogWarning(" Email inválido.");
            return false;
        }
        // Supabase requiere mínimo 6 caracteres por defecto
        
        if (password.Length < 6)
        {
            Debug.LogWarning(" La contraseña debe tener al menos 6 caracteres.");
            return false;
        }
        if (password != confirmPassword)
        {
            Debug.LogWarning(" Las contraseñas no coinciden.");
            return false;
        }
        return true;
    }

    //  COROUTINE PRINCIPAL DE REGISTRO

    private IEnumerator RegisterCoroutine(string username, string email, string password)
    {
        // 1: Crear usuario en Supabase Auth
        Debug.Log("[Auth] Creando usuario...");

        string authUrl = $"{supabaseUrl}/auth/v1/signup";

   
        // FIX 2: Incluir username en data{} para que el trigger pueda usarlo
        string authJson = $"{{" +
                          $"\"email\":\"{EscapeJson(email)}\"," +
                          $"\"password\":\"{EscapeJson(password)}\"," +
                          $"\"data\":{{\"username\":\"{EscapeJson(username)}\"}}" +
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

            try
            {
                // FIX 3: Mostrar respuesta completa para debug 
                string response = authReq.downloadHandler.text;
                Debug.Log($"[Auth] Respuesta raw: {response}");

                if (authReq.result != UnityWebRequest.Result.Success)
                    throw new Exception($"HTTP {authReq.responseCode} – {authReq.error}\nDetalle: {response}");

                userId      = ExtractJsonField(response, "id");
                accessToken = ExtractJsonField(response, "access_token");

                if (string.IsNullOrEmpty(accessToken))
                {
                    Debug.LogWarning("[Auth] No se recibió access_token.");
                    Debug.LogWarning("→ Ve a Supabase → Authentication → Providers → Email → desactiva 'Confirm email'");
                    throw new Exception("Sin access_token no se puede insertar el perfil.");
                }

                if (string.IsNullOrEmpty(userId))
                    throw new Exception("No se pudo obtener el ID del usuario en la respuesta.");

                Debug.Log($"[Auth] Usuario creado. ID: {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($" [Auth] Fallo al registrar: {ex.Message}");
                yield break;
            }
        }

        //  Insertar fila en la tabla players
        Debug.Log("[DB] Insertando perfil en 'players'...");

        string dbUrl  = $"{supabaseUrl}/rest/v1/players";
        string dbJson = $"{{" +
                        $"\"id\":\"{userId}\"," +
                        $"\"username\":\"{EscapeJson(username)}\"," +
                        $"\"level\":1," +
                        $"\"coins\":0" +
                        $"}}";

        Debug.Log($"[DB] JSON a insertar: {dbJson}");

        using (UnityWebRequest dbReq = new UnityWebRequest(dbUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(dbJson);
            dbReq.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            dbReq.downloadHandler = new DownloadHandlerBuffer();
            dbReq.SetRequestHeader("Content-Type",  "application/json");
            dbReq.SetRequestHeader("apikey",        supabaseKey);
            dbReq.SetRequestHeader("Authorization", $"Bearer {accessToken}");
          
            // si el trigger ya insertó la fila automáticamente
            dbReq.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

            yield return dbReq.SendWebRequest();

            string dbResponse = dbReq.downloadHandler.text;
            Debug.Log($"[DB] Respuesta raw: {dbResponse}");

            try
            {
              
                if (dbReq.result != UnityWebRequest.Result.Success &&
                    dbReq.responseCode != 201 &&
                    dbReq.responseCode != 200)
                    throw new Exception($"HTTP {dbReq.responseCode} – {dbReq.error}\n{dbResponse}");

                Debug.Log($" [DB] Perfil insertado: {dbResponse}");
                Debug.Log($" Registro completo para '{username}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($" [DB] Fallo al insertar perfil: {ex.Message}");
            }
        }
    }

    
    //HELPERS
    
    private string ExtractJsonField(string json, string field)
    {
        string key = $"\"{field}\":\"";
        int start  = json.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOf('"', start);
        return end < 0 ? null : json.Substring(start, end - start);
    }

    private string EscapeJson(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}