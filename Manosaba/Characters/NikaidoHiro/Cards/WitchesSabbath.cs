using BaseLib.Utils;
using manosaba.Characters.NikaidoHiro;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.NikaidoHiro.Cards
{
    [Pool(typeof(NikaidoHiroCardPool))]
    public class WitchesSabbath : PathCustomCardModel
    {
        protected override bool ShouldGlowGoldInternal => IsPlayable;
        private const int energyCost = 0;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.AllEnemies;
        private const bool shouldShowInCardLibrary = true;

        protected override bool IsPlayable => AreAllAlivePlayersMajokaThresholdMet(base.CombatState);
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(50m),
            new ExtraDamageVar(1m),
            new CalculatedDamageVar(ValueProp.Unpowered).WithMultiplier(delegate(CardModel card, Creature? _){
                int voteAmount = card.Owner.Creature.GetPowerAmount<SusPower>();
                int majokaAmount = GetTotalMajokaAmount(card.CombatState);

                return (50m + voteAmount * 5 + majokaAmount / 25) * (1 + 0.01m * majokaAmount) - 50m;
            }),
            new PowerVar<MajokaPower>(100)
            ];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<SusPower>()];
        public WitchesSabbath() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var combatState = base.CombatState;
            if (combatState == null)
                return;

            int majokaAmount = GetTotalMajokaAmount(combatState);
            int voteAmount = base.Owner.Creature.GetPowerAmount<SusPower>();

            foreach (Creature creature in combatState.Creatures)
            {
                int creatureMajoka = creature.GetPowerAmount<MajokaPower>();
                if (creatureMajoka > 0)
                {
                    await CommonActions.Apply<MajokaPower>(choiceContext, creature, this, -creatureMajoka);
                }
            }

            await CommonActions.Apply<SusPower>(choiceContext, base.Owner.Creature, this, -voteAmount);

            decimal damage = (DynamicVars.CalculationBase.BaseValue + voteAmount * 5 + majokaAmount / 25) * (1 + 0.01m * majokaAmount);

            await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .TargetingAllOpponentsCompat(combatState)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(20m);
        }

        private static int GetTotalMajokaAmount(ICombatState? combatState)
        {
            if (combatState == null)
            {
                return 0;
            }

            int total = 0;
            foreach (Creature creature in combatState.Creatures)
            {
                total += creature.GetPowerAmount<MajokaPower>();
            }

            return total;
        }

        private static bool AreAllAlivePlayersMajokaThresholdMet(ICombatState? combatState)
        {
            if (combatState == null)
            {
                return false;
            }

            bool hasAlivePlayer = false;
            foreach (var player in combatState.Players)
            {
                if (player?.Creature is not { IsAlive: true } creature)
                {
                    continue;
                }

                hasAlivePlayer = true;
                if (creature.GetPowerAmount<MajokaPower>() < 100)
                {
                    return false;
                }
            }

            return hasAlivePlayer;
        }
    }
}
