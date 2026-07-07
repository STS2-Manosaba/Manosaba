using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public class LetsPlayHideAndSeek : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private static readonly LocString SelectionPrompt = new("cards", "MANOSABA-LETS_PLAY_HIDE_AND_SEEK.selectionScreenPrompt");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(20m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MajokaPower>(),
        HoverTipFactory.FromCard<HideInWardrobe>(),
        HoverTipFactory.FromCard<FoundYou>(),
    ];

    public LetsPlayHideAndSeek() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);

        if (CombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> options = SawatariCocoHelper.HideAndSeekCardTypes
            .Select(type =>
            {
                CardModel? canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
                return canonical != null ? combatState.CreateCard(canonical, Owner) : null;
            })
            .OfType<CardModel>()
            .ToList();

        if (options.Count == 0)
        {
            return;
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, canSkip: true);
        if (selected == null)
        {
            return;
        }

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
