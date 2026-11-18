using System.Collections.Generic;
using System.Linq;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.ShitoAlisa.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.ShitoAlisa.Powers;

/// <summary>每累積對敵人施加足夠層灼燒後，對一名隨機敵人造成傷害。</summary>
public sealed class DisasterGirlPower : PathCustomPowerModel
{
    private sealed class Data
    {
        public int BurnCredit;
        public readonly List<decimal> DamagePerStackLayer = [];
    }

    private const int BurnThreshold = 5;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => GetInternalData<Data>().BurnCredit;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Damage", 7m)];

    /// <summary>戰鬥狀態放在 <see cref="InitInternalData"/>；戰鬥結束能力移除後即丟棄（同原版 Outbreak 模式）。</summary>
    protected override object InitInternalData() => new Data();

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        Data data = GetInternalData<Data>();
        if (power == this)
        {
            List<decimal> layers = data.DamagePerStackLayer;
            if (amount > 0m && cardSource is DisasterGirl dg
                && dg.DynamicVars.TryGetValue("DisasterGirlDmg", out DynamicVar? dmgVar))
            {
                for (int i = 0; i < (int)amount; i++)
                    layers.Add(dmgVar.BaseValue);
            }
            else if (amount < 0m)
            {
                int remove = (int)(-amount);
                for (int r = 0; r < remove && layers.Count > 0; r++)
                    layers.RemoveAt(layers.Count - 1);
            }

            DynamicVars["Damage"].BaseValue = TotalTriggerDamage();
        }

        if (power is not BurnPower || applier == null || Owner == null)
            return;
        if (power.Owner == null || power.Owner.Side == applier.Side)
            return;
        if (applier != Owner)
            return;
        if (amount <= 0m)
            return;

        data.BurnCredit += (int)amount;
        while (data.BurnCredit >= BurnThreshold)
        {
            data.BurnCredit -= BurnThreshold;
            InvokeDisplayAmountChanged();
            Flash();
            if (CombatState == null)
                return;
            decimal dmg = TotalTriggerDamage();
            List<Creature> enemies = CombatState.GetOpponentsOf(Owner)
                .Where(e => e.IsAlive && e.IsHittable)
                .ToList();
            if (enemies.Count == 0)
                continue;
            Creature? target = CombatState.RunState.Rng.CombatTargets.NextItem(enemies);
            if (target == null)
                continue;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, dmg, ValueProp.Unpowered, Owner, null, null);
        }
        InvokeDisplayAmountChanged();
    }

    private decimal TotalTriggerDamage()
    {
        List<decimal> layers = GetInternalData<Data>().DamagePerStackLayer;
        return layers.Count > 0 ? layers.Sum() : 7m;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BurnPower>()];
}
