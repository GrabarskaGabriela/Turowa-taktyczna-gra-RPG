using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wizualne oznaczenie aktywnej postawy postaci.
/// Dodaj ten komponent do prefaba każdej postaci.
/// Wymaga: SpriteRenderer na obiekcie postaci, Canvas w scenie.
/// </summary>
public class StanceIndicator : MonoBehaviour
{
    [Header("Ikona postawy (opcjonalna)")]
    [Tooltip("Obiekt wyświetlający ikonę postawy nad postacią")]
    public SpriteRenderer stanceIcon;

    [Header("Ikony postaw")]
    public Sprite aggressiveSprite;
    public Sprite balancedSprite;
    public Sprite defensiveSprite;

    private Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Update()
    {
        if (character == null) return;
        RefreshVisual(character.currentStance);
    }

    public void RefreshVisual(StanceType stance)
    {
        if (stanceIcon == null) return;

        stanceIcon.color = StanceSystem.GetStanceColor(stance);

        stanceIcon.sprite = stance switch
        {
            StanceType.Aggressive => aggressiveSprite,
            StanceType.Balanced => balancedSprite,
            StanceType.Defensive => defensiveSprite,
            _ => null
        };
    }
}