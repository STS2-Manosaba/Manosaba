using BaseLib.Utils;
using manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using manosaba.Characters.SakurabaEma.Powers;
using manosaba.Characters.SakurabaEma.Relics;

namespace Manosaba.Characters.Common.Cards
{
    [Pool(typeof(CommonCardPool))]
    public class Guillotine : PathCustomCardModel
    {
        private const int energyCost = 2;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Common;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Execution];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14, ValueProp.Move), new DynamicVar("BeheadedThreshold", 10m)];

        public Guillotine() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target is not { } target)
            {
                return;
            }

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);

            decimal threshold = DynamicVars["BeheadedThreshold"].BaseValue + (target.HasPower<DeathSentencePower>() ? 5m : 0m);
            if (target.IsAlive && target.GetHpPercentRemaining() < (double)(threshold / 100m))
            {
                Owner.Relics.OfType<LegRing>().FirstOrDefault()?.RecordExecution(target);
                await DoomPower.DoomKill([target]);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(4m);
            base.DynamicVars["BeheadedThreshold"].UpgradeValueBy(5m);
        }
    }
}
