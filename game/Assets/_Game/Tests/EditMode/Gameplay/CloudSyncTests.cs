using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

// Pruebas EditMode de la sincronización con la nube (sin red): traducción del guardado al
// contrato de la API, ids persistentes, fechas, configuración, token cifrado y cola de envío.
public class CloudSyncTests
{
    private string dir;

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "hex_cloud_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    private static SaveData CreateSample()
    {
        var data = new SaveData { savedAtUtc = "2026-10-21T10:30:00.0000000Z" };
        data.match.health = 70;
        data.checkpoint.scene = "Assets/_Game/Scenes/Chapter1/02_OpenField.unity";
        data.checkpoint.posX = 1.5f;
        data.checkpoint.posY = -2f;
        data.progress.safePointId = "base";
        data.progress.sealOpened = true;
        data.progress.swordLevel = 1;
        data.progress.flags.AddRange(new[] { "woke_up", "has_sword", "camp_briefing" });
        data.progress.collected.AddRange(new[] { "02_OpenField/Pickup_Flor_A", "02_OpenField/Enemy_Slime_A:drop0" });
        data.progress.defeated.Add("02_OpenField/Enemy_Slime_A");
        data.progress.equipped.Add("casco_cuero");
        data.progress.milestones.Add(new MilestoneRecord { id = "woke_up", playTimeSec = 30, completedAtUtc = "2026-10-21T10:00:00Z" });
        data.inventory.Add(new InventoryEntry { materialId = "cuero_antiguo", amount = 3 });
        data.inventory.Add(new InventoryEntry { materialId = "flor_destello", amount = 1 });
        data.stats = new StatsData { playTimeSec = 1800, enemiesDefeated = 12, deaths = 1, materialsCollected = 9, itemsCrafted = 2 };
        return data;
    }

    [Test]
    public void ToCloud_FollowsTheApiContract()
    {
        var cloud = CloudSaveMapper.ToCloud(CreateSample(), 4);

        Assert.AreEqual(4, cloud.Version);
        Assert.AreEqual("TSUKI", cloud.ActiveCharacter);
        Assert.AreEqual(70, cloud.Health);
        Assert.AreEqual(1.5f, cloud.Position.X);
        Assert.AreEqual(1800, cloud.Stats.PlayTimeSec);
        Assert.AreEqual(1, cloud.ExtraData.V);
        // La API solo acepta [A-Za-z0-9_.:-] en los ids del mundo
        foreach (var id in cloud.CollectedPickups.Concat(cloud.Objects.Select(o => o.ObjectId)))
            StringAssert.IsMatch("^[A-Za-z0-9_.:-]{1,100}$", id);
        Assert.AreEqual("02_OpenField.Enemy_Slime_A", cloud.Objects[0].ObjectId);
        Assert.AreEqual(CloudSaveMapper.DefeatedState, cloud.Objects[0].State);
    }

    [Test]
    public void ToCloud_OnlySendsCatalogMilestones()
    {
        var cloud = CloudSaveMapper.ToCloud(CreateSample(), 0);

        // camp_briefing es un flag, pero no un hito del catálogo
        CollectionAssert.AreEquivalent(new[] { "woke_up", "has_sword" }, cloud.CompletedMilestones.Select(m => m.MilestoneId));
        // has_sword venía solo como flag (guardado anterior a PlayStats): toma la fecha del guardado
        var sword = cloud.CompletedMilestones.Single(m => m.MilestoneId == "has_sword");
        Assert.AreEqual(new DateTime(2026, 10, 21, 10, 30, 0, DateTimeKind.Utc), sword.CompletedAt);
        Assert.IsNull(sword.PlayTimeSec);
    }

    [Test]
    public void ToCloud_UnknownHealthIsNull_AndInventoryIsMerged()
    {
        var data = CreateSample();
        data.match.health = -1;
        data.inventory.Add(new InventoryEntry { materialId = "cuero_antiguo", amount = 2 });
        data.inventory.Add(new InventoryEntry { materialId = "pocion", amount = 0 });

        var cloud = CloudSaveMapper.ToCloud(data, 0);

        Assert.IsNull(cloud.Health);
        Assert.AreEqual(5, cloud.Inventory.Single(i => i.ItemId == "cuero_antiguo").Quantity);
        Assert.IsFalse(cloud.Inventory.Any(i => i.ItemId == "pocion"));
    }

    [Test]
    public void CloudRoundTrip_RestoresTheSameSave()
    {
        var original = CreateSample();
        var json = CloudJson.Write(CloudSaveMapper.ToCloud(original, 2));
        var back = CloudSaveMapper.ToLocal(CloudJson.Read<CloudSaveData>(json));

        Assert.IsTrue(back.IsValid);
        Assert.AreEqual(original.savedAtUtc, back.savedAtUtc);
        Assert.AreEqual("Tsuki", back.match.activeCharacter);
        Assert.AreEqual(70, back.match.health);
        Assert.AreEqual(original.checkpoint.scene, back.checkpoint.scene);
        Assert.AreEqual(-2f, back.checkpoint.posY);
        Assert.IsTrue(back.progress.sealOpened);
        Assert.AreEqual(1, back.progress.swordLevel);
        CollectionAssert.AreEqual(original.progress.flags, back.progress.flags);
        CollectionAssert.AreEquivalent(original.progress.collected, back.progress.collected);
        CollectionAssert.AreEqual(original.progress.defeated, back.progress.defeated);
        CollectionAssert.AreEqual(original.progress.equipped, back.progress.equipped);
        Assert.AreEqual(12, back.stats.enemiesDefeated);
        Assert.AreEqual(3, back.inventory.Single(i => i.materialId == "cuero_antiguo").amount);
        Assert.AreEqual(2, back.progress.milestones.Count);
    }

