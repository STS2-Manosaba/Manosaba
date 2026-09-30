using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class BadEndQuestionPower : PathCustomPowerModel
{
    private const int BadEndCount = 15;
    private const int ShowMissingListFromAmount = 10;
    private readonly HashSet<string> _triggeredCardIds = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MissingBadEndsLineVar()];

    public int TriggeredCount => _triggeredCardIds.Count;

    public bool Record(Type cardType)
    {
        if (!_triggeredCardIds.Add(cardType.FullName ?? cardType.Name))
            return false;

        InvokeDisplayAmountChanged();
        return true;
    }

    private sealed class MissingBadEndsLineVar : DynamicVar
    {
        public MissingBadEndsLineVar()
            : base("MissingBadEndsLine", 0m)
        {
        }

        public override string ToString()
        {
            if (_owner is not BadEndQuestionPower tracker)
                return string.Empty;

            int amount = tracker.Amount;
            if (amount < ShowMissingListFromAmount || amount >= BadEndCount)
                return string.Empty;

            List<string> missing = CollectMissingBadEndTitles(tracker);
            if (missing.Count == 0)
                return string.Empty;

            string separator = new LocString("powers", "MANOSABA-BAD_END_QUESTION_POWER.missingListSeparator")
                .GetRawText() ?? "、";
            string prefix = new LocString("powers", "MANOSABA-BAD_END_QUESTION_POWER.missingListPrefix")
                .GetFormattedText();
            return "\n" + prefix + string.Join(separator, missing);
        }
    }

    private static List<string> CollectMissingBadEndTitles(BadEndQuestionPower tracker)
    {
        List<string> missing = [];
        foreach (Type type in EmaBadEndingCards.EmaPool)
        {
            string id = type.FullName ?? type.Name;
            if (tracker._triggeredCardIds.Contains(id))
                continue;

            CardModel card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            missing.Add(card.Title);
        }

        return missing;
    }
}
