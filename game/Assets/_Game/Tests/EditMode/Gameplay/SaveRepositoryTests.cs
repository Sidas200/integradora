using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// GAME-102 / SCRUM-87 (102.4): pruebas EditMode del guardado local.
// Usan una carpeta temporal: no tocan el save_slot1.json real.
public class SaveRepositoryTests
{
    private string dir;
    private LocalSaveRepository repo;

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "hex_save_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        repo = new LocalSaveRepository(Path.Combine(dir, "save_slot1.json"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    private static SaveData CreateSample()
    {
        var data = new SaveData();
        data.match.health = 70;
        data.checkpoint.scene = "Assets/Escena.unity";
        data.checkpoint.posX = 1.5f;
        data.checkpoint.posY = -2f;
        data.progress.safePointId = "base";
        data.progress.sealOpened = true;
        data.progress.swordLevel = 1;
        data.inventory.Add(new InventoryEntry { materialId = "cuero_antiguo", amount = 3 });
        data.inventory.Add(new InventoryEntry { materialId = "flor_destello", amount = 1 });
        return data;
    }

    [Test]
    public void SaveThenLoad_ReturnsSameData()
    {
        repo.Save(CreateSample());

        var loaded = repo.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(70, loaded.match.health);
        Assert.AreEqual("Assets/Escena.unity", loaded.checkpoint.scene);
        Assert.AreEqual(1.5f, loaded.checkpoint.posX);
        Assert.AreEqual(-2f, loaded.checkpoint.posY);
        Assert.AreEqual("base", loaded.progress.safePointId);
        Assert.IsTrue(loaded.progress.sealOpened);
        Assert.AreEqual(1, loaded.progress.swordLevel);
        Assert.AreEqual(2, loaded.inventory.Count);
        Assert.AreEqual("cuero_antiguo", loaded.inventory[0].materialId);
        Assert.AreEqual(3, loaded.inventory[0].amount);
    }

    [Test]
    public void Save_Twice_ReplacesPreviousSave()
    {
        repo.Save(CreateSample());
        var second = CreateSample();
        second.match.health = 25;
        repo.Save(second);

        Assert.AreEqual(25, repo.Load().match.health);
        Assert.IsFalse(File.Exists(repo.FilePath + ".tmp"));
    }

    [Test]
    public void Load_WithoutFile_ReturnsNull()
    {
        Assert.IsFalse(repo.Exists());
        Assert.IsNull(repo.Load());
    }

    [Test]
    public void Load_CorruptedFile_ReturnsNullWithoutThrowing()
    {
        File.WriteAllText(repo.FilePath, "hola, esto no es JSON");
        LogAssert.Expect(LogType.Warning, new Regex("dañado"));

        SaveData loaded = null;
        Assert.DoesNotThrow(() => loaded = repo.Load());
        Assert.IsNull(loaded);
    }

    [Test]
    public void Load_OtherVersion_ReturnsNull()
    {
        var old = CreateSample();
        old.version = 1;
        File.WriteAllText(repo.FilePath, JsonUtility.ToJson(old));
        LogAssert.Expect(LogType.Warning, new Regex("otra versión"));

        Assert.IsNull(repo.Load());
    }

    [Test]
    public void Delete_RemovesFile()
    {
        repo.Save(CreateSample());
        Assert.IsTrue(repo.Exists());

        repo.Delete();

        Assert.IsFalse(repo.Exists());
    }
}
