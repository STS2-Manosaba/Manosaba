using BaseLib.Utils;
using manosaba.Characters.SaekiMiria;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.SaekiMiria.Cards
{
    [Pool(typeof(SaekiMiriaCardPool))]
    public class TraumaSharing : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
        public override IEnumerable<CardKeyword> CanonicalKeywords => [];
        public TraumaSharing() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (base.Owner.Creature is not { } ownerCreature || base.CombatState is not { } combatState || cardPlay.Target is not { } target)
            {
                return;
            }

            List<PowerModel> originalDebuffs = (from p in target.Powers
                                                where p.TypeForCurrentAmount == PowerType.Debuff
                                                select (PowerModel)p.ClonePreservingMutability()).ToList();
            foreach (Creature enemy in combatState.HittableEnemies)
            {
                if (enemy == target)
                {
                    continue;
                }

                foreach (PowerModel item in originalDebuffs)
                {
                    PowerModel? powerById = enemy.GetPowerById(item.Id);
                    if (powerById != null && powerById.InstanceType == PowerInstanceType.None)
                    {
                        Console.WriteLine($"TraumaSharing updating existing debuff {item.Id} on {enemy.Name} from {powerById.Amount} by {item.Amount} sourceType {item.GetType().Name} existingType {powerById.GetType().Name}");
                        DoHackyThingsForSpecificPowers(powerById);
                        await PowerCmd.ModifyAmount(choiceContext, powerById, item.Amount, ownerCreature, this);
                    }
                    else
                    {
                        PowerModel power = (PowerModel)item.ClonePreservingMutability();
                        if (powerById != null)
                        {
                            Console.WriteLine($"TraumaSharing applying cloned debuff {item.Id} to {enemy.Name} because existing power {powerById.GetType().Name} is {powerById.InstanceType}; clone type {power.GetType().Name} amount {item.Amount}");
                        }
                        else
                        {
                            Console.WriteLine($"TraumaSharing applying new debuff {item.Id} to {enemy.Name} type {power.GetType().Name} amount {item.Amount}");
                        }
                        DoHackyThingsForSpecificPowers(power);
                        await PowerCmd.Apply(choiceContext, power, enemy, item.Amount, ownerCreature, this);
                    }
                }
            }

        }


        private static void DoHackyThingsForSpecificPowers(PowerModel power)
        {
            if (power is ManosabaTemporaryStrengthPower temporaryStrengthPower)
            {
                temporaryStrengthPower.IgnoreNextInstance();
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
