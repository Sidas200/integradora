using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudResponse
{
    public long Status;
    public string Body;
    // Sin red, DNS o timeout: no hay respuesta del servidor (se reintenta más tarde).
    public bool NetworkError;

    public bool Ok => !NetworkError && Status >= 200 && Status < 300;

    public CloudProblem Problem
    {
        get
        {
            if (string.IsNullOrEmpty(Body) || Ok) return null;
            try { return CloudJson.Read<CloudProblem>(Body); }
            catch { return null; }
        }
    }

    public T As<T>() => CloudJson.Read<T>(Body);
}

// HTTP con UnityWebRequest. No sabe de tokens: CloudSession agrega el Bearer y renueva.
public class CloudApiClient
{
    private readonly string apiRoot;
    private readonly int timeoutSeconds;

    // Render (plan gratis) tarda hasta ~1 min en despertar el servicio.
    public CloudApiClient(string apiRoot, int timeoutSeconds = 75)
    {
        this.apiRoot = apiRoot.TrimEnd('/');
        this.timeoutSeconds = timeoutSeconds;
    }

    public string ApiRoot => apiRoot;

    public async Awaitable<CloudResponse> Send(string method, string path, object body = null, string bearer = null)
    {
        using var request = new UnityWebRequest(apiRoot + path, method)
        {
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = timeoutSeconds
        };
        if (body != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(CloudJson.Write(body)));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        request.SetRequestHeader("Accept", "application/json");
        if (!string.IsNullOrEmpty(bearer)) request.SetRequestHeader("Authorization", "Bearer " + bearer);

        await request.SendWebRequest();

        return new CloudResponse
        {
            Status = request.responseCode,
            Body = request.downloadHandler?.text,
            NetworkError = request.result == UnityWebRequest.Result.ConnectionError
                           || request.result == UnityWebRequest.Result.DataProcessingError
                           || request.responseCode == 0
        };
    }
}
