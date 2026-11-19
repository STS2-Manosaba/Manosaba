using BaseLib.Utils;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using manosaba.Extensions;
using Manosaba.Characters.Common.Encounters;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.Common.Monsters;

public sealed class BasementGuardian : MonsterModel, ICustomModel
{
    private const int OpeningMajoka = 100;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 108, 101);
    public override int MaxInitialHp => MinInitialHp;

    protected override string VisualsPath => "basement_guardian.tscn".EnemyScenePath();

    // Same base damage as FlailKnight (MysteriousKnight's parent).
    private int FlailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int RamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = [];
        MoveState warChant = new("BASEMENT_GUARDIAN_WAR_CHANT", WarChantMove, new BuffIntent());
        MoveState flail = new("BASEMENT_GUARDIAN_FLAIL", FlailMove, new MultiAttackIntent(FlailDamage, 2));
        MoveState ram = new("BASEMENT_GUARDIAN_RAM", RamMove, new SingleAttackIntent(RamDamage));
        RandomBranchState random = (RandomBranchState)(ram.FollowUpState = flail.FollowUpState = warChant.FollowUpState = new RandomBranchState("RAND"));
        random.AddBranch(warChant, MoveRepeatType.CannotRepeat);
        random.AddBranch(flail, 2);
        random.AddBranch(ram, 2);
        states.Add(warChant);
        states.Add(flail);
        states.Add(ram);
        states.Add(random);
        return new MonsterMoveStateMachine(states, ram);
    }

    public override async Task AfterAddedToRoom()
    {
        if (Creature.CombatState?.Encounter is not BasementGuardianEventEncounter)
        {
            return;
        }

        void FlipIfReady()
        {
            var body = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Body;
            if (body != null)
            {
                body.Scale *= new Vector2(-1f, 1f);
            }
        }

        FlipIfReady();
        Callable.From(FlipIfReady).CallDeferred();

        decimal majokaToApply = OpeningMajoka - Creature.GetPowerAmount<MajokaPower>();
        if (majokaToApply > 0m)
        {
            await CommonActions.Apply<MajokaPower>(new ThrowingPlayerChoiceContext(), Creature, null, majokaToApply);
        }
    }

    private async Task WarChantMove(IReadOnlyList<Creature> targets)
    {
        _ = targets;
        await CommonActions.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, null, 2m);
    }

    private Task FlailMove(IReadOnlyList<Creature> targets)
    {
        return DamageCmd.Attack(FlailDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private Task RamMove(IReadOnlyList<Creature> targets)
    {
        return DamageCmd.Attack(RamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
