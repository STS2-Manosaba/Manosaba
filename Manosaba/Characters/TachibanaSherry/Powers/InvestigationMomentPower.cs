using BaseLib.Utils;
using System.Collections.Generic;
using System.Linq;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TachibanaSherry.Powers;

/// <summary>調查時刻：每回合開始 +1 顯示進度；滿 5 層時依自身疑點對隨機敵人造成 unpower 傷害並結算。</summary>
public sealed class InvestigationMomentPower : PathCustomPowerModel
{
    private sealed class Data
    {
        public int LayerCredit;
    }

    private const int LayerThreshold = 5;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType =>
        GetInternalData<Data>().LayerCredit > 0 ? PowerStackType.Counter : PowerStackType.None;

    public override int DisplayAmount => GetInternalData<Data>().LayerCredit;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CluePower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        await AdvanceOneLayerCreditAsync(choiceContext);
    }

    /// <summary>與「每回合開始 +1」相同：進度 +1，並結算是否達 5 層觸發。</summary>
    public async Task AdvanceOneLayerCreditAsync(PlayerChoiceContext choiceContext)
    {
        Data data = GetInternalData<Data>();
        data.LayerCredit++;
        InvokeDisplayAmountChanged();
        await ResolveLayerThresholdsAsync(choiceContext);
        InvokeDisplayAmountChanged();
    }

    private async Task ResolveLayerThresholdsAsync(PlayerChoiceContext choiceContext)
    {
        Data data = GetInternalData<Data>();

        while (data.LayerCredit >= LayerThreshold)
        {
            data.LayerCredit -= LayerThreshold;
            InvokeDisplayAmountChanged();
            Flash();

            if (CombatState == null)
                return;

            List<Creature> enemies = CombatState.GetOpponentsOf(Owner)
                .Where(e => e.IsAlive && e.IsHittable)
                .ToList();
            if (enemies.Count == 0)
                continue;

            Creature? target = CombatState.RunState.Rng.CombatTargets.NextItem(enemies);
            if (target == null)
                continue;

            decimal clue = Owner.GetPowerAmount<CluePower>();
            if (clue > 0m)
                await CreatureCmd.Damage(choiceContext, target, clue, ValueProp.Unpowered, Owner, null, null);

            if (clue > 0m)
                await CommonActions.Apply<CluePower>(choiceContext, Owner, null, -clue);

            await CommonActions.Apply<SherryDetectiveRewardPower>(choiceContext, Owner, null, 2m);
        }
    }
}
