using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class BestInfoMagicPower : PathCustomPowerModel
{
    private const string CasterNetIdVar = "CasterNetId";
    private PlayerChoiceContext? _lastDrawContext;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Chance", 30m),
        new StringVar(CasterNetIdVar)
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Creature? caster = applier ?? cardSource?.Owner.Creature;
        if (caster?.Player?.NetId is { } casterNetId && DynamicVars[CasterNetIdVar] is StringVar casterVar)
            casterVar.StringValue = casterNetId.ToString();

        RefreshChance();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext context,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(context, power, amount, applier, cardSource);

        if (power is MajokaPower majoka && IsCaster(majoka.Owner))
            RefreshChance(majoka.Amount);
    }

    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        _ = fromHandDraw;

        if (Owner.Player is { } viewer && card.Owner == viewer && !ReferenceEquals(_lastDrawContext, context))
        {
            BestInfoMagicPower primary = Owner.GetPowerInstances<BestInfoMagicPower>().FirstOrDefault() ?? this;
            if (!ReferenceEquals(primary, this))
                return Task.CompletedTask;

            _lastDrawContext = context;
            _ = PreviewAfterDrawSettles(viewer);
        }

        return Task.CompletedTask;
    }

    private async Task PreviewAfterDrawSettles(MegaCrit.Sts2.Core.Entities.Players.Player viewer)
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            PreviewTopCard(viewer);
            return;
        }

        int previousDrawPileCount = -1;
        int stableFrames = 0;
        // CardPileCmd.Draw can pause between individual cards. Require a longer
        // stable period so a multi-card draw cannot be previewed mid-action.
        for (int frame = 0; frame < 120 && stableFrames < 10; frame++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);

            int currentDrawPileCount = PileType.Draw.GetPile(viewer).Cards.Count;
            if (currentDrawPileCount == previousDrawPileCount)
                stableFrames++;
            else
                stableFrames = 0;

            previousDrawPileCount = currentDrawPileCount;
        }

        if (Owner.Player == viewer)
            PreviewTopCard(viewer);
    }

    private void PreviewTopCard(MegaCrit.Sts2.Core.Entities.Players.Player viewer)
    {
        IReadOnlyList<CardModel> drawPile = PileType.Draw.GetPile(viewer).Cards;
        List<BestInfoMagicPower> powers = Owner.GetPowerInstances<BestInfoMagicPower>().ToList();
        if (powers.Count == 0)
            powers.Add(this);

        foreach (BestInfoMagicPower power in powers)
            power.RefreshChance();

        int cardCount = Math.Min(
            Math.Max(0, powers.Sum(power => Math.Max(0, (int)power.Amount))),
            drawPile.Count);
        if (cardCount <= 0)
            return;

        var rng = new Random(CreatePreviewSeed(viewer, drawPile));
        List<CardModel> shownCards = [];
        int cardIndex = 0;
        foreach (BestInfoMagicPower power in powers)
        {
            for (int stack = 0; stack < power.Amount && cardIndex < cardCount; stack++)
            {
                CardModel actualCard = drawPile[cardIndex];
                decimal accuracy = power.DynamicVars["Chance"].BaseValue;
                if (rng.Next(100) < (int)accuracy || drawPile.Count == 1)
                {
                    shownCards.Add(actualCard);
                }
                else
                {
                    List<CardModel> alternatives = drawPile
                        .Where((_, candidateIndex) => candidateIndex != cardIndex)
                        .ToList();
                    shownCards.Add(alternatives[rng.Next(alternatives.Count)]);
                }

                cardIndex++;
            }
        }

        CardCmd.Preview(shownCards, 3f, CardPreviewStyle.HorizontalLayout);
    }

    private void RefreshChance(decimal? majokaAmount = null)
    {
        Creature? caster = GetCaster();
        decimal majoka = majokaAmount ?? caster?.GetPowerAmount<MajokaPower>() ?? 0m;
        decimal accuracy = 30m + Math.Min(70m, majoka * 0.7m);
        DynamicVars["Chance"].BaseValue = accuracy;
        InvokeDisplayAmountChanged();
    }

    private static int CreatePreviewSeed(
        MegaCrit.Sts2.Core.Entities.Players.Player viewer,
        IReadOnlyList<CardModel> drawPile)
    {
        unchecked
        {
            uint hash = 2166136261u;
            string seedText = $"{viewer.NetId}:{drawPile[0].Id.Entry}:{drawPile.Count}";
            foreach (char character in seedText)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            return (int)hash;
        }
    }

    private bool IsCaster(Creature creature) => GetCaster() == creature;

    private Creature? GetCaster()
    {
        if (DynamicVars[CasterNetIdVar] is not StringVar casterVar || string.IsNullOrWhiteSpace(casterVar.StringValue))
            return null;

        return Owner.CombatState?.Players
            .FirstOrDefault(player => string.Equals(player.NetId.ToString(), casterVar.StringValue, StringComparison.Ordinal))
            ?.Creature;
    }
}
