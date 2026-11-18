using BaseLib.Utils;
using manosaba.Characters.NikaidoHiro;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.NikaidoHiro.Cards
{
    [Pool(typeof(NikaidoHiroCardPool))]
    public class SeveringSpin : PathCustomCardModel
    {
        public override bool GainsBlock => true;

        private const int energyCost = 2;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Uncommon;
        private const TargetType targetType = TargetType.AllEnemies;
        private const bool shouldShowInCardLibrary = true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(3m, ValueProp.Move),
            new RepeatVar(4),
            new BlockVar(5m, ValueProp.Move)
        ];

        public SeveringSpin() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var combatState = CombatState;
            if (combatState == null)
                return;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponentsCompat(combatState)
                .Execute(choiceContext);

            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await SwitchToRandomDifferentStance();
        }

        private async Task SwitchToRandomDifferentStance()
        {
            var candidates = new List<int>();
            if (!Owner.Creature.HasPower<HighStancePower>())
                candidates.Add(0);
            if (!Owner.Creature.HasPower<MidStancePower>())
                candidates.Add(1);
            if (!Owner.Creature.HasPower<LowStancePower>())
                candidates.Add(2);

            if (candidates.Count == 0)
                return;

            int stanceChoice = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
            switch (stanceChoice)
            {
                case 0:
                    await ManosabaKeywords.ResolveHighStance(Owner.Creature, Owner.Creature, this);
                    break;
                case 1:
                    await ManosabaKeywords.ResolveMidStance(Owner.Creature, Owner.Creature, this);
                    break;
                default:
                    await ManosabaKeywords.ResolveLowStance(Owner.Creature, Owner.Creature, this);
                    break;
            }
        }

        public override async Task OnEnqueuePlayVfx(Creature? target)
        {
            if (CombatState?.Enemies.Count > 0)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NHellraiserVfx.Create(CombatState.Enemies[CombatState.Enemies.Count / 2]));
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Repeat.UpgradeValueBy(1m);
            DynamicVars.Block.UpgradeValueBy(2m);
        }
    }
}
