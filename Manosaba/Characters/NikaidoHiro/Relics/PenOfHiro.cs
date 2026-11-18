using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.NikaidoHiro.Relics
{

    [Pool(typeof(NikaidoHiroRelicPool))]
    public sealed class PenOfHiro : LevelingPathCustomRelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [.. base.ExtraHoverTips, HoverTipFactory.FromPower<SusPower>()];
        protected override int MaxRelicLevel => 5;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("HalfHpDamageBonusPercent", 10m),
            new DynamicVar("LowHpDamageBonusPercent", 20m),
        ];

        public override Task AfterObtained()
        {
            ApplyRelicLevelEffects();
            return Task.CompletedTask;
        }

        public override async Task BeforeCombatStart()
        {
            if (Owner.Creature == null)
                return;

            await CommonActions.Apply<MidStancePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, 1m);
        }

        public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            _ = choiceContext;
            _ = player;

            ApplyRelicLevelEffects();
            return Task.CompletedTask;
        }

        public override decimal ModifyDamageAdditive(
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            _ = target;
            _ = cardSource;

            Creature? ownerCreature = base.Owner.Creature;
            if (ownerCreature == null || dealer != ownerCreature)
            {
                return 0m;
            }

            if (!props.IsPoweredAttack())
            {
                return 0m;
            }

            decimal bonus = ownerCreature.GetPowerAmount<SusPower>() / 4m;
            bonus += amount * GetHpDamageBonusPercent(ownerCreature) / 100m;
            return bonus;
        }

        protected override void OnRelicLevelChanged(int oldLevel, int newLevel)
        {
            ApplyRelicLevelEffects();
        }

        private void ApplyRelicLevelEffects()
        {
            int level = RelicLevel;
            base.DynamicVars["HalfHpDamageBonusPercent"].BaseValue = level * 10m;
            base.DynamicVars["LowHpDamageBonusPercent"].BaseValue = level * 20m;
        }

        private decimal GetHpDamageBonusPercent(Creature creature)
        {
            if (creature.GetHpPercentRemaining() < 0.2d)
            {
                return base.DynamicVars["LowHpDamageBonusPercent"].BaseValue;
            }

            if (creature.GetHpPercentRemaining() < 0.5d)
            {
                return base.DynamicVars["HalfHpDamageBonusPercent"].BaseValue;
            }

            return 0m;
        }
    }
}
