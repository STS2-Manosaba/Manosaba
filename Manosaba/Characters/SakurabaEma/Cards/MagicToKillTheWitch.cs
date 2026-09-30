using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class MagicToKillTheWitch : PathCustomCardModel
{
    private const decimal SpecialMagicMajokaThreshold = 100m;
    private const decimal MajokaPerAdditionalWeak = 25m;
    private const decimal MaxWeakScalingMajoka = 100m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(1m),
        new DynamicVar("MajokaPerAdditionalWeak", MajokaPerAdditionalWeak),
        new DynamicVar("MaxWeakScalingMajoka", MaxWeakScalingMajoka),
        new DynamicVar("MajokaThreshold", SpecialMagicMajokaThreshold),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<MajokaPower>(),
        HoverTipFactory.FromPower<SpecialMagicPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Eternal),
    ];

    public MagicToKillTheWitch() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        decimal weakAmount = CalculateWeakAmount(ownerCreature);
        foreach (Creature enemy in CombatState.GetOpponentsOf(ownerCreature).Where(enemy => enemy.IsHittable))
        {
            await CommonActions.Apply<WeakPower>(choiceContext, enemy, this, weakAmount);
        }

        if (ownerCreature.GetPowerAmount<MajokaPower>() < DynamicVars["MajokaThreshold"].BaseValue)
        {
            return;
        }

        await ExhaustCombatPiles(choiceContext);
        await CommonActions.Apply<SpecialMagicPower>(choiceContext, ownerCreature, this, 1m);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    private decimal CalculateWeakAmount(Creature ownerCreature)
    {
        decimal majoka = Math.Min(MaxWeakScalingMajoka, ownerCreature.GetPowerAmount<MajokaPower>());
        return DynamicVars["WeakPower"].BaseValue + Math.Floor(majoka / MajokaPerAdditionalWeak);
    }

    private async Task ExhaustCombatPiles(PlayerChoiceContext choiceContext)
    {
        foreach (PileType pileType in new[] { PileType.Hand, PileType.Discard, PileType.Draw })
        {
            List<CardModel> cards = pileType.GetPile(Owner).Cards.ToList();
            foreach (CardModel card in cards)
            {
                if (ReferenceEquals(card, this))
                {
                    continue;
                }

                await CardCmd.Exhaust(choiceContext, card);
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
