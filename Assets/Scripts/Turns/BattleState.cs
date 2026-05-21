namespace Turns
{
    public enum BattleState
    {
        AwaitingTurnStart = 0,

        PlayerChooseAction = 1,
        PlayerChooseMove = 2,
        PlayerChooseAttackTarget = 3,

        ExecutingAiTurn = 4
    }
}
