using System.Collections.Generic;
using BaseLib.Utils;
using manosaba.Characters.TachibanaSherry;
using Manosaba.Characters.TachibanaSherry.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.TachibanaSherry.Cards;

[Pool(typeof(TachibanaSherryCardPool))]
public sealed class SanFindsClue : PathCustomCardModel
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<CluePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    public SanFindsClue() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponentsCompat(CombatState)
            .Execute(choiceContext);

        await PowerCmd.Apply<StrengthPower>(choiceContext, ownerCreature, 1m, ownerCreature, this);
        await PowerCmd.Apply<PlayedSanThisTurnPower>(choiceContext, ownerCreature, 1m, ownerCreature, this);
        RefreshRokuCosts(Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    /// <summary>疑點本回合增量變動時，刷新此牌的耗�E顯示、E/summary>
    public static void RefreshCostsForOwner(Player? player)
    {
        if (player == null)
        {
            return;
        }

        foreach (CardModel card in EnumerateCombatCards(player))
        {
            if (card is SanFindsClue)
            {
                card.InvokeEnergyCostChanged();
            }
        }
    }

    private static void RefreshRokuCosts(Player? player)
    {
        if (player == null)
        {
            return;
        }

        foreach (CardModel card in EnumerateCombatCards(player))
        {
            if (card is RokuBraveFace)
            {
                card.InvokeEnergyCostChanged();
            }
        }
    }

    private static IEnumerable<CardModel> EnumerateCombatCards(Player player)
    {
        foreach (PileType pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Deck })
        {
            foreach (CardModel c in pile.GetPile(player).Cards)
            {
                yield return c;
            }
        }
    }
}
