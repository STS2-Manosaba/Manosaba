using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.TonoHanna.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Powers
{
    public class EmaPuppetPower : PathCustomPowerModel
    {
        private const decimal HiroBonusPerStack = 2m;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(9m, ValueProp.Unpowered),
            new DamageVar("HiroBonus", HiroBonusPerStack, ValueProp.Unpowered),
        ];

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
            if (power == this)
            {
                SyncHiroBonus();
            }
        }

        private void SyncHiroBonus()
        {
            decimal damagePerStack = DynamicVars.Damage.BaseValue;
            if (damagePerStack <= 0m)
            {
                return;
            }

            DynamicVars["HiroBonus"].BaseValue = HiroBonusPerStack * Amount / damagePerStack;
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner.Player)
                return;

            if (!cardPlay.Card.Tags.Contains(ManosabaCardTags.Puppet))
                return;

            if (Owner.CombatState == null)
                return;

            ICombatState combat = Owner.CombatState;
            await CreatureCmd.Damage(context, combat.HittableEnemies, Amount, ValueProp.Unpowered, Owner, null, null);

            if (cardPlay.Card is HiroPuppet)
            {
                decimal hiroBonus = DynamicVars["HiroBonus"].BaseValue;
                if (hiroBonus > 0m)
                {
                    await CreatureCmd.Damage(
                        context,
                        combat.HittableEnemies,
                        hiroBonus,
                        ValueProp.Unpowered,
                        Owner,
                        null, null);
                }
            }
        }
    }
}
