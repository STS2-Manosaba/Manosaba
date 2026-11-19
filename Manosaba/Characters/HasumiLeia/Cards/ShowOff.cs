using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using manosaba.Characters.HikamiMeruru;
using manosaba.Characters.SaekiMiria;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HasumiLeia.Cards
{
    [Pool(typeof(HasumiLeiaCardPool))]
    public class ShowOff : PathCustomCardModel
    {
        public override bool GainsBlock => true;
        private const int energyCost = 1;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Uncommon;
        private const TargetType targetType = TargetType.Self;
        private const bool shouldShowInCardLibrary = true;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m),new BlockVar(8, ValueProp.Move)];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

        public ShowOff() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CommonActions.Apply<Manosaba.Characters.HasumiLeia.Powers.ShowOffPower>(choiceContext, base.Owner.Creature, this, base.DynamicVars.Strength.BaseValue);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3);
        }
    }
}
