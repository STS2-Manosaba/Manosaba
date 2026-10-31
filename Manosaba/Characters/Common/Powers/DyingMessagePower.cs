using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.NikaidoHiro.Powers
{
    public class DyingMessagePower : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2), new CardsVar(3)];

        private int buffToApply = 3;

        public void Upgrade()
        {
            buffToApply += 2;
            DynamicVars.Cards.BaseValue = buffToApply;
            InvokeDisplayAmountChanged();
        }

        /// <summary>
        /// 在持有者進入死亡結算時觸發一次：<b>無論</b>最後是否真的退場。
        /// 真正死亡時 <paramref name="wasRemovalPrevented"/> 為 <c>false</c>；
        /// 若被 <c>ShouldDie</c> / <c>ShouldDieLate</c> 擋下（蜥蜴尾巴、死亡迴歸等），引擎會先以 <c>true</c> 呼叫本方法，再執行擋死方的 <c>AfterPreventingDeath</c>。
        /// 其他「致死觸發一次再復活」的效果可沿用同一寫法，無需逐一套件白名單。
        /// </summary>
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (creature != base.Owner || base.CombatState == null)
                return;

            foreach (Creature item in base.CombatState.GetTeammatesOf(base.Owner))
            {
                if (item == null || !item.IsAlive || !item.IsPlayer || item == Owner)
                    continue;

                await CommonActions.Apply<DrawCardsNextTurnPower>(choiceContext, item, null, buffToApply, silent: true);
                await CommonActions.Apply<EnergyNextTurnPower>(choiceContext, item, null, 2, silent: true);
            }
        }


        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (side != base.Owner.Side)
            {
                Flash();
                await PowerCmd.Remove(this);
            }
        }
    }
}
