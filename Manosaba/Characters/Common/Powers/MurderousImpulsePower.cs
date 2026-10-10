using System.Linq;
using Manosaba.Characters.HikamiMeruru.Powers;
using Manosaba.Multiplayer;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers
{
    public class MurderousImpulsePower : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => false;

        public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
        {
            if (base.Amount < 1 || Owner.GetPowerAmount<InhibitionPower>() >= 1)
            {
                return;
            }

            if (command.Attacker != Owner || command.ModelSource is not CardModel card || card.Owner != Owner.Player || card.Type != CardType.Attack)
            {
                return;
            }

            int totalDamage = command.Results.SelectMany(hit => hit).Sum(result => result.TotalDamage);
            if (totalDamage <= 0)
            {
                return;
            }

            if (Owner.CombatState == null)
            {
                return;
            }

            Creature[] validAllies = Owner.CombatState.Allies
                .Where(c => c != Owner && !c.IsDead && HasCombatNode(c))
                .ToArray();
            if (validAllies.Length == 0)
            {
                return;
            }

            Creature[] filteredAllies = ResolveTargets(Owner, validAllies);
            if (filteredAllies.Length == 0)
            {
                return;
            }

            decimal perStackMultiplier = ManosabaLobbyDifficultyState.GetMurderousImpulseAllyDamageMultiplierForGameplay();
            decimal allyDamage = totalDamage * base.Amount * perStackMultiplier;
            if (allyDamage <= 0m)
            {
                return;
            }

            await Cmd.CustomScaledWait(0.1f, 0.2f);
            if (base.Owner.Player?.Creature is not { } ownerCreature)
            {
                return;
            }

            Creature? ally = base.Owner.Player.RunState.Rng.CombatTargets.NextItem(filteredAllies);
            if (ally == null)
            {
                return;
            }

            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), ally, allyDamage, ValueProp.Unpowered, ownerCreature);
        }

        internal static Creature[] ResolveTargets(Creature attacker, IEnumerable<Creature> candidates)
        {
            var valid = candidates.Where(c => c != attacker && c.IsAlive && !IsDecorativeCompanion(c)).ToArray();
            var sheaths = valid.Where(c => c.Player != null && c.HasPower<ScabbardPower>()).OrderBy(c => c.Player!.NetId).ToArray();
            return sheaths.Length > 0 ? sheaths : valid.Where(c => !PrisonForTwoPower.ShouldExcludeTarget(attacker, c)).ToArray();
        }
        private static bool HasCombatNode(Creature creature)
        {
            return NCombatRoom.Instance is not { } combatRoom || combatRoom.GetCreatureNode(creature) != null;
        }

        /// <summary>Cosmetic relic pets that never participate in combat (no health bar, no AI).</summary>
        private static bool IsDecorativeCompanion(Creature candidate) =>
            candidate.Monster is PaelsLegion or Byrdpip;
    }
}