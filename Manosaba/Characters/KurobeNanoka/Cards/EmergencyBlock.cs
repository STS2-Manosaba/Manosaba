using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class EmergencyBlock : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    public EmergencyBlock() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        List<CardModel> attacks = PileType.Discard.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList()
            .ToList();
        CardModel? attack = Owner.RunState.Rng.CombatCardSelection.NextItem(attacks);

        if (attack != null)
        {
            CardCmd.ApplyKeyword(attack, [CardKeyword.Exhaust]);
            await CardCmd.AutoPlay(context, attack, null);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
