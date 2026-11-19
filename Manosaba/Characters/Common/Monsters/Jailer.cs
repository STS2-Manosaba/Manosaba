using BaseLib.Abstracts;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Manosaba.Characters.Common.Monsters;

public sealed class Jailer : MonsterModel, ICustomModel
{
    // Placeholder HP until the exact value is decided.
    private const int StartingHp = 20;

    public override int MinInitialHp => StartingHp;
    public override int MaxInitialHp => StartingHp;
    public override string? HurtSfx => "event:/Manosaba/audio/monsters/jailer_hurt.ogg";

    protected override string VisualsPath => "jailer.tscn".MonsterScenePath();

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState moveState = new("GUARD_IDLE", static (IReadOnlyList<Creature> _) => Task.CompletedTask);
        moveState.FollowUpState = moveState;
        return new MonsterMoveStateMachine([moveState], moveState);
    }
}
