using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.NikaidoHiro.Powers
{
    public sealed class MusouKenPower : PathCustomPowerModel
    {
        private Creature? _target;
        private CardModel? _sourceCard;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => false;

        public void SetTarget(Creature target)
        {
            _target = target;
        }

        public void SetSourceCard(CardModel sourceCard)
        {
            _sourceCard = sourceCard;
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (target != Owner)
            {
                return 1m;
            }

            return 0.5m;
        }

        public override async Task AfterDamageReceived(
            PlayerChoiceContext choiceContext,
            Creature target,
            DamageResult result,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource)
        {
            if (target != Owner)
            {
                return;
            }

            int damageTaken = result.TotalDamage;
            if (damageTaken <= 0)
            {
                return;
            }

            await CommonActions.Apply<MusouKenPower>(new ThrowingPlayerChoiceContext(), Owner, cardSource, damageTaken);
        }

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player != Owner.Player)
            {
                return;
            }

            if (_target != null && _target.IsAlive)
            {
                var attackBuilder = DamageCmd.Attack(Amount);
                if (_sourceCard != null)
                {
                    attackBuilder = attackBuilder.FromCard(_sourceCard, null);
                }

                await attackBuilder
                    .Targeting(_target)
                    .Execute(choiceContext);
            }

            await PowerCmd.Remove(this);
        }
    }
}
