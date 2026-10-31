using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.KurobeNanoka.Helpers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class DeckShot : GunBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ExhaustedCards", 3m), new DynamicVar("BulletCost", 1m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.GunShot, CardKeyword.Exhaust];

    public DeckShot() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!TrySpendBulletsOnPlay())
            return;

        NanokaHelper.PlayGunFireSfx();
        List<CardModel> cards = PileType.Draw.GetPile(Owner).Cards.Take(3).ToList();
        foreach (CardModel card in cards)
            await CardCmd.Exhaust(context, card);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
