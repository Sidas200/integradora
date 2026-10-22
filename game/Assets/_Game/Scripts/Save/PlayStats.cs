using System;
using System.Collections.Generic;
using UnityEngine;

// Estadísticas de la partida (tiempo jugado, enemigos, muertes, materiales, crafteos) e hitos de historia.
// Los contadores viven en SessionState y se guardan con la partida. Cada registro dispara Recorded,
// que CloudSync convierte en evento de telemetría; sin sesión iniciada solo se acumulan en local.
public static class PlayStats
{
    // Contrato con la API (story_milestones): cada hito es un StoryFlag del capítulo.
    public static readonly string[] MilestoneIds =
    {
        "woke_up", "has_sword", "met_researcher", "sanctuary_unlocked",
        "maiden_awakened", "sword_upgraded", "seal_opened", "permit_obtained"
    };

    // Tipos de evento del contrato (telemetry_events.type).
    public const string EnemyDefeatedType = "ENEMY_DEFEATED";
    public const string BossDefeatedType = "BOSS_DEFEATED";
    public const string DeathType = "DEATH";
    public const string PickupType = "PICKUP";
    public const string CraftType = "CRAFT";
    public const string MilestoneType = "MILESTONE_COMPLETED";

    public static event Action<PlayEvent> Recorded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        // Con "Enter Play Mode" sin recarga de dominio los estáticos sobreviven: se limpian aquí.
        Recorded = null;
        StoryFlags.Changed -= OnFlagSet;
        StoryFlags.Changed += OnFlagSet;
    }

    public static void AddPlayTime(float seconds)
    {
        var s = SessionState.Instance;
        if (s != null && seconds > 0f) s.playTimeSec += seconds;
    }

    public static void EnemyDefeated(string enemyId)
    {
        var s = SessionState.Instance;
        if (s == null) return;
        s.enemiesDefeated++;
        Raise(EnemyDefeatedType, ("enemyId", enemyId), ("total", s.enemiesDefeated));
    }

    public static void BossDefeated(string bossId)
    {
        if (SessionState.Instance == null) return;
        Raise(BossDefeatedType, ("bossId", bossId));
    }

    public static void PlayerDied(string scene)
    {
        var s = SessionState.Instance;
        if (s == null) return;
        s.deaths++;
        Raise(DeathType, ("scene", scene), ("total", s.deaths));
    }

    public static void MaterialCollected(string itemId, int amount, string pickupId)
    {
        var s = SessionState.Instance;
        if (s == null || amount <= 0) return;
        s.materialsCollected += amount;
        Raise(PickupType, ("itemId", itemId), ("quantity", amount), ("pickupId", pickupId));
    }

    public static void ItemCrafted(string recipeId)
    {
        var s = SessionState.Instance;
        if (s == null) return;
        s.itemsCrafted++;
        Raise(CraftType, ("recipeId", recipeId), ("total", s.itemsCrafted));
    }

    public static bool IsMilestone(string flag) => Array.IndexOf(MilestoneIds, flag) >= 0;

    private static void OnFlagSet(string flag)
    {
        var s = SessionState.Instance;
        if (s == null || !IsMilestone(flag) || s.milestones.Exists(m => m.id == flag)) return;
        int playTime = Mathf.FloorToInt(s.playTimeSec);
        s.milestones.Add(new MilestoneRecord { id = flag, playTimeSec = playTime, completedAtUtc = DateTime.UtcNow.ToString("o") });
        Raise(MilestoneType, ("milestoneId", flag), ("playTimeSec", playTime));
    }

    private static void Raise(string type, params (string key, object value)[] data)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (key, value) in data)
        {
            if (value != null) dict[key] = value;
        }
        Recorded?.Invoke(new PlayEvent(type, dict, DateTime.UtcNow));
    }
}

public readonly struct PlayEvent
{
    public readonly string Type;
    public readonly Dictionary<string, object> Data;
    public readonly DateTime OccurredAtUtc;

    public PlayEvent(string type, Dictionary<string, object> data, DateTime occurredAtUtc)
    {
        Type = type;
        Data = data;
        OccurredAtUtc = occurredAtUtc;
    }
}

// Un hito de historia completado, con el tiempo jugado en ese momento.
[Serializable]
public class MilestoneRecord
{
    public string id;
    public int playTimeSec;
    public string completedAtUtc;
}
