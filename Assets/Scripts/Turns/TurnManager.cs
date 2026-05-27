using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI;
using Combat;
using Grid;
using UI;
using Units;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turns
{
    public class TurnManager : MonoBehaviour
    {
        [Header("Refs")] public GridManager grid;
        public CombatSystem combat;
        public FuzzyAiController ai;

        [Header("Units")] public List<Unit> units = new List<Unit>();

        [Header("State")] public BattleState state = BattleState.AwaitingTurnStart;
        [SerializeField] private float aiStepDelay = 0.35f;
        [SerializeField] private float aiMoveStopDistance = 0.03f;

        public Unit ActiveUnit => _activeUnit;
        public BattleState State => state;
        public Unit LastSelectedTarget { get; private set; }
        public IReadOnlyList<Unit> CurrentTurnOrder
        {
            get
            {
                var order = new List<Unit>();
                if (_activeUnit != null && _activeUnit.IsAlive)
                    order.Add(_activeUnit);

                order.AddRange(_turnQueue.Where(u => u != null && u.IsAlive));
                return order;
            }
        }

        public bool CanPlayerChooseMove =>
            IsActivePlayerChoosingAction() && state != BattleState.PlayerChooseMove &&
            !_playerActions.moveUsed;

        public bool CanPlayerChooseAttack =>
            IsActivePlayerChoosingAction() && state != BattleState.PlayerChooseAttackTarget &&
            !_playerActions.attackUsed;

        private readonly Queue<Unit> _turnQueue = new Queue<Unit>();
        private Unit _activeUnit;
        private Coroutine _aiTurnRoutine;

        private readonly PlayerActionState _playerActions = new PlayerActionState();
        private List<Vector2Int> _cachedReachable = new List<Vector2Int>();

        private List<Vector2Int> _cachedAttackTiles = new List<Vector2Int>();

        private GridCoordinateSystem _coords;

        private static void Log(string message)
        {
            BattleLog.Add(message);
            Debug.Log($"[BattleLog] {message}");
        }

        private void Awake()
        {
            if (grid != null)
                _coords = grid.GetComponent<GridCoordinateSystem>();
        }

        public void BeginBattle()
        {
            Log("<b>--- BITWA ROZPOCZÄTA! ---</b>");
            BuildQueue();
            StartNextTurn();
        }

        void BuildQueue()
        {
            _turnQueue.Clear();
            foreach (var u in units.Where(u => u != null && u.IsAlive))
                _turnQueue.Enqueue(u);
        }

        void StartNextTurn()
        {
            CleanupDeadFront();
            grid.ClearRange();

            if (IsBattleOver())
            {
                Log("<b>--- BITWA ZAKOĹCZONA! ---</b>");
                state = BattleState.AwaitingTurnStart;
                return;
            }

            if (_turnQueue.Count == 0)
            {
                Log("<i>--- NOWA RUNDA ---</i>");
                BuildQueue();
                CleanupDeadFront();
                if (_turnQueue.Count == 0) return;
            }

            _activeUnit = _turnQueue.Dequeue();
            if (_activeUnit == null || !_activeUnit.IsAlive)
            {
                StartNextTurn();
                return;
            }

            if (_activeUnit.isPlayer)
            {
                Log($"<color=blue>Tura gracza:</color> {GetUnitLogName(_activeUnit)} na polu {_activeUnit.gridPos}");
                _playerActions.Reset();
                PreparePlayerTurn();
            }
            else
            {
                Log($"<color=red>Tura przeciwnika:</color> {GetUnitLogName(_activeUnit)} na polu {_activeUnit.gridPos}");
                state = BattleState.ExecutingAiTurn;
                _aiTurnRoutine = StartCoroutine(DoAiTurnRoutine(_activeUnit));
            }
        }

        void PreparePlayerTurn()
        {
            state = BattleState.PlayerChooseAction;
            CachePlayerReachable();
            grid.ClearRange();
        }

        public void PlayerChooseMove()
        {
            if (!CanPlayerChooseMove) return;

            state = BattleState.PlayerChooseMove;
            CachePlayerReachable();
            grid.ClearRange();
            grid.ShowRange(_cachedReachable, false);
        }

        public void PlayerChooseAttack()
        {
            if (!CanPlayerChooseAttack) return;

            state = BattleState.PlayerChooseAttackTarget;
            ShowAttackRange();
        }

        public void PlayerSkipMove()
        {
            if (_activeUnit == null || !_activeUnit.isPlayer || state != BattleState.PlayerChooseMove) return;

            Log($"{GetUnitLogName(_activeUnit)} pomija ruch.");
            _playerActions.moveUsed = true;
            ReturnToPlayerActionSelectionOrEndTurn();
        }

        public void PlayerSkipAttack()
        {
            if (_activeUnit == null || !_activeUnit.isPlayer || state != BattleState.PlayerChooseAttackTarget) return;

            Log($"{GetUnitLogName(_activeUnit)} pomija atak.");
            _playerActions.attackUsed = true;
            ReturnToPlayerActionSelectionOrEndTurn();
        }

        public void PlayerEndTurn()
        {
            if (_activeUnit == null || !_activeUnit.isPlayer) return;

            Log($"{GetUnitLogName(_activeUnit)} koĹ„czy turÄ™.");
            _playerActions.moveUsed = true;
            _playerActions.attackUsed = true;
            EndActiveTurn();
        }

        IEnumerator DoAiTurnRoutine(Unit enemy)
        {
            var aliveUnits = units.Where(x => x != null && x.IsAlive).ToList();
            var d = ai.Evaluate(enemy, aliveUnits, grid);

            Stance oldStance = enemy.stance;
            enemy.stance = (Stance)d.stance;
            if (oldStance != enemy.stance)
                Log($"{GetUnitLogName(enemy)} wybiera postawÄ™: <b>{UnitDisplayNames.StanceName(enemy.stance)}</b>");

            Vector2Int dest = new Vector2Int(d.moveX, d.moveY);
            Unit target = (d.targetId >= 0) ? UnitQuery.GetById(aliveUnits, d.targetId) : null;

            if (d.sequence == ActionSequence.MoveOnly || d.sequence == ActionSequence.MoveThenAttack)
            {
                if (enemy.gridPos != dest)
                    Log($"{GetUnitLogName(enemy)} rusza z {enemy.gridPos} na {dest}.");
                else
                    Log($"{GetUnitLogName(enemy)} zostaje na polu {enemy.gridPos}.");

                yield return MoveEnemyTo(enemy, dest);
            }

            if (d.attack && target != null && target.IsAlive)
            {
                if (d.sequence == ActionSequence.MoveThenAttack)
                    yield return new WaitForSeconds(aiStepDelay);

                if (enemy.GetBestDamageFromPosition(enemy.gridPos, target.gridPos) > 0)
                {
                    Log($"{GetUnitLogName(enemy)} atakuje {GetUnitLogName(target)}.");
                    combat.Attack(enemy, target);
                }
                else
                {
                    Log("<color=orange>AI ostrzeĹĽenie:</color> Cel poza zasiÄ™giem");
                }
            }

            if (d.sequence == ActionSequence.AttackThenMove)
            {
                yield return new WaitForSeconds(aiStepDelay);
                if (enemy.gridPos != dest)
                    Log($"{GetUnitLogName(enemy)} wykonuje ruch taktyczny z {enemy.gridPos} na {dest}.");
                else
                    Log($"{GetUnitLogName(enemy)} zostaje po ataku na polu {enemy.gridPos}.");
                yield return MoveEnemyTo(enemy, dest);
            }

            yield return new WaitForSeconds(aiStepDelay);
            _aiTurnRoutine = null;
            EndActiveTurn();
        }

        private IEnumerator MoveEnemyTo(Unit enemy, Vector2Int dest)
        {
            if (enemy == null || !enemy.IsAlive)
                yield break;

            if (enemy.gridPos == dest)
                yield break;

            enemy.gridPos = dest;
            yield return WaitForUnitMovement(enemy);
        }

        private IEnumerator WaitForUnitMovement(Unit unit)
        {
            if (unit == null || _coords == null)
                yield break;

            Vector3 targetPos = _coords.GridToWorld(unit.gridPos);
            const float maxWaitSeconds = 2f;
            float elapsed = 0f;

            while (unit != null &&
                   Vector3.Distance(unit.transform.position, targetPos) > aiMoveStopDistance &&
                   elapsed < maxWaitSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public void PlayerClickGrid(Vector2Int clicked)
        {
            if (_activeUnit == null || !_activeUnit.IsAlive || !_activeUnit.isPlayer) return;

            if (state == BattleState.PlayerChooseMove)
                HandlePlayerMoveClick(clicked);
            else if (state == BattleState.PlayerChooseAttackTarget)
                HandlePlayerAttackClick(clicked);
        }

        void HandlePlayerMoveClick(Vector2Int dest)
        {
            if (_playerActions.moveUsed) return;
            if (!ActionValidator.CanMoveTo(_activeUnit, dest, _cachedReachable)) return;

            Log($"{GetUnitLogName(_activeUnit)} rusza z {_activeUnit.gridPos} na {dest}.");
            _activeUnit.gridPos = dest;
            SyncUnitTransformToGrid(_activeUnit);

            _playerActions.moveUsed = true;
            ReturnToPlayerActionSelectionOrEndTurn();
        }

        void HandlePlayerAttackClick(Vector2Int clicked)
        {
            if (_playerActions.attackUsed) return;

            Unit target = UnitQuery.GetUnitAt(units, clicked);
            if (target == null)
            {
                Log("Na tym polu nie ma celu do ataku.");
                return;
            }

            LastSelectedTarget = target;

            if (!ActionValidator.CanAttack(_activeUnit, target))
            {
                Log("Nie mozesz zaatakowac tego celu.");
                return;
            }

            Log($"{GetUnitLogName(_activeUnit)} atakuje {GetUnitLogName(target)}.");
            combat.Attack(_activeUnit, target);
            _playerActions.attackUsed = true;
            ReturnToPlayerActionSelectionOrEndTurn();
        }

        void ShowAttackRange()
        {
            grid.ClearRange();
            _cachedAttackTiles = GetAttackTilesFrom(_activeUnit.gridPos, _activeUnit);
            grid.ShowRange(_cachedAttackTiles, true);
        }

        private static List<Vector2Int> GetAttackTilesFrom(Vector2Int from, Unit attacker)
        {
            var tiles = new HashSet<Vector2Int>();
            int maxRange = Mathf.Max(attacker.classData.meleeRange, attacker.classData.rangedRange);
            if (attacker.classData != null && attacker.classData.className == "Heavy Warrior")
                maxRange = Mathf.Max(maxRange, 1);

            for (int dx = -maxRange; dx <= maxRange; dx++)
            {
                for (int dy = -maxRange; dy <= maxRange; dy++)
                {
                    var candidate = new Vector2Int(from.x + dx, from.y + dy);
                    if (!GridManager.InBounds(candidate)) continue;

                    if (candidate == from) continue;

                    if (attacker.GetBestDamageFromPosition(from, candidate) > 0)
                        tiles.Add(candidate);
                }
            }

            return tiles.ToList();
        }

        void EndActiveTurn()
        {
            grid.ClearRange();
            if (_activeUnit != null && _activeUnit.IsAlive)
                _turnQueue.Enqueue(_activeUnit);

            _activeUnit = null;
            state = BattleState.AwaitingTurnStart;
            StartNextTurn();
        }

        void ReturnToPlayerActionSelectionOrEndTurn()
        {
            grid.ClearRange();

            if (_playerActions.moveUsed && _playerActions.attackUsed)
            {
                EndActiveTurn();
                return;
            }

            state = BattleState.PlayerChooseAction;
        }

        private bool IsActivePlayerChoosingAction()
        {
            if (_activeUnit == null || !_activeUnit.isPlayer || !_activeUnit.IsAlive)
                return false;

            return state == BattleState.PlayerChooseAction ||
                   state == BattleState.PlayerChooseMove ||
                   state == BattleState.PlayerChooseAttackTarget;
        }

        void CleanupDeadFront()
        {
            while (_turnQueue.Count > 0 && (_turnQueue.Peek() == null || !_turnQueue.Peek().IsAlive))
                _turnQueue.Dequeue();
        }

        bool IsBattleOver()
        {
            if (units == null || units.Count == 0) return false;
            int alivePlayers = units.Count(u => u != null && u.IsAlive && u.isPlayer);
            int aliveEnemies = units.Count(u => u != null && u.IsAlive && !u.isPlayer);

            if (alivePlayers == 0)
            {
                SceneManager.LoadScene(2);
                return true;
            }
            if (aliveEnemies == 0)
            {
                SceneManager.LoadScene(3);
                return true;
            }
            return false;
        }

        void CachePlayerReachable()
        {
            var occupied = UnitQuery.GetOccupiedTiles(units);
            _cachedReachable = grid.GetReachableTiles(_activeUnit.gridPos, _activeUnit.classData.moveRange, occupied);
        }

        private void SyncUnitTransformToGrid(Unit u)
        {
            if (u == null || _coords == null) return;
            Vector3 cellWorldPos = _coords.GridToWorld(u.gridPos);
            cellWorldPos.y += 0.25f;
            cellWorldPos.z = -1f;
            u.transform.position = cellWorldPos;
        }

        public void PlayerSkipCurrentStep()
        {
            if (_activeUnit == null || !_activeUnit.isPlayer) return;

            if (state == BattleState.PlayerChooseAction)
                PlayerEndTurn();
            else if (state == BattleState.PlayerChooseMove)
                PlayerSkipMove();
            else if (state == BattleState.PlayerChooseAttackTarget)
                PlayerSkipAttack();
        }

        private static string GetUnitLogName(Unit unit)
        {
            if (unit == null)
                return "Jednostka";

            return UnitDisplayNames.UnitName(unit);
        }
    }
}
