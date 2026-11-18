using BaseLib.Utils;
using manosaba.Characters.SaekiMiria;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Characters.SaekiMiria.Helper;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.NikaidoHiro.Cards
{
    [Pool(typeof(SaekiMiriaCardPool))]
    public class MindSharing : PathCustomCardModel
    {
        private const int energyCost = 3;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.AllAllies;
        private const bool shouldShowInCardLibrary = true;
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

        
        public MindSharing() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Dictionary<ModelId, int> powerAmounts = new();
            Dictionary<ModelId, PowerModel> sourcePowers = new();

            var teammates = CombatState?.GetTeammatesOf(Owner.Creature)
                ?.Where(c => c != null && c.IsAlive && c.IsPlayer)
                .ToList();

            if (teammates == null || teammates.Count == 0)
            {
                Console.WriteLine("No valid teammates found for Mind Sharing.");
                return;
            }

            foreach (var teammate in teammates)
            {
                if (teammate?.Powers == null) continue;

                foreach (var item in teammate.Powers)
                {
                    if (item == null) continue;
                    if (ShouldIgnoreThisPower(item)) continue;

                    Console.WriteLine($"Collected power {item.Id} type {item.GetType().Name} amount {item.Amount} instanceType {item.InstanceType} from {teammate.Name}");

                    AddPowerToList(powerAmounts, item);
                    sourcePowers.TryAdd(item.Id, item);
                }
            }

            foreach (var teammate in teammates)
            {
                if (teammate == null) continue;

                Dictionary<ModelId, PowerModel> existingPowers = teammate.Powers?
                    .Where(power => power != null)
                    .GroupBy(power => power.Id)
                    .ToDictionary(group => group.Key, group => group.First()) ?? [];

                foreach (var kvp in powerAmounts)
                {
                    ModelId powerId = kvp.Key;
                    int targetAmount = kvp.Value;
                    existingPowers.TryGetValue(powerId, out PowerModel? existing);

                    if (existing != null && existing.InstanceType == PowerInstanceType.None)
                    {
                        Console.WriteLine($"Updating existing power {powerId} on {teammate.Name} from {existing.Amount} to target {targetAmount} type {existing.GetType().Name}");

                        DoHackyThingsForSpecificPowers(existing);

                        if (targetAmount > existing.Amount)
                        {
                            await PowerCmd.ModifyAmount(
                                choiceContext,
                                existing,
                                targetAmount - existing.Amount,
                                Owner.Creature,
                                this);
                        }
                    }
                    else
                    {
                        if (!sourcePowers.TryGetValue(powerId, out PowerModel? source)) continue;

                        if (existing != null && existing.InstanceType != PowerInstanceType.None)
                        {
                            Console.WriteLine($"Skipping instanced power {powerId} on {teammate.Name} because existing power {existing.GetType().Name} is {existing.InstanceType}");
                            continue;
                        }

                        var clone = source.ClonePreservingMutability() as PowerModel;
                        if (clone == null) continue;

                        Console.WriteLine($"Applying new power {powerId} to {teammate.Name} from source {source.GetType().Name} instanceType {source.InstanceType} amount {targetAmount}");

                        DoHackyThingsForSpecificPowers(clone);

                        await PowerCmd.Apply(
                            choiceContext,
                            clone,
                            teammate,
                            targetAmount,
                            Owner.Creature,
                            this);
                    }
                }
            }
        }

        private static void AddPowerToList(Dictionary<ModelId, int> powers, PowerModel power)
        {
            powers[power.Id] = powers.TryGetValue(power.Id, out int existingAmount)
                ? Math.Max(existingAmount, power.Amount)
                : power.Amount;
        }

        private static bool ShouldIgnoreThisPower(PowerModel power)
        {
            if (power is MegaCrit.Sts2.Core.Models.Powers.TemporaryStrengthPower or ManosabaTemporaryStrengthPower)
            {
                return true;
            }

            HashSet<Type> ignoredPowers = MiriaConstants.IgnoredPowers;
            return power != null && ignoredPowers.Contains(power.GetType());
        }

        private static void DoHackyThingsForSpecificPowers(PowerModel power)
        {
            if (power is ManosabaTemporaryStrengthPower manosabaTemporaryStrengthPower)
            {
                manosabaTemporaryStrengthPower.IgnoreNextInstance();
            }
        }

        protected override void OnUpgrade()
        {
            base.AddKeyword(CardKeyword.Retain);
        }
    }
}
