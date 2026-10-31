using BaseLib.Abstracts;
using BaseLib.Utils;
using manosaba.Extensions;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Powers;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Monsters;

public abstract class LibraryTrialMonster : MonsterModel, ICustomModel
{
    private Player? _defeatingPlayer;
    public override bool IsHealthBarVisible => true;

    protected decimal ScaleAddedHp(int baseHp) => Creature.ScaleHpForMultiplayer(baseHp,
        CombatState.Encounter, CombatState.Players.Count, CombatState.RunState.CurrentActIndex);

    public override Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target == Creature && result.WasTargetKilled)
            _defeatingPlayer = dealer?.Player ?? dealer?.PetOwner ?? cardSource?.Owner;
        return Task.CompletedTask;
    }

    protected async Task CreditDefeat(PlayerChoiceContext context)
    {
        Player? player = _defeatingPlayer;
        _defeatingPlayer = null;
        if (player?.Creature.IsAlive == true)
            await CommonActions.Apply<GuiltPower>(context, player.Creature, null, 1);
    }
}

public sealed class LibraryWarden : LibraryTrialMonster
{
    public const int TransformationRound = 8;
    public const int BasePhaseHpGain = 220;
    private int _defeats, _completed, _displayed, _settled, _observedRound;
    private bool _transformed, _roundHadDamage, _syncing;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 16, 13);
    public override int MaxInitialHp => MinInitialHp;
    private int PhaseHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, BasePhaseHpGain);
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);
    protected override string VisualsPath => "event_warden.tscn".MonsterScenePath();

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState("LIBRARY_WARDEN_ATTACK", Attack, new SingleAttackIntent(AttackDamage));
        var suspicion = new MoveState("LIBRARY_WARDEN_SUSPICION", GiveSuspicion, new DebuffIntent());
        var guilt = new MoveState("LIBRARY_WARDEN_GUILT", GiveGuilt, new DebuffIntent());
        // PrepareForNextTurn rolls a new move AFTER BeforeSideTurnStart. Route
        // every roll through the round schedule instead of forcing a move in
        // that hook and then letting the engine overwrite it with an attack.
        var schedule = new ConditionalBranchState("LIBRARY_WARDEN_SCHEDULE");
        schedule.AddState(suspicion, () => CombatState.RoundNumber is 2 or 5);
        schedule.AddState(guilt, () => CombatState.RoundNumber >= TransformationRound
            && (CombatState.RoundNumber - TransformationRound) % 3 == 0);
        schedule.AddState(attack, () => true);
        attack.FollowUpState = schedule;
        suspicion.FollowUpState = schedule;
        guilt.FollowUpState = schedule;
        return new MonsterMoveStateMachine([schedule, attack, suspicion, guilt], schedule);
    }

    public override async Task AfterAddedToRoom()
    {
        var context = new ThrowingPlayerChoiceContext();
        await CommonActions.Apply<LibraryExecutionerPower>(context, Creature, null, 1);
        await CommonActions.Apply<LibraryInfiniteRevivalPower>(context, Creature, null, 1);
        for (int clue = 0; clue < 5; clue++) await SetCluePower(context, clue, false);
    }

    private Task Attack(IReadOnlyList<Creature> targets) => DamageCmd.Attack(AttackDamage)
        .FromMonster(this).WithHitFx("vfx/vfx_attack_slash").Execute(null);

    private async Task GiveSuspicion(IReadOnlyList<Creature> targets)
    {
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId))
            for (int i = 0; i < 2; i++)
                await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<GuiltSuspicion>(player), PileType.Hand, player);
    }

    private async Task GiveGuilt(IReadOnlyList<Creature> targets)
    {
        var context = new ThrowingPlayerChoiceContext();
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId))
            await CommonActions.Apply<GuiltPower>(context, player.Creature, null, 3);
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !Creature.IsAlive || _observedRound == combatState.RoundNumber) return;
        if (_observedRound > 0 && !_roundHadDamage) _completed |= 1 << 4;
        _observedRound = combatState.RoundNumber;
        _roundHadDamage = false;
        if (!_transformed && combatState.RoundNumber >= TransformationRound)
        {
            // Remove immortality before healing and before any clue damage is resolved.
            _transformed = true;
            await PowerCmd.Remove(Creature.GetPower<LibraryInfiniteRevivalPower>()!);
            await CreatureCmd.GainMaxHp(Creature, ScaleAddedHp(PhaseHp));
            await CreatureCmd.Heal(Creature, Creature.MaxHp);
            await CommonActions.Apply<StrengthPower>(context, Creature, null, 10);
            var players = combatState.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId).ToList();
            int highest = players.Select(p => p.Creature.GetPowerAmount<GuiltPower>()).DefaultIfEmpty(0).Max();
            foreach (var player in players.Where(p => p.Creature.GetPowerAmount<GuiltPower>() == highest))
                await CommonActions.Apply<TrueCulpritPower>(context, player.Creature, null, 1);
        }
        await SyncClues(context);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(context, dealer, result, props, target, cardSource);
        if (target.IsPlayer && result.UnblockedDamage > 0) _roundHadDamage = true;
        if ((dealer?.Player != null || dealer?.PetOwner != null || cardSource?.Owner != null)
            && target.Side == CombatSide.Enemy && result.UnblockedDamage + result.OverkillDamage >= 80)
            _completed |= 1 << 3;
        // Lethal damage must finish death prevention before clue damage can run.
        if (Creature.IsAlive) await SyncClues(context);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is GuiltPower && power.Owner.IsPlayer && Creature.IsAlive) await SyncClues(context);
    }

    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Creature)
        {
            if (CombatState.RoundNumber < TransformationRound && ++_defeats >= 3) _completed |= 1;
            await CreditDefeat(context);
        }
    }

    internal async Task RecordGuardDefeat(PlayerChoiceContext context)
    {
        _completed |= 1 << 1;
        await SyncClues(context);
    }

    public async Task SyncClues(PlayerChoiceContext context)
    {
        if (_syncing || !Creature.IsAlive) return;
        _syncing = true;
        try
        {
            if (CombatState.Players.Sum(p => p.Creature.GetPowerAmount<GuiltPower>()) > 20) _completed |= 1 << 2;
            for (int clue = 0; clue < 5; clue++)
            {
                int bit = 1 << clue;
                if ((_completed & bit) == 0 || (_settled & bit) != 0) continue;
                if ((_displayed & bit) == 0)
                {
                    _displayed |= bit;
                    await RemoveCluePower(clue, false);
                    await SetCluePower(context, clue, true);
                }
                if (_transformed && Creature.IsAlive)
                {
                    _settled |= bit;
                    await RemoveCluePower(clue, true);
                    await CreatureCmd.Damage(context, Creature, Creature.MaxHp * 0.10m,
                        ValueProp.Unblockable | ValueProp.Unpowered, null, null);
                }
            }
        }
        finally { _syncing = false; }
    }

    private PowerModel CluePower(int clue, bool solved) => (clue, solved) switch
    {
        (0, false) => ModelDb.Power<LibraryClueOnePower>(), (0, true) => ModelDb.Power<LibraryClueOneSolvedPower>(),
        (1, false) => ModelDb.Power<LibraryClueTwoPower>(), (1, true) => ModelDb.Power<LibraryClueTwoSolvedPower>(),
        (2, false) => ModelDb.Power<LibraryClueThreePower>(), (2, true) => ModelDb.Power<LibraryClueThreeSolvedPower>(),
        (3, false) => ModelDb.Power<LibraryClueFourPower>(), (3, true) => ModelDb.Power<LibraryClueFourSolvedPower>(),
        (4, false) => ModelDb.Power<LibraryClueFivePower>(), _ => ModelDb.Power<LibraryClueFiveSolvedPower>(),
    };

    private Task SetCluePower(PlayerChoiceContext context, int clue, bool solved) =>
        PowerCmd.Apply(context, CluePower(clue, solved).ToMutable(), Creature, 1, Creature, null);

    private async Task RemoveCluePower(int clue, bool solved)
    {
        var power = Creature.Powers.FirstOrDefault(p => p.Id == CluePower(clue, solved).Id);
        if (power != null) await PowerCmd.Remove(power);
    }
}

