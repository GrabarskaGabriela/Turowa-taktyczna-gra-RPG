using UnityEngine;

// ─────────────────────────────────────────────
// WOJOWNIK CIĘŻKI
// Duże HP, mały zasięg ruchu, silny atak wręcz
// ─────────────────────────────────────────────
public class HeavyWarrior : Character
{
    private void Awake()
    {
        characterName = "Wojownik Ciężki";
        maxHP = 120;
        moveRange = 2;
        meleeAttack = 35;
        rangedAttack = 0;
        attackRange = 1;
    }

    protected override int GetBaseDefense() => 15;
}

// ─────────────────────────────────────────────
// WOJOWNIK LEKKI
// Średnie HP, średni zasięg, wręcz + ograniczony dystansowy
// ─────────────────────────────────────────────
public class LightWarrior : Character
{
    private void Awake()
    {
        characterName = "Wojownik Lekki";
        maxHP = 80;
        moveRange = 4;
        meleeAttack = 22;
        rangedAttack = 12;
        attackRange = 2;
    }

    protected override int GetBaseDefense() => 8;
}

// ─────────────────────────────────────────────
// ŁUCZNIK CIĘŻKI
// Średnie HP, mały zasięg ruchu, silny dystansowy, umiarkowany wręcz
// ─────────────────────────────────────────────
public class HeavyArcher : Character
{
    private void Awake()
    {
        characterName = "Łucznik Ciężki";
        maxHP = 75;
        moveRange = 2;
        meleeAttack = 14;
        rangedAttack = 30;
        attackRange = 5;
    }

    protected override int GetBaseDefense() => 10;
}

// ─────────────────────────────────────────────
// ŁUCZNIK LEKKI
// Małe HP, duży zasięg ruchu, umiarkowany dystansowy, słaby wręcz
// ─────────────────────────────────────────────
public class LightArcher : Character
{
    private void Awake()
    {
        characterName = "Łucznik Lekki";
        maxHP = 50;
        moveRange = 6;
        meleeAttack = 8;
        rangedAttack = 20;
        attackRange = 7;
    }

    protected override int GetBaseDefense() => 5;
}