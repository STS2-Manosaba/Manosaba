using BaseLib.Utils;
using manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.JogasakiNoah.Cards
{
    [Pool(typeof(JogasakiNoahCardPool))]
    public class ColorMix : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Basic;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(5m),
            new ExtraDamageVar(5m),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) => {
                if (card.Owner?.PlayerCombatState == null)
                    return 0m;
                HashSet<Type> paintOrbTypes = JogasakiNoahOrbPool.AllOrbs
                .Select(o => o.GetType())
                .ToHashSet();
                int distinctColorCount = card.Owner.PlayerCombatState.OrbQueue.Orbs
                    .Select(o => o.GetType())
                    .Where(paintOrbTypes.Contains)
                    .Distinct()
                    .Count();

                decimal damageMultiplier = distinctColorCount switch
                {
                    >= 6 => 9m,
                    >= 5 => 5m,
                    >= 4 => 2m,
                    >= 3 => 1m,
                    _ => 0m
                };
                return damageMultiplier;
            })];

        public ColorMix() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target is not { } target)
            {
                return;
            }

            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext);
        }


        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
