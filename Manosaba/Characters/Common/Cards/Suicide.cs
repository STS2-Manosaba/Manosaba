using BaseLib.Utils;
using manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using manosaba.Extensions;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(CommonCardPool))]
    public class Suicide : PathCustomCardModel
    {

        private const int energyCost = 1;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Common;
        private const TargetType targetType = TargetType.Self;
        private const bool shouldShowInCardLibrary = true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Execution];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];
        protected override IEnumerable<DynamicVar> CanonicalVars => BaseCanonicalVars;

        protected static IEnumerable<DynamicVar> BaseCanonicalVars =>
        [
            new DamageVar(3, ValueProp.Unpowered),
            new PowerVar<MajokaPower>(20),
        ];

        protected virtual bool DrawOnPlay => false;

        public Suicide() : this(energyCost, rarity, shouldShowInCardLibrary)
        {
        }

        protected Suicide(int energyCost, CardRarity rarity, bool shouldShowInCardLibrary)
            : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (DrawOnPlay)
            {
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            }

            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                ValueProp.Unpowered,
                base.Owner.Creature,
                this, null);
            await CommonActions.Apply<MajokaPower>(choiceContext, base.Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars["MajokaPower"].UpgradeValueBy(10m);
        }
    }

    [Pool(typeof(TokenCardPool))]
    public class KokoroSuicide : Suicide
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => [.. BaseCanonicalVars, new CardsVar(1)];

        protected override bool DrawOnPlay => true;

        public override string PortraitPath => "suicide.png".CardsImagePath();

        public override bool CanBeGeneratedInCombat => false;

        public override bool CanBeGeneratedByModifiers => false;

        public KokoroSuicide() : base(0, CardRarity.Token, false)
        {
        }
    }
}
