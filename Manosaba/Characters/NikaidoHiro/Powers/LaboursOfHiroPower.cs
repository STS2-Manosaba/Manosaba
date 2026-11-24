using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.NikaidoHiro.Powers
{
    public class LaboursOfHiroPower : PathCustomPowerModel, Manosaba.Compatibility.IManosabaAfterDeathHandler
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3m)];
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Type == CardType.Attack)
            {
                if (Owner.Player?.Creature is not { } ownerCreature)
                {
                    return;
                }

                await CommonActions.Apply<MajokaPower>(context, ownerCreature, cardPlay.Card, 10 * cardPlay.Card.EnergyCost.GetResolved());
            }
        }

        public async Task HandleAfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            _ = choiceContext;
            _ = deathAnimLength;

            if (wasRemovalPrevented || Owner is not { IsAlive: true } || Owner.CombatState == null)
            {
                return;
            }

            if (creature.CombatState != Owner.CombatState || creature.Side != Owner.Side || creature == Owner)
            {
                return;
            }

            Flash();
            await CommonActions.Apply<StrengthPower>(choiceContext, Owner, null, DynamicVars[nameof(StrengthPower)].BaseValue);
        }
    }
}
