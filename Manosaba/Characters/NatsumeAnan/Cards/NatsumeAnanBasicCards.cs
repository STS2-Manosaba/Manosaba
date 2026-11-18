using BaseLib.Utils;
using manosaba.Characters.NatsumeAnan.Powers;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace manosaba.Characters.NatsumeAnan.Cards;

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class StrikeNatsumeAnan : NatsumeKotodamaCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public StrikeNatsumeAnan() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class DefendNatsumeAnan : NatsumeKotodamaCardModel
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    public DefendNatsumeAnan() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class TraumaNatsumeAnan : NatsumeKotodamaCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(10m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        HoverTipFactory.FromPower<MajokaPower>(),
    ];

    public TraumaNatsumeAnan() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MajokaPower"].UpgradeValueBy(5m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class FlashOfInspiration : NatsumeKotodamaCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("KotodamaGain", 2m)];

    public FlashOfInspiration() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        KotodamaEnergy.Gain(Owner, DynamicVars["KotodamaGain"].IntValue);
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["KotodamaGain"].UpgradeValueBy(1m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class Instigate : NatsumeKotodamaCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("KotodamaCost", 1m),
        new DynamicVar("CardsNextTurn", 1m),
    ];

    public Instigate() : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyPlayer, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null)
        {
            Log.Debug($"[Manosaba SyncTrace][Instigate] skip owner={Owner.NetId} reason=no_target");
            return;
        }

        Log.Debug(
            $"[Manosaba SyncTrace][Instigate] begin owner={Owner.NetId} target={targetPlayer.NetId} targetChar={targetPlayer.Character.Id.Entry} " +
            $"ownerHandBefore={GetPileCount(Owner, PileType.Hand)} targetDrawBefore={GetPileCount(targetPlayer, PileType.Draw)} " +
            $"rng={FormatRngCounters(Owner.RunState.Rng)}");

        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, targetPlayer, 1, CardPilePosition.Top, forceExhaust: false);

        if (IsUpgraded)
        {
            await CommonActions.Apply<DrawCardsNextTurnPower>(
                choiceContext,
                targetPlayer.Creature,
                this,
                DynamicVars["CardsNextTurn"].BaseValue);
        }

        Log.Debug(
            $"[Manosaba SyncTrace][Instigate] afterAutoPlay owner={Owner.NetId} target={targetPlayer.NetId} " +
            $"ownerHandNow={GetPileCount(Owner, PileType.Hand)} targetDrawNow={GetPileCount(targetPlayer, PileType.Draw)} " +
            $"rng={FormatRngCounters(Owner.RunState.Rng)}");
    }

    protected override void OnUpgrade()
    {
    }

    private static int GetPileCount(Player player, PileType pileType)
    {
        CardPile? pile = pileType.GetPile(player);
        return pile?.Cards?.Count ?? -1;
    }

    private static string FormatRngCounters(object? rngRoot)
    {
        if (rngRoot == null)
        {
            return "rngRoot=null";
        }

        return $"gen={TryGetStreamCounter(rngRoot, "CombatCardGeneration")},sel={TryGetStreamCounter(rngRoot, "CombatCardSelection")}," +
               $"shuf={TryGetStreamCounter(rngRoot, "Shuffle")},target={TryGetStreamCounter(rngRoot, "CombatTargets")}";
    }

    private static string TryGetStreamCounter(object rngRoot, string streamName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type rootType = rngRoot.GetType();

        object? stream =
            rootType.GetProperty(streamName, flags)?.GetValue(rngRoot) ??
            rootType.GetField(streamName, flags)?.GetValue(rngRoot);

        if (stream == null)
        {
            return $"{streamName}:na";
        }

        Type streamType = stream.GetType();
        object? counter =
            streamType.GetProperty("Counter", flags)?.GetValue(stream) ??
            streamType.GetField("Counter", flags)?.GetValue(stream) ??
            streamType.GetField("_counter", flags)?.GetValue(stream);

        return counter == null ? $"{streamName}:?" : $"{streamName}:{counter}";
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class Jigaishiro : NatsumeKotodamaCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("KotodamaCost", 1m),
        new PowerVar<JigaiPower>(5m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        HoverTipFactory.FromPower<JigaiPower>(),
    ];

    public Jigaishiro() : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;

        if (cardPlay.Target == null)
            return;

        await CommonActions.Apply<JigaiPower>(choiceContext, cardPlay.Target, this, DynamicVars["JigaiPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["JigaiPower"].UpgradeValueBy(3m);
    }
}
