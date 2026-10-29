using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class MagicToKillTheWitch : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1), new DynamicVar("MajokaThreshold", 100)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<SpecialMagicPower>(), YouDidWell.DerivationTip];
    public MagicToKillTheWitch() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState == null) return;
        if (MagicToKillWitchPresentation.IsEligible(this))
        {
            await MagicToKillWitchPresentation.Play(this);
            if (CombatState == null) return;
        }
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(c => c.IsHittable).ToList())
            await CommonActions.Apply<WeakPower>(context, enemy, this, 1);
        if (Owner.Creature.GetPowerAmount<MajokaPower>() < DynamicVars["MajokaThreshold"].IntValue) return;
        await CommonActions.Apply<SpecialMagicPower>(context, Owner.Creature, this, 1);
        await Derive(context);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
    private async Task Derive(PlayerChoiceContext context)
    {
        // Transforming this card removes it from Play, so its CombatState then becomes null.
        // Retain the combat scope for all remaining transformations and generated cards.
        ICombatState? combat = CombatState;
        if (combat == null) return;
        int level = YouDidWell.Level(Owner);
        List<CardModel> cards = Owner.PlayerCombatState!.AllCards.ToList();
        if (level >= 13)
        {
            foreach (CardModel card in cards)
            {
                if (card.Pile == null || !card.IsTransformable) continue;
                CardModel replacement = combat.CreateCard<DieSakurabaEma>(Owner);
                CardCmd.Upgrade(replacement);
                var result = await CardCmd.Transform(card, replacement);
                // A replacement in the Play pile has no active CardPlay to move it afterwards.
                if (result is { success: true, cardAdded: { } transformed } && transformed.Pile?.Type == PileType.Play)
                    await CardPileCmd.Add(transformed, PileType.Discard);
            }
        }
        else if (level >= 8)
        {
            CardModel? selected = await CardSelectCmd.FromChooseACardScreen(context,
                [combat.CreateCard<AcceptExhaustMagic>(Owner), combat.CreateCard<DeclineExhaustMagic>(Owner)], Owner);
            if (selected is AcceptExhaustMagic)
                foreach (CardModel card in cards) card.AddKeyword(CardKeyword.Exhaust);
        }
        if (level >= 6)
        {
            await Give<CUrOi>(combat);
            await Give<ShiDes>(combat);
        }
        if (level >= 12) await Give<SentKy>(combat);
    }
    private async Task Give<T>(ICombatState combat) where T : CardModel
    {
        await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<T>(Owner), PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
