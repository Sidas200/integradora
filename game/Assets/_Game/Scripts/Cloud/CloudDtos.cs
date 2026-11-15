using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// DTOs del contrato docs/api/openapi.yaml (integradora-web). JSON en camelCase, fechas ISO-8601 en UTC.
public static class CloudJson
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        // Sin esto un string con forma de fecha (savedAtUtc) se reescribe en otro formato al leerlo.
        DateParseHandling = DateParseHandling.None,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public static string Write(object value) => JsonConvert.SerializeObject(value, Settings);

    public static T Read<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
}

public class CloudLoginRequest
{
    public string Email;
    public string Password;
    public string Client = "UNITY_PC";
}

public class CloudRefreshRequest
{
    public string RefreshToken;
}

public class CloudPlayer
{
    public string Id;
    public string Username;
    public string Email;
}

public class CloudTokenResponse
{
    public string AccessToken;
    public int ExpiresInSec;
    public string RefreshToken;
    public CloudPlayer Player;
}

public class CloudProblem
{
    public string Code;
    public string Detail;
    public int? ServerVersion;
}

// PUT/GET /account/saves/{slot}/data
public class CloudSaveData
{
    public int Version;
    public string ActiveCharacter;
    public int CurrentChapter;
    public string CurrentScene;
    public CloudPosition Position;
    public int? Health;
    public CloudStats Stats = new CloudStats();
    public List<CloudInventoryEntry> Inventory = new List<CloudInventoryEntry>();
    public List<string> CollectedPickups = new List<string>();
    public List<CloudObjectState> Objects = new List<CloudObjectState>();
    public List<CloudMilestone> CompletedMilestones = new List<CloudMilestone>();
    public CloudExtraData ExtraData = new CloudExtraData();
}

public class CloudPosition
{
    public float X;
    public float Y;
}

public class CloudStats
{
    public int PlayTimeSec;
    public int EnemiesDefeated;
    public int Deaths;
    public int MaterialsCollected;
    public int ItemsCrafted;
}

public class CloudInventoryEntry
{
    public string ItemId;
    public int Quantity;
}

public class CloudObjectState
{
    public string ObjectId;
    public string State;
}

public class CloudMilestone
{
    public string MilestoneId;
    public int? PlayTimeSec;
    public DateTime CompletedAt;
}

// extraData: solo lo que no tiene tabla propia. "v" es la versión de este formato.
public class CloudExtraData
{
    public int V = 1;
    public string SavedAtUtc;
    public string SafePointId;
    public bool SealOpened;
    public bool GuardianPermit;
    public int SwordLevel;
    public List<string> Flags = new List<string>();
    public List<string> Equipped = new List<string>();
}

public class CloudSaveAccepted
{
    public int Version;
    public DateTime UpdatedAt;
}

// POST /account/saves/{slot}/sessions (id generado por el juego: reenviar no duplica)
public class CloudPlaySession
{
    public string Id;
    public DateTime StartedAt;
    public DateTime EndedAt;
    public int DurationSec;
    public string Platform;
    public string GameVersion;
}

// POST /account/saves/{slot}/events
public class CloudEvent
{
    public string Id;
    public string Type;
    public Dictionary<string, object> Data;
    public DateTime OccurredAt;
}

public class CloudBatchResult
{
    public int Accepted;
    public int Duplicates;
}

public class CloudUnlockRequest
{
    public DateTime UnlockedAt;
    public int? Slot;
}
