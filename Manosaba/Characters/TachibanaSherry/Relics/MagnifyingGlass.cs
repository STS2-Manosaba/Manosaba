using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.TachibanaSherry.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.TachibanaSherry.Relics
{
    [Pool(typeof(TachibanaSherryRelicPool))]
    public sealed class MagnifyingGlass : LevelingPathCustomRelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        protected override int MaxRelicLevel => 5;

        public override async Task BeforeCombatStart()
        {
            if (Owner.Creature == null)
                return;

            if (Owner.Creature.GetPower<InvestigationMomentPower>() == null)
                await CommonActions.Apply<InvestigationMomentPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, 1);

            await CommonActions.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, RelicLevel);
        }

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (base.Owner.Creature != player.Creature)
                return;

            decimal majoka = base.Owner.Creature.GetPowerAmount<MajokaPower>();
            if (majoka > 0)
                await CommonActions.Apply<MajokaPower>(choiceContext, base.Owner.Creature, null, -majoka);
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (target != base.Owner.Creature)
                return 1m;

            if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
                return 1m;

            decimal strength = base.Owner.Creature.GetPowerAmount<StrengthPower>();
            if (strength <= 0m)
                return 1m;

            decimal maxPercent = RelicLevel >= 4 ? 0.60m : RelicLevel >= 2 ? 0.45m : 0.30m;
            decimal percent = Math.Min(strength * 0.05m, maxPercent);
            return 1m - percent;
        }
    }
}
