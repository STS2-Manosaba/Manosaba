using BaseLib.Utils;
using BaseLib.Extensions;
using Manosaba.Characters.HikamiMeruru.Cards;
using Manosaba.Characters.JogasakiNoahCard.Cards;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Characters.SaekiMiria.Cards;
using Manosaba.Characters.TachibanaSherry.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.Common.Powers
{
    public sealed class PoweredReflectPower : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        private Creature? _lastReflectedDealer;
        private bool _pendingVigorClear;

        public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            if (ReflectionDamageGuard.IsActive)
                return;

            if (target == base.Owner
                && dealer != null
                && props.IsPoweredAttack_()
                && _pendingVigorClear
                && _lastReflectedDealer != null
                && dealer != _lastReflectedDealer)
            {
                await ConsumeCurrentVigor(choiceContext);
            }

            if (target == base.Owner && result.BlockedDamage > 0 && props.IsPoweredAttack_() && dealer != null)
            {
                await ReflectionDamageGuard.Run(() => CreatureCmd.Damage(choiceContext, dealer, result.BlockedDamage, ValueProp.Move, base.Owner, null, null));
                _pendingVigorClear = true;
                _lastReflectedDealer = dealer;
            }
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            _ = creatures;

            if (side == base.Owner.Side)
            {
                _pendingVigorClear = false;
                _lastReflectedDealer = null;
                return;
            }

            if (!_pendingVigorClear)
                return;

            await ConsumeCurrentVigor(choiceContext);
        }

        private async Task ConsumeCurrentVigor(PlayerChoiceContext choiceContext)
        {
            decimal vigor = base.Owner.GetPowerAmount<VigorPower>();
            if (vigor > 0m)
            {
                await CommonActions.Apply<VigorPower>(choiceContext, base.Owner, null, -vigor);
            }

            _pendingVigorClear = false;
            _lastReflectedDealer = null;
        }

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side == base.Owner.Side)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }


}