public sealed class LibraryGuard : LibraryTrialMonster
{
    public const int RevivalDelay = 2;
    public const int HpPerRevival = 3;
    private int _reviveRound;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 28, 24);
    public override int MaxInitialHp => MinInitialHp;
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    protected override string VisualsPath => "basement_guardian.tscn".EnemyScenePath();
    public override string? HurtSfx => "event:/Manosaba/audio/monsters/jailer_hurt.ogg";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState("LIBRARY_GUARD_ATTACK", Attack, new SingleAttackIntent(AttackDamage));
        attack.FollowUpState = attack;
        return new MonsterMoveStateMachine([attack], attack);
    }

    private Task Attack(IReadOnlyList<Creature> targets) => DamageCmd.Attack(AttackDamage)
        .FromMonster(this).WithHitFx("vfx/vfx_attack_slash").Execute(null);

    public override async Task AfterAddedToRoom()
    {
        var context = new ThrowingPlayerChoiceContext();
        await CommonActions.Apply<LibraryGuardRevivalPower>(context, Creature, null, 1);
        await CommonActions.Apply<MinionPower>(context, Creature, null, 1);
        await CommonActions.Apply<MajokaPower>(context, Creature, null, 100);
        await CommonActions.Apply<GuiltPower>(context, Creature, null, 1);
        await CommonActions.Apply<LibraryExecutionerPower>(context, Creature, null, 1);
    }

    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature || wasRemovalPrevented) return;
        _reviveRound = CombatState.RoundNumber + RevivalDelay;
        // Credit the killing blow before a newly solved clue can kill the warden
        // and start combat cleanup (which would otherwise reject the Guilt gain).
        await CreditDefeat(context);
        foreach (var warden in CombatState.Enemies.Select(c => c.Monster).OfType<LibraryWarden>().ToList())
            await warden.RecordGuardDefeat(context);
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Creature.IsAlive || _reviveRound == 0
            || combatState.RoundNumber < _reviveRound
            || !combatState.Enemies.Any(c => c.Monster is LibraryWarden && c.IsAlive)) return;
        _reviveRound = 0;
        await CreatureCmd.GainMaxHp(Creature, ScaleAddedHp(HpPerRevival));
        await CreatureCmd.Heal(Creature, Creature.MaxHp);
    }
}
