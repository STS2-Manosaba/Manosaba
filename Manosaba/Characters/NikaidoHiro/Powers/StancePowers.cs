using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.NikaidoHiro.Powers
{
    public abstract class StancePowerBase : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        protected abstract Task RemoveOtherStances();

        protected bool HasOtherStance()
        {
            if (this is HighStancePower)
            {
                return Owner.HasPower<MidStancePower>() || Owner.HasPower<LowStancePower>();
            }

            if (this is MidStancePower)
            {
                return Owner.HasPower<HighStancePower>() || Owner.HasPower<LowStancePower>();
            }

            return Owner.HasPower<HighStancePower>() || Owner.HasPower<MidStancePower>();
        }

        protected static bool ShouldAffectDamage(ValueProp props)
        {
            return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
        }

        protected static bool ShouldAffectBlock(ValueProp props)
        {
            return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
        }

        public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            bool switchedFromAnotherStance = HasOtherStance();
            await RemoveOtherStances();

            if (switchedFromAnotherStance && Owner.HasPower<FluxPower>() && Owner.Player != null)
            {
                Flash();
                await PlayerCmd.GainEnergy(1m, Owner.Player);
            }
        }
    }

    public sealed class HighStancePower : StancePowerBase
    {
        protected override async Task RemoveOtherStances()
        {
            await PowerCmd.Remove<MidStancePower>(Owner);
            await PowerCmd.Remove<LowStancePower>(Owner);
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (!ShouldAffectDamage(props))
            {
                return 1m;
            }

            if (dealer == Owner)
            {
                return 1.2m;
            }

            return 1m;
        }

        public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
        {
            if (target != Owner)
            {
                return 1m;
            }

            return ShouldAffectBlock(props) ? 0.8m : 1m;
        }
    }

    public sealed class MidStancePower : StancePowerBase
    {
        protected override async Task RemoveOtherStances()
        {
            await PowerCmd.Remove<HighStancePower>(Owner);
            await PowerCmd.Remove<LowStancePower>(Owner);
        }
    }

    public sealed class LowStancePower : StancePowerBase
    {
        protected override async Task RemoveOtherStances()
        {
            await PowerCmd.Remove<HighStancePower>(Owner);
            await PowerCmd.Remove<MidStancePower>(Owner);
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (!ShouldAffectDamage(props))
            {
                return 1m;
            }

            if (dealer == Owner)
            {
                return 0.8m;
            }

            return 1m;
        }

        public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
        {
            if (target != Owner)
            {
                return 1m;
            }

            return ShouldAffectBlock(props) ? 1.2m : 1m;
        }
    }
}
