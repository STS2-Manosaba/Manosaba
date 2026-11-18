using BaseLib.Utils;
using manosaba.Characters.Common;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(CommonCardPool))]
    public class SimpleSpear : PathCustomCardModel
    {
        private const int energyCost = 0;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Ancient;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = false;
        public override bool CanBeGeneratedInCombat => false;
        public override bool CanBeGeneratedByModifiers => false;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(50, ValueProp.Move)];
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

        public SimpleSpear() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        public static SimpleSpear Create(Player owner, ICombatState combatState)
        {
            SimpleSpear card = combatState.CreateCard<SimpleSpear>(owner);
            if (owner.Creature.GetPowerAmount<SpearMasteryPower>() > 0m && !card.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }

            return card;
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null)
                return;

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(25m);
        }
    }
}
