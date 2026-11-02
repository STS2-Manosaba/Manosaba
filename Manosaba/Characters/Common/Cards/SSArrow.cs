using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Models.CardPools;
using HasumiLeiaCharacter = manosaba.Characters.HasumiLeia.HasumiLeia;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(TokenCardPool))]
    public class SSArrow : PathCustomCardModel
    {

        private const int energyCost = -1;
        private const CardType type = CardType.Quest;
        private const CardRarity rarity = CardRarity.Token;
        private const TargetType targetType = TargetType.None;
        private const bool shouldShowInCardLibrary = true;

        public override bool CanBeGeneratedInCombat => false;
        public override bool CanBeGeneratedByModifiers => false;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<SimpleSpear>()];
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];

        public SSArrow() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (Owner?.Creature?.CombatState is not { } combatState)
            {
                return;
            }

            CardModel? token1, token2, token3, token4 = null;
            bool requiresRapier = Owner.Character is not HasumiLeiaCharacter;

            if ((token1 = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c is SSArrow)) == null)
                return;
            if ((token2 = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c is SSBroom)) == null)
                return;
            if ((token3 = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c is SSRibbon)) == null)
                return;
            if (requiresRapier && (token4 = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c is SSRapier)) == null)
                return;
            await CardPileCmd.RemoveFromCombat(token1);
            await CardPileCmd.RemoveFromCombat(token2);
            await CardPileCmd.RemoveFromCombat(token3);
            if (token4 is not null)
                await CardPileCmd.RemoveFromCombat(token4);
            CardModel craftedSpear = SimpleSpear.Create(Owner, combatState);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(craftedSpear, PileType.Hand, Owner);
            CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.HorizontalLayout);
            await CheckForAdditionalEffect();
        }


        public static IEnumerable<SSArrow> Create(Player owner, int amount, ICombatState combatState)
        {
            List<SSArrow> list = new List<SSArrow>();
            for (int i = 0; i < amount; i++)
            {
                list.Add(combatState.CreateCard<SSArrow>(owner));
            }

            return list;
        }

        public async Task CheckForAdditionalEffect()
        {
            if (Owner?.Creature?.CombatState is not { } combatState)
                return;


            PortableFletchingStationPower? portableFletchingStation = Owner.Creature.GetPower<PortableFletchingStationPower>();
            int portableFletchingStationAmount = portableFletchingStation?.Amount ?? 0;
            if (portableFletchingStationAmount <= 0)
                return;

            int procChancePercent = portableFletchingStation!.DynamicVars[PortableFletchingStationPower.ProcChanceVar].IntValue;
            for (int i = 0; i < portableFletchingStationAmount; i++)
            {
                if (Owner.RunState.Rng.CombatCardGeneration.NextInt(100) >= procChancePercent)
                    continue;

                await portableFletchingStation.TriggerFlash();
                CardModel bonusSpear = SimpleSpear.Create(Owner, combatState);
                await CardPileCmd.AddGeneratedCardToCombat(bonusSpear, PileType.Hand, Owner);
            }
        }
    }
}
