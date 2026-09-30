using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public abstract class CappedCorpsePhotoPower : PathCustomPowerModel
{
    protected abstract int MaximumStacks { get; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount > MaximumStacks) SetAmount(MaximumStacks);
        return Task.CompletedTask;
    }
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (ReferenceEquals(power, this) && Amount > MaximumStacks) SetAmount(MaximumStacks);
        return base.AfterPowerAmountChanged(context, power, amount, applier, cardSource);
    }
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || canonicalPower.GetType() != GetType() || amount <= 0) return false;
        modifiedAmount = Math.Min(amount, Math.Max(0, MaximumStacks - Amount));
        return modifiedAmount != amount;
    }
}

public sealed class RedButterflyPower : CappedCorpsePhotoPower
{
    protected override int MaximumStacks => 5;
    public override string CustomPackedIconPath => "res://Manosaba/images/powers/liquid_manipulation_power.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? -Math.Min(Amount, 5) : 0;
}

public sealed class AlternationPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // This hook only runs during actual HP-loss resolution, never for damage previews.
    // Native redirection sends the unblocked damage back to the attacking enemy without recursive attacks.
    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        if (target != Owner || amount <= 0 || dealer is not { IsAlive: true } || dealer.Side == Owner.Side || Owner.Player == null)
            return target;
        return Owner.Player.RunState.Rng.CombatTargets.NextInt(100) < 20 ? dealer : target;
    }
}

public sealed class LightnessPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && cardSource?.Type == CardType.Attack && props.HasFlag(ValueProp.Move) ? Owner.GetPowerAmount<DexterityPower>() : 0;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side) await PowerCmd.Remove(this);
    }
}

public sealed class WarmFlamePower : CappedCorpsePhotoPower
{
    protected override int MaximumStacks => 10;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BurnPower>()];
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Owner.CombatState == null || Owner.Player == null) return;
        var enemies = Owner.CombatState.GetOpponentsOf(Owner).Where(enemy => enemy.IsHittable).ToList();
        if (enemies.Count == 0) return;
        Creature? target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target != null) await CommonActions.Apply<BurnPower>(context, target, null, Amount);
    }
}

public sealed class ResistancePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [WitchEncyclopediaState.HoverTip];
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        await CardPileCmd.Draw(context, 1, player);
        await WitchEncyclopediaState.GenerateRandomEvidence(player);
    }
}
