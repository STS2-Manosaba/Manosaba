using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(StatusCardPool))]
    public class BadEnd : PathCustomCardModel
    {
        public override int MaxUpgradeLevel => 0;
        private const int energyCost = -1;
        private const CardType type = CardType.Status;
        private const CardRarity rarity = CardRarity.Status;
        private const TargetType targetType = TargetType.None;
        private const bool shouldShowInCardLibrary = true;
        public override bool HasTurnEndInHandEffect => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Unpowered)];
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

        public BadEnd() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        public static IEnumerable<BadEnd> Create(Player owner, int amount, ICombatState combatState)
        {
            List<BadEnd> list = new List<BadEnd>();
            for (int i = 0; i < amount; i++)
            {
                list.Add(combatState.CreateCard<BadEnd>(owner));
            }

            return list;
        }

        protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        {
            VfxCmd.PlayOnCreatureCenter(base.Owner.Creature, "vfx/vfx_bloody_impact");
            await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.Damage, this, null);
        }
    }
}
