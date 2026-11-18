using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HikamiMeruru.Powers
{
    public class DoubleSuicidePower : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override bool AllowNegative => false;

        protected override IEnumerable<DynamicVar> CanonicalVars => [new ExtraDamageVar(3)];

        public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (target != Owner)
            {
                return 0m;
            }

            return DynamicVars.ExtraDamage.BaseValue;
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (side == CombatSide.Enemy)
            {
                await CreatureCmd.Heal(Owner, Owner.MaxHp * (base.Amount / 100m));
                await PowerCmd.Remove<DoubleSuicidePower>(Owner);
            }
        }
    }
}
