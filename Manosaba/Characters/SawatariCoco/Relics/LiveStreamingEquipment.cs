using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SawatariCoco.Relics;

[Pool(typeof(SawatariCocoRelicPool))]
public sealed class LiveStreamingEquipment : LevelingPathCustomRelicModel
{
    private int _totalFanCount;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override int MaxRelicLevel => 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FanCount", 0m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        HoverTipFactory.FromPower<LiveStreamModePower>(),
        HoverTipFactory.FromPower<FanPower>(),
        HoverTipFactory.FromPower<HidingPower>(),
    ];

    [SavedProperty]
    public int TotalFanCount
    {
        get => _totalFanCount;
        set
        {
            AssertMutable();
            _totalFanCount = value < 0 ? 0 : value;
            SyncFanCountVar();
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterObtained()
    {
        SyncFanCountVar();
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature is not { } creature)
        {
            return;
        }

        Flash();
        await CommonActions.Apply<HidingPower>(new ThrowingPlayerChoiceContext(), creature, null, RelicLevel);

        if (RelicLevel >= 2)
        {
            Flash();
            await CommonActions.Apply<LiveStreamModePower>(new ThrowingPlayerChoiceContext(), creature, null, 3m);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (RelicLevel < 4 || player != Owner || Owner.Creature is not { } creature)
        {
            return;
        }

        if (!SawatariCocoHelper.IsInLiveStreamMode(creature))
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        _ = props;
        _ = cardSource;

        if (Owner.Creature is not { } ownerCreature)
        {
            return;
        }

        if (dealer != ownerCreature && dealer?.PetOwner != Owner)
        {
            return;
        }

        if (!SawatariCocoHelper.IsInLiveStreamMode(ownerCreature))
        {
            return;
        }

        if (target.IsPlayer || result.TotalDamage <= 0m)
        {
            return;
        }

        if (SawatariCocoHelper.IsFanOf(target, ownerCreature))
        {
            return;
        }

        Flash();
        await SawatariCocoHelper.TryMakeFanAsync(choiceContext, Owner, target);
    }

    private void SyncFanCountVar()
    {
        DynamicVars["FanCount"].BaseValue = _totalFanCount;
    }
}