    [Test]
    public void CloudJson_UsesCamelCaseAndOmitsNulls()
    {
        var data = CreateSample();
        data.match.health = -1;
        string json = CloudJson.Write(CloudSaveMapper.ToCloud(data, 0));

        StringAssert.Contains("\"activeCharacter\":\"TSUKI\"", json);
        StringAssert.Contains("\"extraData\":{\"v\":1", json);
        StringAssert.Contains("\"completedAt\":\"2026-10-21T10:00:00Z\"", json);
        StringAssert.DoesNotContain("\"health\"", json);
    }

    [TestCase("02_OpenField/Pickup_Flor_A")]
    [TestCase("05_CombatZone/Enemy_Melee_A:drop1")]
    public void PersistentIds_SurviveEncoding(string id)
    {
        string encoded = CloudSaveMapper.EncodeId(id);
        StringAssert.IsMatch("^[A-Za-z0-9_.:-]{1,100}$", encoded);
        Assert.AreEqual(id, CloudSaveMapper.DecodeId(encoded));
    }

    [Test]
    public void IsNewer_ComparesSaveTimes()
    {
        Assert.IsTrue(CloudSaveMapper.IsNewer("2026-10-21T11:00:00Z", "2026-10-21T10:00:00Z"));
        Assert.IsFalse(CloudSaveMapper.IsNewer("2026-10-21T10:00:00Z", "2026-10-21T11:00:00Z"));
        Assert.IsTrue(CloudSaveMapper.IsNewer("2026-10-21T10:00:00Z", null));
        Assert.IsFalse(CloudSaveMapper.IsNewer(null, "2026-10-21T10:00:00Z"));
    }

    [Test]
    public void Config_UsesFileThenEnvironmentOverride()
    {
        string path = Path.Combine(dir, CloudConfig.FileName);
        Assert.AreEqual(CloudConfig.DefaultBaseUrl + "/api/v1", CloudConfig.Load(path, null).ApiRoot);

        File.WriteAllText(path, "{\"baseUrl\":\"https://tdimp-api.onrender.com/\",\"apiKey\":\"x\"}");
        Assert.AreEqual("https://tdimp-api.onrender.com/api/v1", CloudConfig.Load(path, null).ApiRoot);
        Assert.AreEqual("http://localhost:18090/api/v1", CloudConfig.Load(path, "http://localhost:18090").ApiRoot);
    }

    [Test]
    public void TokenStore_EncryptsAndIsBoundToTheDevice()
    {
        string path = Path.Combine(dir, "cloud_session.dat");
        const string token = "Zm9vYmFyYmF6cXV4cXV1eGNvcmdlZ3JhdWx0Z2FycGx5";

        new FileCloudTokenStore(path, "device-a").Save(token);

        StringAssert.DoesNotContain(token, File.ReadAllText(path));
        Assert.AreEqual(token, new FileCloudTokenStore(path, "device-a").Load());
        // Otro equipo no puede leerlo (y el archivo ilegible se descarta)
        Assert.IsNull(new FileCloudTokenStore(path, "device-b").Load());
        Assert.IsFalse(File.Exists(path));
    }

    [Test]
    public void Outbox_PersistsAndResetsForAnotherAccount()
    {
        string path = Path.Combine(dir, "cloud_outbox.json");
        var outbox = new CloudOutbox(path);
        outbox.AddEvent(new CloudEvent { Id = Guid.NewGuid().ToString(), Type = PlayStats.CraftType, OccurredAt = DateTime.UtcNow });
        Assert.IsTrue(outbox.AddAchievement(CloudSync.FirstCraft, DateTime.UtcNow));
        Assert.IsFalse(outbox.AddAchievement(CloudSync.FirstCraft, DateTime.UtcNow));

        // Lo jugado sin sesión se asigna a la primera cuenta que entra
        outbox.BindPlayer("player-1");
        outbox.State.SaveVersion = 3;
        outbox.Save();

        var reloaded = new CloudOutbox(path);
        reloaded.Load();
        Assert.AreEqual(1, reloaded.State.Events.Count);
        Assert.AreEqual(3, reloaded.State.SaveVersion);

        // Otra cuenta en el mismo equipo no hereda lo pendiente de la anterior
        reloaded.BindPlayer("player-2");
        Assert.AreEqual(0, reloaded.State.Events.Count);
        Assert.AreEqual(0, reloaded.State.SaveVersion);
        Assert.AreEqual(0, reloaded.State.AchievementsPending.Count);
    }

    [Test]
    public void OldSaveFlags_BecomeMilestonesWithAFixedDate()
    {
        var go = new UnityEngine.GameObject("SessionStateTest");
        try
        {
            var session = go.AddComponent<SessionState>();
            var old = CreateSample();
            old.progress.milestones.Clear(); // guardado anterior a PlayStats: solo flags

            old.ApplyTo(session);
            var first = SaveData.FromSession(session);
            first.savedAtUtc = "2026-10-22T09:00:00Z"; // un guardado posterior
            var cloud = CloudSaveMapper.ToCloud(first, 0);

            var sword = cloud.CompletedMilestones.Single(m => m.MilestoneId == "has_sword");
            Assert.AreEqual(new DateTime(2026, 10, 21, 10, 30, 0, DateTimeKind.Utc), sword.CompletedAt);
            Assert.IsNull(sword.PlayTimeSec);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SaveData_KeepsStatsAndMilestones()
    {
        var original = CreateSample();
        var json = UnityEngine.JsonUtility.ToJson(original);
        var back = UnityEngine.JsonUtility.FromJson<SaveData>(json);

        Assert.AreEqual(1800, back.stats.playTimeSec);
        Assert.AreEqual("woke_up", back.progress.milestones[0].id);
    }
}
