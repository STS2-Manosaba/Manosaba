using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NikaidoHiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class SmokeOutEvidence : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new PowerVar<SusPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<SusPower>(),
    ];

    public SmokeOutEvidence() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        int maxSelect = Math.Min(DynamicVars.Cards.IntValue, PileType.Hand.GetPile(Owner).Cards.Count);
        if (maxSelect > 0)
        {
            IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, maxSelect),
                null,
                this);

            foreach (CardModel card in selected.ToList())
                await CardCmd.Exhaust(choiceContext, card);
        }

        decimal sus = DynamicVars["SusPower"].BaseValue;
        if (CombatState != null)
        {
            Player? hiro = EmaCardTargeting
                .FindTeammatesByCharacterIds(CombatState, Owner.Creature, NikaidoHiroCharacter.CharacterId)
                .FirstOrDefault();
            if (hiro != null)
                sus += hiro.Creature.GetPowerAmount<SusPower>();
        }

        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, sus);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
