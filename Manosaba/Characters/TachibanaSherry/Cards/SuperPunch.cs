using BaseLib.Utils;
using manosaba.Characters.TachibanaSherry;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.TachibanaSherry.Cards
{
    [Pool(typeof(TachibanaSherryCardPool))]
    public class SuperPunch : PathCustomCardModel
    {
        private const int energyCost = 0;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Ancient;
        private const TargetType targetType = TargetType.AllEnemies;
        private const bool shouldShowInCardLibrary = true;

        public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Mahou, CardKeyword.Eternal];

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [
            HoverTipFactory.FromPower<MajokaPower>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(8),
            new ExtraDamageVar(15),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                static (CardModel card, Creature? _) =>
                    (card.Owner?.Creature?.GetPowerAmount<MajokaPower>() ?? 0) >= 100 ? 1m : 0m),
            new PowerVar<StrengthPower>(5m)
        ];

        protected override bool ShouldGlowGoldInternal => base.Owner.Creature.GetPowerAmount<MajokaPower>() >= 100;

        public SuperPunch() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (base.CombatState == null)
                return;

            await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
                .FromCard(this, cardPlay)
                .TargetingAllOpponentsCompat(base.CombatState)
                .Execute(choiceContext);

            if (base.Owner.Creature.GetPowerAmount<MajokaPower>() >= 100)
                await CommonActions.Apply<StrengthPower>(choiceContext, base.Owner.Creature, this, base.DynamicVars["StrengthPower"].BaseValue);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(3);
        }
    }
}
