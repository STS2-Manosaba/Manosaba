using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using Manosaba.Characters.TachibanaSherry.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TachibanaSherry.Powers;

/// <summary>扳手腕：每名玩家同時只能鎖定一名敵人；依雙方力量調整傷害。</summary>
public sealed class AchubaPower : PathCustomPowerModel, Manosaba.Compatibility.IManosabaAfterDeathHandler
{
    private const string ApplierVar = "Applier";
    private const decimal DamageBonusMultiplier = 1.2m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar(ApplierVar)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromCard<Achuba>(),
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = cardSource;
        if (applier == null || Owner?.CombatState is not { } combatState)
        {
            return;
        }

        foreach (Creature enemy in combatState.GetOpponentsOf(applier).ToList())
        {
            foreach (AchubaPower existing in enemy.GetPowerInstances<AchubaPower>())
            {
                if (existing != this && existing.Applier == applier)
                {
                    await PowerCmd.Remove(this);
                    return;
                }
            }
        }

        if (DynamicVars[ApplierVar] is StringVar applierVar && applier.Player?.NetId is { } netId)
        {
            applierVar.StringValue = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, netId);
        }
    }

    public async Task HandleAfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        _ = choiceContext;
        _ = deathAnimLength;
        if (!wasRemovalPrevented && creature == Applier)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = cardSource;
        if (Applier == null || Owner == null || !props.IsPoweredAttack_())
        {
            return 1m;
        }

        decimal applierStrength = Applier.GetPowerAmount<StrengthPower>();
        decimal enemyStrength = Owner.GetPowerAmount<StrengthPower>();

        if (target == Owner && dealer == Applier && applierStrength > enemyStrength)
        {
            return DamageBonusMultiplier;
        }

        if (dealer == Owner && target == Applier && applierStrength < enemyStrength)
        {
            return DamageBonusMultiplier;
        }

        return 1m;
    }

    public static Creature? GetLockedEnemy(Creature? playerCreature)
    {
        if (playerCreature?.CombatState is not { } combatState)
        {
            return null;
        }

        foreach (Creature enemy in combatState.GetOpponentsOf(playerCreature))
        {
            foreach (AchubaPower power in enemy.GetPowerInstances<AchubaPower>())
            {
                if (power.Applier == playerCreature && power.Amount > 0m)
                {
                    return enemy;
                }
            }
        }

        return null;
    }

    public static bool AllowsAttackTarget(CardModel card, Creature? target)
    {
        if (card.Type != CardType.Attack || card.TargetType != TargetType.AnyEnemy)
        {
            return true;
        }

        if (card.Owner?.Creature is not { } ownerCreature)
        {
            return true;
        }

        Creature? lockedEnemy = GetLockedEnemy(ownerCreature);
        return lockedEnemy == null || target == lockedEnemy;
    }
}
