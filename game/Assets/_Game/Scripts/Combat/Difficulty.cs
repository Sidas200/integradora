using UnityEngine;

// Ajuste global de dificultad del capítulo: escala el daño que recibe Tsuki y la vida de los enemigos.
// Se aplica encima de los valores de cada prefab y del multiplicador de zona (SceneContext).
public static class Difficulty
{
    public const float DamageToPlayer = 0.85f; // -15%
    public const float EnemyHealth = 0.85f;    // -15%

    public static int Damage(int amount) =>
        amount <= 0 ? amount : Mathf.Max(1, Mathf.RoundToInt(amount * DamageToPlayer));

    public static int Health(int max) => Mathf.Max(1, Mathf.RoundToInt(max * EnemyHealth));
}
