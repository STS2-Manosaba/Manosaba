using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.HasumiLeia;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using HasumiLeiaCharacter = manosaba.Characters.HasumiLeia.HasumiLeia;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(CommonCardPool))]
    public class ForgeSpear : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.Self;
        private const bool shouldShowInCardLibrary = true;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<SimpleSpear>()];

        protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

        public ForgeSpear() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var combatState = base.CombatState;
            if (combatState == null)
                return;

            List<CardModel> cards = new List<CardModel>();
            cards.AddRange(SSArrow.Create(base.Owner, base.DynamicVars.Cards.IntValue, combatState));
            if (base.Owner.Character is not HasumiLeiaCharacter)
                cards.AddRange(SSRapier.Create(base.Owner, base.DynamicVars.Cards.IntValue, combatState));
            cards.AddRange(SSRibbon.Create(base.Owner, base.DynamicVars.Cards.IntValue, combatState));
            cards.AddRange(SSBroom.Create(base.Owner, base.DynamicVars.Cards.IntValue, combatState));
            IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(results);
        }

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}
