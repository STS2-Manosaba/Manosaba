using BaseLib.Utils;
using System.Collections.Generic;
using Manosaba.Characters.Common.Commands;
using Manosaba.Extensions;
using manosaba.Characters.TonoHanna.Relics;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace Manosaba.Characters.TonoHanna.Powers;
/// <summary>
/// Single visible buff: stack count = number of distinct puppet companions with a collection charge (Amount &gt; 0).
/// Icons use <c>puppet_collection_power.png</c>; per-character collection powers stay hidden and only drive the ring + this tally.
/// </summary>
public sealed class PuppetCollectionSummaryPower : PathCustomPowerModel
{
    private const string SharedIconFile = "puppet_collection_power.png";
    private const string WinVfxScenePath = "res://Manosaba/scenes/tono_hanna/vfx/puppet_collection.tscn";
    private const string WinBgmEventPath = "event:/Manosaba/audio/bgm/puppet_collection.mp3";
    private const int CollectionWinThreshold = 13;
    private const int ShowMissingPuppetListFromAmount = 10;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => SharedIconFile.PowerImagePath();

    public override string CustomBigIconPath => SharedIconFile.PowerImagePath();

    public override string CustomBigBetaIconPath => SharedIconFile.PowerImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new MissingPuppetsLineVar(); }
    }

    private sealed class MissingPuppetsLineVar : DynamicVar
    {
        public MissingPuppetsLineVar()
            : base("MissingPuppetsLine", 0m)
        {
        }

        public override string ToString()
        {
            if (_owner is not PuppetCollectionSummaryPower summary || summary.Owner == null)
            {
                return string.Empty;
            }

            int amt = summary.Amount;
            if (amt < ShowMissingPuppetListFromAmount || amt >= CollectionWinThreshold)
            {
                return string.Empty;
            }

            List<string> missing = CollectMissingPuppetTitles(summary.Owner);
            if (missing.Count == 0)
            {
                return string.Empty;
            }

            string sep = new LocString("powers", "MANOSABA-PUPPET_COLLECTION_SUMMARY_POWER.missingListSeparator")
                .GetRawText() ?? "、";
            string prefix = new LocString("powers", "MANOSABA-PUPPET_COLLECTION_SUMMARY_POWER.missingListPrefix")
                .GetFormattedText();
            return "\n" + prefix + string.Join(sep, missing);
        }
    }

    private static List<string> CollectMissingPuppetTitles(Creature owner)
    {
        List<string> missing = [];
        AppendIfMissing<AlisaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<AnAnPuppetCollectionPower>(owner, missing);
        AppendIfMissing<CocoPuppetCollectionPower>(owner, missing);
        AppendIfMissing<EmaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<HannaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<HiroPuppetCollectionPower>(owner, missing);
        AppendIfMissing<LeiaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<MargoPuppetCollectionPower>(owner, missing);
        AppendIfMissing<MeruruPuppetCollectionPower>(owner, missing);
        AppendIfMissing<MiriaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<NanokaPuppetCollectionPower>(owner, missing);
        AppendIfMissing<NoahPuppetCollectionPower>(owner, missing);
        AppendIfMissing<SherryPuppetCollectionPower>(owner, missing);
        return missing;
    }

    private static void AppendIfMissing<T>(Creature owner, List<string> missing)
        where T : PuppetCollectionPowerBase
    {
        if (owner.GetPower<T>() is { Amount: > 0 })
        {
            return;
        }

        missing.Add(ModelDb.Power<T>().Title.GetFormattedText());
    }

    public static async Task SyncToRosterAsync(Creature owner)
    {
        if (TestMode.IsOn)
            return;

        int target = CountActiveCollectionKinds(owner);
        PuppetCollectionSummaryPower? summary = owner.GetPower<PuppetCollectionSummaryPower>();
        int current = summary?.Amount ?? 0;
        if (target == current)
            return;

        if (target == 0)
        {
            if (summary != null)
                await PowerCmd.Remove(summary);
            return;
        }

        int gainedDistinctKinds = target - current;
        await CommonActions.Apply<PuppetCollectionSummaryPower>(new ThrowingPlayerChoiceContext(), owner, null, gainedDistinctKinds, silent: true);
    }

    private static int CountActiveCollectionKinds(Creature owner)
    {
        int n = 0;
        foreach (PowerModel p in owner.Powers)
        {
            if (p is PuppetCollectionPowerBase && p.Amount > 0)
                n++;
        }

        return n;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power is not PuppetCollectionSummaryPower || power.Owner == null)
            return;
        if (CountActiveCollectionKinds(power.Owner) < CollectionWinThreshold)
            return;
        SfxCmd.Play(WinBgmEventPath, 0.8f);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != CombatSide.Player || !Owner.IsPlayer)
            return;
        if (CountActiveCollectionKinds(Owner) < CollectionWinThreshold)
            return;
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
            return;

        await ManosabaVfxCmd.PlaySceneAtCombatCenterAndWait(
            WinVfxScenePath,
            fitCoverViewport: true,
            spriteNodeNames: ["StillA", "StillB"]);
        await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape(combatState);
    }
}
