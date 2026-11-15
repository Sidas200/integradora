using System;
using UnityEngine;

public enum CloudSessionState
{
    SignedOut,
    Restoring,
    SignedIn
}

// Sesión del jugador en la API: login, renovación del access token y logout.
// El access token vive solo en memoria; el refresh token, cifrado en disco (ICloudTokenStore).
public class CloudSession
{
    // Se renueva un poco antes de que venza, para no gastar una petición en un 401.
    private const float RefreshMarginSeconds = 30f;

    private readonly CloudApiClient api;
    private readonly ICloudTokenStore store;

    private string accessToken;
    private float accessExpiresAt;

    public CloudSessionState State { get; private set; } = CloudSessionState.SignedOut;
    public CloudPlayer Player { get; private set; }
    public bool IsSignedIn => State == CloudSessionState.SignedIn;

    public event Action Changed;

    public CloudSession(CloudApiClient api, ICloudTokenStore store)
    {
        this.api = api;
        this.store = store;
        HasStoredSession = !string.IsNullOrEmpty(store.Load());
    }

    // Hay un refresh token guardado (aunque ahora no haya red para usarlo).
    public bool HasStoredSession { get; private set; }

    // Devuelve null si entró; si no, el mensaje para el jugador.
    public async Awaitable<string> Login(string email, string password)
    {
        var response = await api.Send("POST", "/auth/login",
            new CloudLoginRequest { Email = email.Trim(), Password = password });
        if (response.Ok)
        {
            Accept(response.As<CloudTokenResponse>());
            return null;
        }
        if (response.NetworkError) return "No hay conexión con el servidor. Intenta más tarde.";
        switch (response.Problem?.Code)
        {
            case "INVALID_CREDENTIALS": return "Correo o contraseña incorrectos.";
            case "VALIDATION_FAILED": return "Revisa el correo y la contraseña.";
            case "TOO_MANY_ATTEMPTS": return "Demasiados intentos. Espera un momento.";
            default: return $"No se pudo iniciar sesión (error {response.Status}).";
        }
    }

    // Al abrir el juego: si hay un refresh token guardado, recupera la sesión sin pedir contraseña.
    // Sin red la sesión queda pendiente (no se borra el token) y se reintenta en la siguiente petición.
    public async Awaitable<bool> Restore()
    {
        if (!HasStoredSession) return false;
        SetState(CloudSessionState.Restoring);
        bool ok = await Refresh();
        if (!ok && State == CloudSessionState.Restoring) SetState(CloudSessionState.SignedOut);
        return ok;
    }

    public async Awaitable Logout()
    {
        string refresh = store.Load();
        store.Clear();
        HasStoredSession = false;
        accessToken = null;
        Player = null;
        SetState(CloudSessionState.SignedOut);
        if (!string.IsNullOrEmpty(refresh))
            await api.Send("POST", "/auth/logout", new CloudRefreshRequest { RefreshToken = refresh });
    }

    // Petición autenticada. Renueva el token si va a vencer y reintenta una vez ante TOKEN_EXPIRED.
    public async Awaitable<CloudResponse> Send(string method, string path, object body = null)
    {
        if (string.IsNullOrEmpty(accessToken) || Time.realtimeSinceStartup >= accessExpiresAt - RefreshMarginSeconds)
        {
            // Si la sesión sigue viva, la renovación falló por red: se reintenta después.
            if (!await Refresh()) return IsSignedIn ? new CloudResponse { NetworkError = true } : new CloudResponse { Status = 401 };
        }

        var response = await api.Send(method, path, body, accessToken);
        if (response.Status == 401 && await Refresh())
            response = await api.Send(method, path, body, accessToken);
        return response;
    }

    private async Awaitable<bool> Refresh()
    {
        string refresh = store.Load();
        if (string.IsNullOrEmpty(refresh))
        {
            SignOutLocally();
            return false;
        }

        var response = await api.Send("POST", "/auth/refresh", new CloudRefreshRequest { RefreshToken = refresh });
        if (response.Ok)
        {
            Accept(response.As<CloudTokenResponse>());
            return true;
        }
        if (response.Status == 401)
        {
            // Token vencido, revocado o reusado: hay que volver a iniciar sesión.
            Debug.Log("[Cloud] La sesión expiró; hay que iniciar sesión otra vez.");
            SignOutLocally();
        }
        return false;
    }

    private void Accept(CloudTokenResponse tokens)
    {
        accessToken = tokens.AccessToken;
        accessExpiresAt = Time.realtimeSinceStartup + Mathf.Max(60, tokens.ExpiresInSec);
        // El refresh token rota en cada uso: hay que guardar el nuevo siempre.
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            store.Save(tokens.RefreshToken);
            HasStoredSession = true;
        }
        Player = tokens.Player;
        SetState(CloudSessionState.SignedIn);
    }

    private void SignOutLocally()
    {
        store.Clear();
        HasStoredSession = false;
        accessToken = null;
        Player = null;
        SetState(CloudSessionState.SignedOut);
    }

    private void SetState(CloudSessionState state)
    {
        if (State == state) return;
        State = state;
        Changed?.Invoke();
    }
}
