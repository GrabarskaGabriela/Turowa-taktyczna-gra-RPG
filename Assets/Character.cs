using UnityEngine;

/// <summary>
/// Bazowa klasa postaci. Wszystkie klasy dziedziczą po Character.
/// </summary>
public abstract class Character : MonoBehaviour
{
    [Header("Statystyki bazowe")]
    public string characterName;
    public int maxHP;
    public int currentHP;
    public int moveRange;       // ile pól może przejść w turze
    public int meleeAttack;     // obrażenia wręcz
    public int rangedAttack;    // obrażenia dystansowe (0 = brak)
    public int attackRange;     // zasięg ataku w polach (1 = tylko sąsiednie)

    [Header("Pozycja na siatce")]
    public Vector2Int gridPosition;

    [Header("Postawa")]
    public StanceType currentStance = StanceType.Balanced;

    // Obliczone statystyki uwzględniające postawę
    public int EffectiveMeleeAttack => StanceSystem.ApplyStanceToAttack(meleeAttack, currentStance);
    public int EffectiveRangedAttack => StanceSystem.ApplyStanceToAttack(rangedAttack, currentStance);
    public int EffectiveDefense => StanceSystem.ApplyStanceToDefense(GetBaseDefense(), currentStance);

    // Każda klasa definiuje własną obronę bazową
    protected abstract int GetBaseDefense();

    public bool IsAlive => currentHP > 0;

    /// <summary>
    /// Inicjalizacja postaci – wywołaj po Instantiate.
    /// </summary>
    public virtual void Initialize()
    {
        currentHP = maxHP;
        currentStance = StanceType.Balanced;
    }

    /// <summary>
    /// Zmiana postawy. Można wywołać tylko na początku tury tej postaci.
    /// </summary>
    public void ChangeStance(StanceType newStance)
    {
        currentStance = newStance;
        Debug.Log($"{characterName} zmienił postawę na {newStance}");
    }

    /// <summary>
    /// Zadaje obrażenia postaci. Jeśli HP <= 0, wywołuje śmierć.
    /// </summary>
    public void TakeDamage(int damage)
    {
        int actual = Mathf.Max(0, damage - EffectiveDefense);
        currentHP -= actual;
        currentHP = Mathf.Max(0, currentHP);
        Debug.Log($"{characterName} otrzymał {actual} obrażeń. HP: {currentHP}/{maxHP}");

        // Animacja trafienia
        StartCoroutine(PlayHitAnimation());

        if (!IsAlive)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{characterName} zginął!");
        // Krótkie opóźnienie przed usunięciem z planszy
        StartCoroutine(DieRoutine());
    }

    private System.Collections.IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator PlayHitAnimation()
    {
        // Proste migotanie przy trafieniu
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            renderer.color = Color.white;
        }
    }

    /// <summary>
    /// Animacja ataku – przesuwa sprite w stronę celu i wraca.
    /// </summary>
    public System.Collections.IEnumerator PlayAttackAnimation(Vector3 targetWorldPos)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = (targetWorldPos - startPos).normalized;
        Vector3 attackPos = startPos + direction * 0.3f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.position = Vector3.Lerp(startPos, attackPos, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.position = Vector3.Lerp(attackPos, startPos, t);
            yield return null;
        }
        transform.position = startPos;
    }
}