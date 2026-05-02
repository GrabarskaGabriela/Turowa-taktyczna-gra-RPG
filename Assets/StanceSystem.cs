using UnityEngine;

/// <summary>
/// Typ postawy postaci.
/// </summary>
public enum StanceType
{
    Aggressive,  // Agresywna
    Balanced,    // Zbalansowana
    Defensive    // Defensywna
}

/// <summary>
/// System postaw – przechowuje mnożniki i stosuje je do statystyk.
/// </summary>
public static class StanceSystem
{
    // Mnożniki ataku dla każdej postawy
    private static readonly float[] AttackMultipliers = new float[]
    {
        1.20f,  // Agresywna  (+20% atak)
        1.00f,  // Zbalansowana (brak zmian)
        0.80f   // Defensywna (-20% atak)
    };

    // Mnożniki obrony dla każdej postawy
    private static readonly float[] DefenseMultipliers = new float[]
    {
        0.80f,  // Agresywna  (-20% obrona)
        1.00f,  // Zbalansowana (brak zmian)
        1.20f   // Defensywna (+20% obrona)
    };

    public static int ApplyStanceToAttack(int baseAttack, StanceType stance)
    {
        return Mathf.RoundToInt(baseAttack * AttackMultipliers[(int)stance]);
    }

    public static int ApplyStanceToDefense(int baseDefense, StanceType stance)
    {
        return Mathf.RoundToInt(baseDefense * DefenseMultipliers[(int)stance]);
    }

    /// <summary>
    /// Zwraca czytelną nazwę postawy po polsku.
    /// </summary>
    public static string GetStanceName(StanceType stance)
    {
        return stance switch
        {
            StanceType.Aggressive => "Agresywna",
            StanceType.Balanced => "Zbalansowana",
            StanceType.Defensive => "Defensywna",
            _ => "Nieznana"
        };
    }

    /// <summary>
    /// Zwraca kolor UI reprezentujący postawę.
    /// </summary>
    public static Color GetStanceColor(StanceType stance)
    {
        return stance switch
        {
            StanceType.Aggressive => new Color(0.9f, 0.2f, 0.2f),  // Czerwony
            StanceType.Balanced => new Color(0.2f, 0.7f, 0.2f),  // Zielony
            StanceType.Defensive => new Color(0.2f, 0.4f, 0.9f),  // Niebieski
            _ => Color.white
        };
    }
}