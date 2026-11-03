using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(KurobeNanokaCardPool))]
    public class DyingMessage : PathCustomCardModel
    {
        private const int energyCost = 0;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.Self;
        private const bool shouldShowInCardLibrary = true;
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2), new CardsVar(3)];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [base.EnergyHoverTip];
        public DyingMessage() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            DyingMessagePower? power = await CommonActions.Apply<DyingMessagePower>(choiceContext, base.Owner.Creature, this, 1m, silent: true);
            if (base.IsUpgraded) power?.Upgrade();

            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature,
                Owner.Creature.CurrentHp,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(2m);
        }

    }
}
