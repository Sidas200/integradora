using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// Lo que falta enviar a la API. Se guarda en disco: sin red, al cerrar el juego o con la sesión
// vencida nada se pierde y se manda en cuanto se pueda (las sesiones y eventos traen su id, así que
// reenviarlos no duplica nada).
public class CloudOutboxState
{
    public string PlayerId;
    // Última versión de la ranura que confirmó el servidor (bloqueo optimista entre dispositivos).
    public int SaveVersion;
    public bool SavePending;
    public List<CloudPlaySession> Sessions = new List<CloudPlaySession>();
    public List<CloudEvent> Events = new List<CloudEvent>();
    public List<CloudPendingAchievement> AchievementsPending = new List<CloudPendingAchievement>();
    public List<string> AchievementsSent = new List<string>();
    public DateTime? LastSyncUtc;

    [JsonIgnore]
    public bool HasPending => SavePending || Sessions.Count > 0 || Events.Count > 0 || AchievementsPending.Count > 0;
}

public class CloudPendingAchievement
{
    public string Id;
    public DateTime UnlockedAt;
}

public class CloudOutbox
{
    // Topes para que un jugador sin cuenta durante mucho tiempo no llene el disco.
    public const int MaxSessions = 200;
    public const int MaxEvents = 2000;

    private readonly string path;

    public CloudOutboxState State { get; private set; } = new CloudOutboxState();

    public CloudOutbox() : this(Path.Combine(Application.persistentDataPath, "cloud_outbox.json")) { }

    public CloudOutbox(string path)
    {
        this.path = path;
    }

    public void Load()
    {
        if (!File.Exists(path)) return;
        try
        {
            State = CloudJson.Read<CloudOutboxState>(File.ReadAllText(path)) ?? new CloudOutboxState();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] Cola de envío dañada; se empieza vacía. ({e.Message})");
            State = new CloudOutboxState();
        }
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, CloudJson.Write(State));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] No se pudo guardar la cola de envío: {e.Message}");
        }
    }

    public void AddSession(CloudPlaySession session)
    {
        State.Sessions.Add(session);
        Trim(State.Sessions, MaxSessions);
    }

    public void AddEvent(CloudEvent e)
    {
        State.Events.Add(e);
        Trim(State.Events, MaxEvents);
    }

    // true si es nuevo (no estaba enviado ni pendiente).
    public bool AddAchievement(string id, DateTime unlockedAt)
    {
        if (State.AchievementsSent.Contains(id) || State.AchievementsPending.Exists(a => a.Id == id)) return false;
        State.AchievementsPending.Add(new CloudPendingAchievement { Id = id, UnlockedAt = unlockedAt });
        return true;
    }

    // Otra cuenta en este equipo: la versión y los logros enviados eran de la anterior.
    public void BindPlayer(string playerId)
    {
        if (State.PlayerId == playerId) return;
        bool otherAccount = State.PlayerId != null;
        State.PlayerId = playerId;
        State.SaveVersion = 0;
        State.AchievementsSent.Clear();
        if (otherAccount)
        {
            // Lo jugado sin sesión (PlayerId null) sí se asigna a la cuenta que entra.
            State.Sessions.Clear();
            State.Events.Clear();
            State.AchievementsPending.Clear();
        }
    }

    private static void Trim<T>(List<T> list, int max)
    {
        if (list.Count > max) list.RemoveRange(0, list.Count - max);
    }
}
