using BaseLib.Extensions;
using Manosaba.Characters.HikamiMeruru.Cards;
using Manosaba.Characters.JogasakiNoahCard.Cards;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Characters.SaekiMiria.Cards;
using Manosaba.Characters.TachibanaSherry.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace Manosaba.Characters.Common.Powers
{
    /// <summary>
    /// modified from flanking.
    /// Stacks does not multiply like the original flanking
    /// </summary>
    public sealed class IncomeDamageMultiplyPower : PathCustomPowerModel
    {
        private const string MultiplierKey = "Multiplier";

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(MultiplierKey, 2m)];

        public override Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            _ = applier;
            _ = cardSource;
            UpdateMultiplierVar();
            return Task.CompletedTask;
        }

        public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            _ = amount;
            _ = applier;
            _ = cardSource;

            if (power == this)
            {
                UpdateMultiplierVar();
            }

            return Task.CompletedTask;
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (target != base.Owner)
            {
                return 1m;
            }

            if (!props.IsPoweredAttack_())
            {
                return 1m;
            }


            return 1m+base.Amount;
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (side == base.Owner.Side)
            {
                await PowerCmd.Remove(this);
            }
        }

        private void UpdateMultiplierVar()
        {
            if (DynamicVars.TryGetValue(MultiplierKey, out DynamicVar? multiplierVar))
            {
                multiplierVar.BaseValue = 1m + Amount;
            }
        }
    }


}
