namespace Turns
{
    public enum BattleState
    {
        AwaitingTurnStart = 0,

        PlayerChooseMove = 1,
        PlayerChooseAttackTarget = 2,

        ExecutingAiTurn = 3
    }
}