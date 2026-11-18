using BaseLib.Utils;
using BaseLib.Abstracts;
using manosaba.Extensions;
using Manosaba.Characters.Common.Commands;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Monsters;

public abstract class NovelSettingMonsterBase : MonsterModel, ICustomModel
{
    private const int StartingHp = 1;

    protected abstract string IdleMoveId { get; }
    protected abstract string VisualSceneFileName { get; }

    public override int MinInitialHp => StartingHp;
    public override int MaxInitialHp => StartingHp;
    public override bool IsHealthBarVisible => true;

    protected override string VisualsPath => VisualSceneFileName.MonsterScenePath();

    protected sealed override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState moveState = CreateMoveState();
        moveState.FollowUpState ??= moveState;
        return new MonsterMoveStateMachine([moveState], moveState);
    }

    protected abstract MoveState CreateMoveState();

    protected static List<Creature> GetLivingTargets(IReadOnlyList<Creature> targets)
    {
        return targets.Where(target => target != null && target.IsAlive).Distinct().ToList();
    }
}

public sealed class NovelFlameRabbit : NovelSettingMonsterBase
{
    private const int BurnAmount = 5;

    protected override string IdleMoveId => "NOVEL_FLAME_RABBIT_MOVE";
    protected override string VisualSceneFileName => "flame_rabbit.tscn";

    protected override MoveState CreateMoveState()
    {
        return new MoveState(IdleMoveId, FlameRabbitMove, new DebuffIntent());
    }

    private async Task FlameRabbitMove(IReadOnlyList<Creature> targets)
    {
        List<Creature> livingTargets = GetLivingTargets(targets);
        if (livingTargets.Count == 0)
        {
            return;
        }

        foreach (Creature target in livingTargets)
        {
            await CommonActions.Apply<BurnPower>(new ThrowingPlayerChoiceContext(), target, null, BurnAmount);
        }
    }
}

public sealed class NovelHolyWhiteSnake : NovelSettingMonsterBase
{
    private const decimal BlockAmount = 8m;

    protected override string IdleMoveId => "NOVEL_HOLY_WHITE_SNAKE_MOVE";
    protected override string VisualSceneFileName => "holy_white_snake.tscn";

    protected override MoveState CreateMoveState()
    {
        return new MoveState(IdleMoveId, HolyWhiteSnakeMove, new DefendIntent());
    }

    private async Task HolyWhiteSnakeMove(IReadOnlyList<Creature> targets)
    {
        _ = targets;
        if (base.Creature.CombatState == null)
        {
            return;
        }

        List<Player> players = base.Creature.CombatState.Players
            .Where(player => player?.Creature != null && player.Creature.IsAlive)
            .ToList();
        foreach (Player player in players)
        {
            await CreatureCmd.GainBlock(player.Creature, BlockAmount, ValueProp.Move, null);
        }
    }
}

public sealed class NovelClawedCockatrice : NovelSettingMonsterBase
{
    private const decimal DamageAmount = 9m;
    private const decimal VulnerableAmount = 1m;

    protected override string IdleMoveId => "NOVEL_CLAWED_COCKATRICE_MOVE";
    protected override string VisualSceneFileName => "clawed_cockatrice.tscn";

    protected override MoveState CreateMoveState()
    {
        return new MoveState(IdleMoveId, ClawedCockatriceMove, new SingleAttackIntent((int)DamageAmount), new DebuffIntent());
    }

    private async Task ClawedCockatriceMove(IReadOnlyList<Creature> targets)
    {
        List<Creature> livingTargets = GetLivingTargets(targets);
        if (livingTargets.Count == 0)
        {
            return;
        }

        ThrowingPlayerChoiceContext context = new();
        await CreatureCmd.Damage(context, livingTargets, DamageAmount, ValueProp.Move, base.Creature, null, null);
        foreach (Creature target in livingTargets)
        {
            await CommonActions.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), target, null, VulnerableAmount);
        }
    }
}

public sealed class NovelCrimsonValstrax : NovelSettingMonsterBase
{
    private const decimal FixedDamage = 100m;

    protected override string IdleMoveId => "NOVEL_CRIMSON_VALSTRAX_MOVE";
    protected override string VisualSceneFileName => "crimson_valstrax.tscn";

    protected override MoveState CreateMoveState()
    {
        return new MoveState(IdleMoveId, CrimsonValstraxMove, new SingleAttackIntent((int)FixedDamage));
    }

    private async Task CrimsonValstraxMove(IReadOnlyList<Creature> targets)
    {
        List<Creature> enemyTargets = GetLivingTargets(targets);
        if (enemyTargets.Count > 0)
        {
            await ValstraxDiveBombVfxCmd.PlayUntilImpactForTargets(enemyTargets);
            ThrowingPlayerChoiceContext context = new();
            await CreatureCmd.Damage(
                context,
                enemyTargets,
                FixedDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                base.Creature,
                null, null);
        }
    }
}
