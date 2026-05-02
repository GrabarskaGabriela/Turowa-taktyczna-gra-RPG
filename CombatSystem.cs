using UnityEngine;

/// <summary>
/// System walki – sprawdza zasięg, oblicza obrażenia, przeprowadza atak.
/// </summary>
public static class CombatSystem
{
    /// <summary>
    /// Próbuje wykonać atak. Zwraca true jeśli atak się udał.
    /// </summary>
    public static bool TryAttack(Character attacker, Character defender)
    {
        if (!attacker.IsAlive || !defender.IsAlive)
        {
            Debug.LogWarning("Próba ataku martwą postacią.");
            return false;
        }

        int distance = GetGridDistance(attacker.gridPosition, defender.gridPosition);

        // Sprawdź czy cel jest w zasięgu
        if (distance > attacker.attackRange)
        {
            Debug.Log($"{attacker.characterName}: cel poza zasięgiem ({distance} > {attacker.attackRange})");
            return false;
        }

        bool isMelee = distance == 1;
        int damage = CalculateDamage(attacker, defender, isMelee);

        // Animacja ataku
        attacker.StartCoroutine(attacker.PlayAttackAnimation(GridToWorld(defender.gridPosition)));

        defender.TakeDamage(damage);
        return true;
    }

    /// <summary>
    /// Oblicza obrażenia:
    /// 1. Wybierz atak (wręcz gdy distance=1, dystansowy w pozostałych).
    /// 2. Zastosuj mnożnik postawy atakującego.
    /// 3. Odejmij obronę obrońcy z mnożnikiem jego postawy.
    /// 4. Minimum 1 obrażenie.
    /// </summary>
    public static int CalculateDamage(Character attacker, Character defender, bool isMelee)
    {
        int baseAttack = isMelee
            ? attacker.EffectiveMeleeAttack
            : attacker.EffectiveRangedAttack;

        int defense = defender.EffectiveDefense;
        int damage = Mathf.Max(1, baseAttack - defense);

        Debug.Log($"Atak: {attacker.characterName} ({(isMelee ? "wręcz" : "dystansowy")}) " +
                  $"-> {defender.characterName} | Atak={baseAttack}, Obrona={defense}, Obrażenia={damage}");

        return damage;
    }

    /// <summary>
    /// Dystans Manhattan na siatce kwadratowej.
    /// </summary>
    public static int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// Przelicza pozycję na siatce na pozycję w świecie Unity (zakłada tile 1x1).
    /// </summary>
    public static Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, 0f);
    }

    /// <summary>
    /// Sprawdza czy atak wręcz jest możliwy (odległość == 1).
    /// </summary>
    public static bool CanMeleeAttack(Character attacker, Character defender)
    {
        return GetGridDistance(attacker.gridPosition, defender.gridPosition) == 1;
    }

    /// <summary>
    /// Sprawdza czy atak dystansowy jest możliwy.
    /// </summary>
    public static bool CanRangedAttack(Character attacker, Character defender)
    {
        if (attacker.rangedAttack == 0) return false;
        int dist = GetGridDistance(attacker.gridPosition, defender.gridPosition);
        return dist > 1 && dist <= attacker.attackRange;
    }
}