using System.Linq;
using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class EmergencySuture : PathCustomCardModel
{
    private const string calculatedHitsKey = "CalculatedHits";

    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(calculatedHitsKey).WithMultiplier(static (card, _) => GetStatuses(card.Owner).Count()),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public EmergencySuture()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner.PlayerCombatState == null)
        {
            return;
        }

        // Same as vanilla FlakCannon: all Status cards in combat not already in Exhaust.
        // Must read hit count before exhausting  ECalculatedHits multiplier counts GetStatuses().
        List<CardModel> statuses = GetStatuses(Owner).ToList();
        int statusCount = (int)((CalculatedVar)DynamicVars[calculatedHitsKey]).Calculate(cardPlay.Target);
        if (statusCount <= 0)
        {
            return;
        }

        foreach (CardModel c in statuses)
        {
            await CardCmd.Exhaust(choiceContext, c);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(statusCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponentsCompat(CombatState)
            .Execute(choiceContext);
    }

    private static IEnumerable<CardModel> GetStatuses(Player owner)
    {
        return owner.PlayerCombatState!.AllCards.Where(static c =>
            c.Type == CardType.Status && c.Pile?.Type != PileType.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
