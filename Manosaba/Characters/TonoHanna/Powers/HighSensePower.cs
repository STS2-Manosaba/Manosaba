using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.TonoHanna.Powers;

/// <summary>Hanna's combat skill state and its one-turn numeric dice bonus.</summary>
public sealed class HighSensePower : PathCustomPowerModel
{
    public const int DefaultCooldownTurnsAfterUse = 3;
    public const int MinimumCooldownTurnsAfterUse = 1;

    private const string CooldownVarName = "Cooldown";
    private const string CooldownTurnsAfterUseVarName = "CooldownTurnsAfterUse";
    private const string NumericFaceBonusVarName = "NumericFaceBonus";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CooldownVarName, 0m),
        new DynamicVar(CooldownTurnsAfterUseVarName, DefaultCooldownTurnsAfterUse),
        new DynamicVar(NumericFaceBonusVarName, 0m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SoarPower>(),
    ];

    public int CooldownRemaining => DynamicVars[CooldownVarName].IntValue;
    public int CooldownTurnsAfterUse => DynamicVars[CooldownTurnsAfterUseVarName].IntValue;
    public int NumericFaceBonus => DynamicVars[NumericFaceBonusVarName].IntValue;
    public bool IsReady => CooldownRemaining <= 0;

    public void MarkUsed() => DynamicVars[CooldownVarName].BaseValue = CooldownTurnsAfterUse;

    public void ActivateDiceBonus() => DynamicVars[NumericFaceBonusVarName].BaseValue = 2m;

    public void OnDuplicateCardPlayed()
    {
        DynamicVar cooldownTurnsAfterUse = DynamicVars[CooldownTurnsAfterUseVarName];
        if (cooldownTurnsAfterUse.IntValue <= MinimumCooldownTurnsAfterUse)
        {
            return;
        }

        cooldownTurnsAfterUse.BaseValue--;
        if (CooldownRemaining > 0)
        {
            DynamicVars[CooldownVarName].BaseValue--;
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;
        if (player == Owner.Player && CooldownRemaining > 0)
        {
            DynamicVars[CooldownVarName].BaseValue--;
        }

        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;
        _ = creatures;
        if (side == Owner.Side && NumericFaceBonus > 0)
        {
            DynamicVars[NumericFaceBonusVarName].BaseValue = 0m;
        }

        return Task.CompletedTask;
    }
}
