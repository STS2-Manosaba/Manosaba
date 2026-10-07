using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Cards;

internal sealed class TraumaMajokaVar(decimal amount) : PowerVar<MajokaPower>(amount)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        PreviewValue = BaseValue;
        if (!runGlobalHooks || card.CombatState is not { } combat || card.Owner?.Creature is not { } owner)
            return;

        // These cards always apply Majoka to their owner, even before a target is selected.
        PowerModel power = ModelDb.Power<MajokaPower>();
        decimal amount = Hook.ModifyPowerAmountGiven(combat, power, owner, BaseValue, owner, card, out _);
        amount = Hook.ModifyPowerAmountReceived(combat, power, owner, amount, owner, out _);
        PreviewValue = (int)amount;
    }
}
