using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawner postaci – umieszcza jednostki na startowych pozycjach obu drużyn.
/// Przeciągnij prefaby postaci do odpowiednich pól w Inspectorze.
/// </summary>
public class CharacterSpawner : MonoBehaviour
{
    [Header("Prefaby – Drużyna 1 (lewa strona)")]
    public GameObject heavyWarriorPrefab;
    public GameObject lightWarriorPrefab;
    public GameObject heavyArcherPrefab;
    public GameObject lightArcherPrefab;

    [Header("Prefaby – Drużyna 2 (prawa strona)")]
    public GameObject heavyWarriorPrefab2;
    public GameObject lightWarriorPrefab2;
    public GameObject heavyArcherPrefab2;
    public GameObject lightArcherPrefab2;

    [Header("Rozmiar siatki")]
    public int gridWidth = 10;
    public int gridHeight = 8;

    // Startowe pozycje drużyn (możesz je edytować w Inspectorze)
    private Vector2Int[] team1StartPositions = new Vector2Int[]
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, 3),
        new Vector2Int(1, 5),
        new Vector2Int(1, 7),
    };

    private Vector2Int[] team2StartPositions = new Vector2Int[]
    {
        new Vector2Int(8, 1),
        new Vector2Int(8, 3),
        new Vector2Int(8, 5),
        new Vector2Int(8, 7),
    };

    public List<Character> Team1Characters { get; private set; } = new();
    public List<Character> Team2Characters { get; private set; } = new();

    private void Start()
    {
        SpawnTeam(
            new[] { heavyWarriorPrefab, lightWarriorPrefab, heavyArcherPrefab, lightArcherPrefab },
            team1StartPositions,
            Team1Characters,
            "Drużyna 1"
        );

        SpawnTeam(
            new[] { heavyWarriorPrefab2, lightWarriorPrefab2, heavyArcherPrefab2, lightArcherPrefab2 },
            team2StartPositions,
            Team2Characters,
            "Drużyna 2"
        );
    }

    private void SpawnTeam(GameObject[] prefabs, Vector2Int[] positions,
                           List<Character> list, string teamName)
    {
        for (int i = 0; i < prefabs.Length && i < positions.Length; i++)
        {
            if (prefabs[i] == null) continue;

            Vector3 worldPos = new Vector3(positions[i].x, positions[i].y, 0f);
            GameObject go = Instantiate(prefabs[i], worldPos, Quaternion.identity);
            go.name = $"{teamName} – {go.name}";

            Character c = go.GetComponent<Character>();
            if (c != null)
            {
                c.gridPosition = positions[i];
                c.Initialize();
                list.Add(c);
            }
        }
    }
}