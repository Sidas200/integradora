// GAME-102 / SCRUM-86 (102.3): contrato del guardado.
// Hoy se implementa en local; después podría haber uno remoto (backend) sin cambiar el juego.
public interface ISaveRepository
{
    void Save(SaveData data);
    SaveData Load();     // null si no hay guardado o está dañado (SRS-HEX-04)
    bool Exists();
    void Delete();
}