using BaseLib.Utils;
using manosaba.Characters.NikaidoHiro;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.NikaidoHiro.Cards
{
    [Pool(typeof(NikaidoHiroCardPool))]
    public class RonpaNikaidoHiro : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Uncommon;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(10m),
            new ExtraDamageVar(6m),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(delegate(CardModel card, Creature? _){
                int currentSus = card.Owner.Creature.GetPowerAmount<SusPower>();
                return (currentSus + 1) / 2;
            })];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<SusPower>()];
        public RonpaNikaidoHiro() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null)
                return;
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                 .FromCard(this, cardPlay)
                 .Targeting(target)
                 .Execute(choiceContext);
            int currentSus = base.Owner.Creature.GetPowerAmount<SusPower>();
            if (currentSus >= 1)
            {
                int susPowerCost = (currentSus + 1) / 2;
                await CommonActions.Apply<SusPower>(choiceContext, base.Owner.Creature, this, -susPowerCost);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(4);
            DynamicVars.ExtraDamage.UpgradeValueBy(1);
        }
    }
}
