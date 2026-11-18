using BaseLib.Utils;
using manosaba.Characters.HikamiMeruru;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using KurobeNanokaCharacter = manosaba.Characters.KurobeNanoka.KurobeNanoka;

namespace Manosaba.Characters.HikamiMeruru.Cards
{
    [Pool(typeof(HikamiMeruruCardPool))]
    public class ShotsFired : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Common;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];

        public ShotsFired() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null)
                return;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext);

            if (CombatState == null || Owner?.Creature == null)
            {
                return;
            }

            List<Creature> kurobeNanokas = CombatState.Creatures
                .Where(c => c != null && c.IsAlive && c.IsPlayer && c.Player?.Character is KurobeNanokaCharacter)
                .ToList();
            if (kurobeNanokas.Count == 0)
                return;

            Creature? randomKurobeNanoka = Owner.RunState.Rng.CombatTargets.NextItem(kurobeNanokas);
            if (randomKurobeNanoka == null)
            {
                return;
            }

            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), randomKurobeNanoka, 1m, ValueProp.Unpowered, Owner.Creature);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5);
        }
    }
}
