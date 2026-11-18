using BaseLib.Extensions;
using BaseLib.Utils;
using Manosaba.Characters.Common.Monsters;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Characters.SaekiMiria.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.HasumiLeia.Relics
{

    [Pool(typeof(HasumiLeiaRelicPool))]
    public sealed class Rapier : LevelingPathCustomRelicModel
    {
        private static readonly HashSet<string> SwordRelicTypeNames =
        [
            // Vanilla relics
            "SwordOfJade",
            "SwordOfStone",

            // Mod relics
            "RitualSword",
            "RitualSwordBloodied",
        ];

        public override RelicRarity Rarity => RelicRarity.Starter;
        protected override int MaxRelicLevel => 5;

        private decimal basePercentage = 0.6m;
        private Creature? _lastReflectedDealer;
        private bool _pendingVigorClear;


        public override Task AfterObtained()
        {
            ApplyRelicLevelEffects();
            return Task.CompletedTask;
        }

        public override async Task BeforeCombatStart()
        {
            if (Owner?.Creature == null)
            {
                return;
            }

            // Leia gains Two Swords at combat start if she has any sword relic.
            if (Owner.Character is HasumiLeia && Owner.Creature.GetPowerAmount<SecondSwordPower>() <= 0m && HasAnySwordRelic(Owner.Relics))
            {
                await CommonActions.Apply<SecondSwordPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, 1m);
            }
        }

        public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            if (ReflectionDamageGuard.IsActive)
                return;

            if (RelicLevel >= 5
                && target == base.Owner.Creature
                && dealer != null
                && props.IsPoweredAttack_()
                && _pendingVigorClear
                && _lastReflectedDealer != null
                && dealer != _lastReflectedDealer)
            {
                await ConsumeCurrentVigor(choiceContext);
            }

            if (target == base.Owner.Creature && result.BlockedDamage > 0 && props.IsPoweredAttack_() && dealer != null)
            {
                decimal damage = decimal.Ceiling(result.BlockedDamage * basePercentage);
                int extraHits = target.GetPowerAmount<RapierMasteryPower>();
                int hits = 1 + Math.Max(0, extraHits);
                bool dealtPoweredReflectDamage = false;

                await ReflectionDamageGuard.Run(async () =>
                {
                    for (int i = 0; i < hits; i++)
                    {
                        if (!dealer.IsAlive)
                            return;
                        if (cardSource is Objection)
                            return;
                        if (dealer.IsPlayer && cardSource == null)
                            return;

                        ValueProp parryProps = RelicLevel >= 5 ? ValueProp.Move : ValueProp.Unpowered;
                        Creature? parryDealer = Owner?.Creature;
                        if (parryProps.HasFlag(ValueProp.Move) && IsEntomancer(dealer))
                        {
                            parryDealer = null;
                        }

                        await CreatureCmd.Damage(choiceContext, dealer, damage, parryProps, parryDealer, null, null);
                        if (parryProps.HasFlag(ValueProp.Move))
                        {
                            dealtPoweredReflectDamage = true;
                        }
                    }
                });

                if (RelicLevel >= 5 && dealtPoweredReflectDamage)
                {
                    _pendingVigorClear = true;
                    _lastReflectedDealer = dealer;
                }
            }
        }

        private static bool IsEntomancer(Creature creature)
        {
            const string entomancerTypeName = "Entomancer";
            return creature.Monster?.GetType().Name == entomancerTypeName
                || creature.Monster?.GetType().Name == entomancerTypeName;
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            _ = creatures;

            if (side == base.Owner.Creature.Side)
            {
                _pendingVigorClear = false;
                _lastReflectedDealer = null;
                return;
            }

            if (RelicLevel < 5 || !_pendingVigorClear)
                return;

            await ConsumeCurrentVigor(choiceContext);
        }

        private async Task ConsumeCurrentVigor(PlayerChoiceContext choiceContext)
        {
            decimal vigor = base.Owner.Creature.GetPowerAmount<VigorPower>();
            if (vigor > 0m)
            {
                await CommonActions.Apply<VigorPower>(choiceContext, base.Owner.Creature, null, -vigor);
            }

            _pendingVigorClear = false;
            _lastReflectedDealer = null;
        }


        protected override void OnRelicLevelChanged(int oldLevel, int newLevel)
        {
            ApplyRelicLevelEffects();
        }

        private void ApplyRelicLevelEffects()
        {
            
            int level = RelicLevel;
            basePercentage = 0.6m + (level - 1) * 0.1m; // Increases by 10% each level, starting at 60% at level 1, 100% at level 5
        }

        private static bool HasAnySwordRelic(IEnumerable<RelicModel>? relics)
        {
            if (relics == null)
            {
                return false;
            }

            foreach (RelicModel relic in relics)
            {
                string? name = relic?.GetType().Name;
                if (name != null && SwordRelicTypeNames.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
