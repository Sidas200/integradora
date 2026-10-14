// Cualquier componente que tenga stats de vida puede implementar esto
public interface IHealthProvider
{
    int MaxHealth { get; }
    int Defense { get; }
}