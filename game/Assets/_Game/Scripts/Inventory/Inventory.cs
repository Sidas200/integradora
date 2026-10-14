using System;
using System.Collections.Generic;
using UnityEngine;

// GAME-41 / GAME-45: guarda cuántos materiales tiene el jugador.
// Va en el Player. Los materiales se guardan por id de texto (fácil de guardar en JSON/Supabase después).
public class Inventory : MonoBehaviour
{
    private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();

    // Se dispara cada vez que cambia una cantidad: (idMaterial, cantidadNueva).
    // El HUD de Omar se podrá suscribir a este evento.
    public event Action<string, int> OnAmountChanged;

    public void Add(MaterialData material, int amount = 1)
    {
        if (material == null || amount <= 0) return;

        amounts.TryGetValue(material.id, out int current);
        amounts[material.id] = current + amount;
        OnAmountChanged?.Invoke(material.id, amounts[material.id]);
    }

    public int GetAmount(string materialId)
    {
        return amounts.TryGetValue(materialId, out int value) ? value : 0;
    }

    public bool Has(string materialId, int amount)
    {
        return GetAmount(materialId) >= amount;
    }

    // Lo usará el Crafting (GAME-52). Solo descuenta si alcanza; si no, no toca nada.
    public bool TryConsume(string materialId, int amount)
    {
        if (!Has(materialId, amount)) return false;

        amounts[materialId] -= amount;
        OnAmountChanged?.Invoke(materialId, amounts[materialId]);
        return true;
    }

        // Revisa si hay suficientes ingredientes para la receta (no consume nada).
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.result == null) return false;

        foreach (var ing in recipe.ingredients)
        {
            if (ing.material == null) return false;
            if (!Has(ing.material.id, ing.amount)) return false;
        }
        return true;
    }

    // Todo o nada: si falta algo, no consume nada.
    public bool TryCraft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe)) return false;

        foreach (var ing in recipe.ingredients)
            TryConsume(ing.material.id, ing.amount);

        Add(recipe.result, recipe.resultAmount);
        return true;
    }
}
