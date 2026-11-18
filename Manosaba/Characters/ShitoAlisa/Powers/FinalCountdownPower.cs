using BaseLib.Utils;
using Manosaba.Characters.Common.Commands;
using MegaCrit.Sts2.Core.Combat;
using Manosaba.Extensions;
using Manosaba.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.ShitoAlisa.Powers;

/// <summary>終焉的倒數計時：歸 0 時直接獲勝。</summary>
public sealed class FinalCountdownPower : PathCustomPowerModel
{
    private const string VfxScenePath = "res://Manosaba/scenes/shito_alisa/vfx/final_countdown_ring_flames.tscn";
    private bool _winTriggered;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side)
            return;
        if (Amount <= 0)
            return;
        if (IsCombatOverOrEnding(ManosabaCombatCompat.From(Owner)))
            return;
        await CommonActions.Apply<FinalCountdownPower>(choiceContext, Owner, null, -1m, silent: true);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this)
            return;

        if (IsCombatOverOrEnding(ManosabaCombatCompat.From(Owner)) || _winTriggered)
            return;
        if (Amount > 0)
            return;

        // Only trigger on crossing from positive -> zero/below.
        decimal previousAmount = Amount - amount;
        if (previousAmount <= 0m)
            return;

        _winTriggered = true;
        await ManosabaVfxCmd.PlaySceneAtCombatCenterAndWait(VfxScenePath, fitCoverViewport: false);
        ICombatState? combatState = ManosabaCombatCompat.From(Owner);
        if (combatState != null)
        {
            await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape(combatState);
        }
    }

    public static async Task ReduceFromCombust(Creature owner, CardModel? cardSource)
    {
        FinalCountdownPower? power = owner.GetPower<FinalCountdownPower>();
        if (power == null || power.Amount <= 0m || IsCombatOverOrEnding(ManosabaCombatCompat.From(owner)))
            return;
        await CommonActions.Apply<FinalCountdownPower>(new ThrowingPlayerChoiceContext(), owner, cardSource, -1m, silent: true);
    }

    private static bool IsCombatOverOrEnding(ICombatState? combatState)
    {
        if (combatState == null)
            return true;

        CombatManager? manager = CombatManager.Instance;
        return manager != null && manager.IsOverOrEnding;
    }
}
