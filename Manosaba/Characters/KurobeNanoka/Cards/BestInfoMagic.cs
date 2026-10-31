using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.KurobeNanoka.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class BestInfoMagic : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BestInfoMagicPower>()];

    public BestInfoMagic() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState)
            return;

        foreach (var player in combatState.Players.Where(player => player.Creature.IsAlive))
            await CommonActions.Apply<BestInfoMagicPower>(context, player.Creature, this, 1m);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
