using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Traduce el guardado local (SaveData) al documento de la API (CloudSaveData) y de vuelta.
// Los PersistentId usan "/" ("02_OpenField/Pickup_Flor_A") y la API solo acepta [A-Za-z0-9_.:-]:
// se cambia por "." (ningún id del juego lo usa; ":" sí, en los drops "...:drop0").
public static class CloudSaveMapper
{
    public const string DefeatedState = "DEFEATED";
    private const int SceneMaxLength = 80;

    public static string EncodeId(string id) => id?.Replace('/', '.');

    public static string DecodeId(string id) => id?.Replace('.', '/');

    public static CloudSaveData ToCloud(SaveData local, int version)
    {
        var cloud = new CloudSaveData
        {
            Version = version,
            ActiveCharacter = string.IsNullOrEmpty(local.match.activeCharacter) ? "TSUKI" : local.match.activeCharacter.ToUpperInvariant(),
            CurrentChapter = Math.Max(0, local.match.currentChapter),
            Health = local.match.health >= 0 ? local.match.health : (int?)null
        };

        string scene = local.checkpoint.scene;
        if (!string.IsNullOrEmpty(scene) && scene.Length <= SceneMaxLength)
        {
            cloud.CurrentScene = scene;
            cloud.Position = new CloudPosition { X = local.checkpoint.posX, Y = local.checkpoint.posY };
        }

        var stats = local.stats ?? new StatsData();
        cloud.Stats = new CloudStats
        {
            PlayTimeSec = Math.Max(0, stats.playTimeSec),
            EnemiesDefeated = Math.Max(0, stats.enemiesDefeated),
            Deaths = Math.Max(0, stats.deaths),
            MaterialsCollected = Math.Max(0, stats.materialsCollected),
            ItemsCrafted = Math.Max(0, stats.itemsCrafted)
        };

        // Un id por item (la API lo exige); cantidad entre 1 y 999.
        foreach (var group in local.inventory.Where(e => e.amount > 0 && !string.IsNullOrEmpty(e.materialId)).GroupBy(e => e.materialId))
            cloud.Inventory.Add(new CloudInventoryEntry { ItemId = group.Key, Quantity = Math.Min(999, group.Sum(e => e.amount)) });

        var p = local.progress;
        cloud.CollectedPickups = (p.collected ?? new List<string>()).Select(EncodeId).Distinct().ToList();
        cloud.Objects = (p.defeated ?? new List<string>()).Distinct()
            .Select(id => new CloudObjectState { ObjectId = EncodeId(id), State = DefeatedState }).ToList();

        var completed = new HashSet<string>();
        foreach (var m in p.milestones ?? new List<MilestoneRecord>())
        {
            if (!PlayStats.IsMilestone(m.id) || !completed.Add(m.id)) continue;
            cloud.CompletedMilestones.Add(new CloudMilestone
            {
                MilestoneId = m.id,
                PlayTimeSec = m.playTimeSec >= 0 ? m.playTimeSec : (int?)null,
                CompletedAt = ParseUtc(m.completedAtUtc) ?? ParseUtc(local.savedAtUtc) ?? DateTime.UtcNow
            });
        }
        // Guardados anteriores a PlayStats: el flag existe pero no su registro con fecha.
        foreach (var flag in p.flags ?? new List<string>())
        {
            if (!PlayStats.IsMilestone(flag) || !completed.Add(flag)) continue;
            cloud.CompletedMilestones.Add(new CloudMilestone
            {
                MilestoneId = flag,
                CompletedAt = ParseUtc(local.savedAtUtc) ?? DateTime.UtcNow
            });
        }

        cloud.ExtraData = new CloudExtraData
        {
            SavedAtUtc = local.savedAtUtc,
            SafePointId = p.safePointId,
            SealOpened = p.sealOpened,
            GuardianPermit = p.guardianPermit,
            SwordLevel = p.swordLevel,
            Flags = new List<string>(p.flags ?? new List<string>()),
            Equipped = new List<string>(p.equipped ?? new List<string>())
        };
        return cloud;
    }

    public static SaveData ToLocal(CloudSaveData cloud, int slot = 1)
    {
        var extra = cloud.ExtraData ?? new CloudExtraData();
        var local = new SaveData
        {
            savedAtUtc = extra.SavedAtUtc ?? DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };

        local.match.slot = slot;
        local.match.activeCharacter = TitleCase(cloud.ActiveCharacter ?? "TSUKI");
        local.match.currentChapter = cloud.CurrentChapter;
        local.match.health = cloud.Health ?? -1;

        local.checkpoint.scene = cloud.CurrentScene ?? "";
        local.checkpoint.posX = cloud.Position?.X ?? 0f;
        local.checkpoint.posY = cloud.Position?.Y ?? 0f;

        var stats = cloud.Stats ?? new CloudStats();
        local.stats = new StatsData
        {
            playTimeSec = stats.PlayTimeSec,
            enemiesDefeated = stats.EnemiesDefeated,
            deaths = stats.Deaths,
            materialsCollected = stats.MaterialsCollected,
            itemsCrafted = stats.ItemsCrafted
        };

        foreach (var e in cloud.Inventory ?? new List<CloudInventoryEntry>())
            local.inventory.Add(new InventoryEntry { materialId = e.ItemId, amount = e.Quantity });

        var p = local.progress;
        p.safePointId = extra.SafePointId ?? "";
        p.sealOpened = extra.SealOpened;
        p.guardianPermit = extra.GuardianPermit;
        p.swordLevel = Math.Max(0, extra.SwordLevel);
        p.flags = new List<string>(extra.Flags ?? new List<string>());
        p.equipped = new List<string>(extra.Equipped ?? new List<string>());
        p.collected = (cloud.CollectedPickups ?? new List<string>()).Select(DecodeId).ToList();
        p.defeated = (cloud.Objects ?? new List<CloudObjectState>())
            .Where(o => o.State == DefeatedState).Select(o => DecodeId(o.ObjectId)).ToList();
        p.milestones = (cloud.CompletedMilestones ?? new List<CloudMilestone>())
            .Select(m => new MilestoneRecord
            {
                id = m.MilestoneId,
                playTimeSec = m.PlayTimeSec ?? -1,
                completedAtUtc = DateTime.SpecifyKind(m.CompletedAt, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture)
            }).ToList();
        return local;
    }

    // true si "a" se guardó después que "b" (comparando savedAtUtc). Sin fecha cuenta como más viejo.
    public static bool IsNewer(string a, string b)
    {
        var da = ParseUtc(a);
        var db = ParseUtc(b);
        if (da == null) return false;
        if (db == null) return true;
        return da.Value > db.Value;
    }

    public static DateTime? ParseUtc(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d)
            ? d
            : (DateTime?)null;
    }

    private static string TitleCase(string upper) =>
        upper.Length == 0 ? upper : upper.Substring(0, 1) + upper.Substring(1).ToLowerInvariant();
}
