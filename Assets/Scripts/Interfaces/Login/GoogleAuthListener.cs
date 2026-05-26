using System;
using System.Net;
using System.Threading;
using UnityEngine;

public class GoogleAuthListener
{
    private HttpListener _listener;
    private Thread _thread;

    public Action<string, string> OnTokenReceived;
    public Action<string> OnError;

    private const int PORT = 54321;
    public const string REDIRECT_URI = "http://localhost:54321/auth/callback";

    public void StartListening()
    {
        _listener = new HttpListener();

        // ✅ FIX: agregar AMBOS prefijos (con y sin slash final)
        _listener.Prefixes.Add($"http://localhost:{PORT}/auth/callback/");
        _listener.Prefixes.Add($"http://localhost:{PORT}/auth/");
        _listener.Start();

        _thread = new Thread(() =>
        {
            try
            {
                Debug.Log("[Google] Listener esperando conexión...");

                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;

                Debug.Log($"[Google] Petición recibida: {request.Url}");

                // ✅ Responder al navegador PRIMERO
                string htmlResponse = @"
                <html>
                <head><title>Eclipsera</title></head>
                <body style='font-family:sans-serif;text-align:center;padding:60px;
                            background:#0a0a1a;color:white;'>
                <h1 style='color:#00ff88;'>Autenticacion exitosa!</h1>
                <p>Volviendo a <strong>Eclipsera</strong>...</p>
                <script>setTimeout(function() { window.close(); }, 2000);</script>
                </body></html>";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlResponse);
                context.Response.ContentType     = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                // ✅ Extraer token del query string
                string query        = request.Url.Query;
                Debug.Log($"[Google] Query recibida: {query}");

                string accessToken  = ExtractParam(query, "access_token");
                string refreshToken = ExtractParam(query, "refresh_token");

                Debug.Log($"[Google] access_token={accessToken}");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    OnTokenReceived?.Invoke(accessToken, refreshToken);
                }
                else
                {
                    // ✅ FIX: a veces el token viene en el path, no en el query
                    string fullUrl = request.Url.ToString();
                    Debug.LogWarning($"[Google] Token no encontrado en query. URL completa: {fullUrl}");
                    OnError?.Invoke("No se recibió el access_token en el callback.");
                }
            }
            catch (HttpListenerException ex)
            {
                // Se dispara al llamar Stop() — ignorar
                Debug.Log($"[Google] Listener detenido: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Google] Error en listener: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                Stop();
            }
        });

        _thread.IsBackground = true;
        _thread.Start();
        Debug.Log($"[Google] Escuchando en {REDIRECT_URI}");
    }

    public void Stop()
    {
        try { _listener?.Stop(); } catch { }
    }

    private string ExtractParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return null;
        query = query.TrimStart('?');
        foreach (string part in query.Split('&'))
        {
            string[] kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}