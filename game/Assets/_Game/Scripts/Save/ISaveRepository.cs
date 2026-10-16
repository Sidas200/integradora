// GAME-102 / SCRUM-86 (102.3): contrato del guardado.
// Hoy se implementa en local; después podría haber uno remoto (backend) sin cambiar el juego.
public interface ISaveRepository
{
    void Save(SaveData data);
    bool TryLoad(out SaveData data);
    bool HasSave();
    void Delete();
}