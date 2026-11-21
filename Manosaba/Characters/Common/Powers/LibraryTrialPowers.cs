using Manosaba.Characters.Common.Monsters;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class LibraryExecutionerPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PerGuilt", 2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay) => dealer == Owner && target != null && props.IsPoweredAttack()
        ? target.GetPowerAmount<GuiltPower>() * DynamicVars["PerGuilt"].BaseValue : 0;
}

public sealed class LibraryInfiniteRevivalPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "res://Manosaba/images/powers/healing_power.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Round", LibraryWarden.TransformationRound), new DynamicVar("MaxHp", LibraryWarden.BasePhaseHpGain)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<GuiltPower>(), HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<TrueCulpritPower>()];

    public override bool ShouldDie(Creature creature) => creature != Owner || CombatManager.Instance.IsEnding;

    // IsEnding is computed as soon as the last primary enemy reaches zero HP,
    // before ShouldDie/AfterPreventingDeath run. Keep combat active through that
    // interval so both death prevention and CreatureCmd.Heal can finish.
    // Do not inspect IsEnding here: its getter invokes this hook.
    public override bool ShouldStopCombatFromEnding() => Owner.CombatState is { } combat
        && combat.Players.Any(player => player.Creature.IsAlive);

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        Flash();
        await CreatureCmd.Heal(Owner, Owner.MaxHp);
        if (Owner.Monster is LibraryWarden warden)
            await warden.SyncClues(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext());
    }
}

public sealed class LibraryGuardRevivalPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "res://Manosaba/images/powers/healing_power.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Rounds", LibraryGuard.RevivalDelay), new DynamicVar("MaxHp", LibraryGuard.HpPerRevival)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    // Keep Majoka, Guilt, Executioner, and Minion while the corpse waits to revive.
    public override bool ShouldPowerBeRemovedOnDeath(PowerModel power) => power.Owner != Owner;
    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => creature != Owner;
}

public abstract class LibraryCluePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Percent", 10), new DynamicVar("Round", LibraryWarden.TransformationRound)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];
}

public sealed class LibraryClueOnePower : LibraryCluePower { }
public sealed class LibraryClueTwoPower : LibraryCluePower { }
public sealed class LibraryClueThreePower : LibraryCluePower { }
public sealed class LibraryClueFourPower : LibraryCluePower { }
public sealed class LibraryClueFivePower : LibraryCluePower { }
public sealed class LibraryClueOneSolvedPower : LibraryCluePower { }
public sealed class LibraryClueTwoSolvedPower : LibraryCluePower { }
public sealed class LibraryClueThreeSolvedPower : LibraryCluePower { }
public sealed class LibraryClueFourSolvedPower : LibraryCluePower { }
public sealed class LibraryClueFiveSolvedPower : LibraryCluePower { }
