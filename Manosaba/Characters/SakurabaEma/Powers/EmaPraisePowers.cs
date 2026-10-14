using BaseLib.Utils;
using Manosaba.Extensions;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public abstract class EmaPraisePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "res://Manosaba/images/powers/special_magic_power.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
}

public sealed class PureEmaPower : EmaPraisePower
{
    private bool _exhausting;
    private async Task ExhaustBadEnds(PlayerChoiceContext context)
    {
        if (_exhausting || Owner.Player == null) return;
        _exhausting = true;
        try
        {
            foreach (CardModel card in PileType.Hand.GetPile(Owner.Player).Cards.OfType<BadEnd>().ToList())
            {
                await CardCmd.Exhaust(context, card);
                await CommonActions.Apply<RegenPower>(context, Owner, null, 1);
            }
        }
        finally { _exhausting = false; }
    }
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => ExhaustBadEnds(new ThrowingPlayerChoiceContext());
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source) =>
        card.Owner == Owner.Player && card.Pile?.Type == PileType.Hand && card is BadEnd
            ? ExhaustBadEnds(new ThrowingPlayerChoiceContext()) : Task.CompletedTask;
}

public sealed class KindEmaPower : EmaPraisePower
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? 0.9m : 1m;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Owner.Player?.PlayerCombatState is not { } playerCombat || Owner.CombatState == null) return;
        decimal block = playerCombat.Energy * 3;
        if (block <= 0) return;
        foreach (Creature ally in Owner.CombatState.Allies.Where(c => c.IsPlayer && c.IsAlive))
            await CreatureCmd.GainBlock(ally, block, ValueProp.Unpowered, null);
    }
}

public sealed class GoodChildEmaPower : EmaPraisePower
{
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner.Player ? amount + 1 : amount;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) await CreatureCmd.GainBlock(Owner, 5, ValueProp.Unpowered, null);
    }
}

public sealed class DeathSentencePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "res://Manosaba/images/powers/guilt_power.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner && cardSource?.Keywords.Contains(ManosabaKeywords.Execution) == true ? 1.2m : 1m;
}
