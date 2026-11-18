using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
namespace Manosaba.Characters.SaekiMiria.Powers
{
    public class FriendlyPower : PathCustomPowerModel
    {
        private const string DamageReductionPercentVar = "DamageReductionPercent";
        private const decimal DamageTakenMultiplierPerStack = 0.8m;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override bool AllowNegative => false;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(DamageReductionPercentVar, 0m)];

        public override Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            _ = applier;
            _ = cardSource;
            RefreshDerivedValues();
            return Task.CompletedTask;
        }

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

            if (power != this)
            {
                return;
            }

            RefreshDerivedValues();
        }


        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (target != base.Owner)
            {
                return 1m;
            }
            decimal multiplier = 1m;
            // for each stack, decrease damage taken multiplicatively
            for (int i = 0; i < base.Amount; i++)
            {
                multiplier *= DamageTakenMultiplierPerStack;
            }
            return multiplier;
        }

        private void RefreshDerivedValues()
        {
            decimal multiplier = 1m;
            for (int i = 0; i < base.Amount; i++)
            {
                multiplier *= DamageTakenMultiplierPerStack;
            }

            DynamicVars[DamageReductionPercentVar].BaseValue = (1m - multiplier) * 100m;
            InvokeDisplayAmountChanged();
        }
    }
}
