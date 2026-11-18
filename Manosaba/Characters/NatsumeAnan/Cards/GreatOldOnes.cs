using BaseLib.Utils;
using manosaba.Characters.NatsumeAnan.Powers;
using Manosaba.Characters.Common.Commands;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace manosaba.Characters.NatsumeAnan.Cards;

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class BookOfGreatOldOnes : NatsumeKotodamaCardModel
{
    private int _resolvedKotodamaX;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override bool IsPlayable => base.IsPlayable && HasKotodamaForXCostPlay();

    protected override bool ShouldGlowRedInternal => base.ShouldGlowRedInternal || !HasKotodamaForXCostPlay();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        HoverTipFactory.FromPower<SanityPower>(),
        HoverTipFactory.FromCard<PageAzathoth>(),
        HoverTipFactory.FromCard<PageNyarlathotep>(),
        HoverTipFactory.FromCard<PageShubNiggurath>(),
        HoverTipFactory.FromCard<PageYogSothoth>(),
        HoverTipFactory.FromCard<PageHastur>(),
        HoverTipFactory.FromCard<PageCthugha>(),
        HoverTipFactory.FromCard<PageCthulhu>(),
    ];

    public BookOfGreatOldOnes() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    public int PrepareKotodamaXCostForPlay()
    {
        int kotodamaSpent = Math.Max(0, KotodamaEnergy.Get(Owner));
        _resolvedKotodamaX = ResolveKotodamaXValue(kotodamaSpent);
        return kotodamaSpent;
    }

    public void OverrideResolvedKotodamaXCostForPlay(int value)
    {
        _resolvedKotodamaX = Math.Max(0, value);
    }

    private bool HasKotodamaForXCostPlay()
    {
        try
        {
            Player owner = Owner;
            if (owner.Character == null || owner.RunState == null)
            {
                return true;
            }

            return ResolveKotodamaXValue(KotodamaEnergy.Get(owner)) > 0;
        }
        catch (CanonicalModelException)
        {
            return true;
        }
        catch (NullReferenceException)
        {
            return true;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay && _resolvedKotodamaX <= 0)
        {
            _resolvedKotodamaX = ResolveKotodamaXValue(KotodamaEnergy.Get(Owner));
        }

        if (_resolvedKotodamaX > 0)
        {
            await CommonActions.Apply<SanityPower>(choiceContext, Owner.Creature, this, _resolvedKotodamaX);
        }

        await RemoveCurrentDeckHandAndDiscardFromCombat();
        await AddGreatOldOnesDeck();

        if (cardPlay.IsLastInSeries)
        {
            _resolvedKotodamaX = 0;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private async Task RemoveCurrentDeckHandAndDiscardFromCombat()
    {
        List<CardModel> cardsToRemove =
        [
            .. PileType.Draw.GetPile(Owner).Cards,
            .. PileType.Hand.GetPile(Owner).Cards,
            .. PileType.Discard.GetPile(Owner).Cards,
        ];

        List<CardModel> combatPileCards = cardsToRemove
            .Where(card => card.Pile?.Type is PileType.Draw or PileType.Hand or PileType.Discard)
            .ToList();

        if (combatPileCards.Count > 0)
        {
            await CardPileCmd.RemoveFromCombat(combatPileCards);
        }
    }

    private async Task AddGreatOldOnesDeck()
    {
        ICombatState? combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        List<CardModel> cards =
        [
            combatState.CreateCard<PageAzathoth>(Owner),
            combatState.CreateCard<PageAzathoth>(Owner),
            .. CreateMany<PageNyarlathotep>(combatState, 6),
            .. CreateMany<PageShubNiggurath>(combatState, 4),
            combatState.CreateCard<PageYogSothoth>(Owner),
            combatState.CreateCard<PageYogSothoth>(Owner),
            .. CreateMany<PageHastur>(combatState, 4),
            combatState.CreateCard<PageCthugha>(Owner),
            combatState.CreateCard<PageCthugha>(Owner),
            .. CreateMany<PageCthulhu>(combatState, 4),
        ];

        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.MessyLayout);
    }

    private IEnumerable<CardModel> CreateMany<T>(ICombatState combatState, int count) where T : CardModel
    {
        for (int i = 0; i < count; i++)
        {
            yield return combatState.CreateCard<T>(Owner);
        }
    }
}

public abstract class GreatOldOnePageCard : PathCustomCardModel
{
    protected GreatOldOnePageCard(int energyCost, CardType type, TargetType targetType)
        : base(energyCost, type, CardRarity.Ancient, targetType, false)
    {
    }

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageAzathoth : GreatOldOnePageCard
{
    private const string VfxScenePath = "res://Manosaba/scenes/natsume_anan/vfx/azathoth.tscn";
    private const string BgmEventPath = "event:/Manosaba/audio/bgm/azathoth.ogg";

    private static readonly Type[] RequiredPageTypes =
    [
        typeof(PageAzathoth),
        typeof(PageNyarlathotep),
        typeof(PageShubNiggurath),
        typeof(PageYogSothoth),
        typeof(PageHastur),
        typeof(PageCthugha),
        typeof(PageCthulhu),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public PageAzathoth() : base(1, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        _ = oldPileType;
        _ = source;

        if (card.Owner == Owner && card.Pile?.Type == PileType.Exhaust)
        {
            TryStartAzathothBgm();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;

        if (side != CombatSide.Player || CombatState == null || !HasAllGreatOldOnePagesInExhaust())
        {
            return;
        }

        TryStartAzathothBgm();
        await ManosabaVfxCmd.PlaySceneAtCombatCenterAndWait(
            VfxScenePath,
            fitCoverViewport: true,
            spriteNodeNames: ["StillA", "StillB"]);
        await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape(CombatState);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    private bool HasAllGreatOldOnePagesInExhaust()
    {
        HashSet<Type> exhaustedPageTypes = PileType.Exhaust.GetPile(Owner)
            .Cards
            .Select(card => card.GetType())
            .Where(type => RequiredPageTypes.Contains(type))
            .ToHashSet();

        return RequiredPageTypes.All(exhaustedPageTypes.Contains);
    }

    private void TryStartAzathothBgm()
    {
        if (CombatState != null && HasAllGreatOldOnePagesInExhaust())
        {
            SfxCmd.Play(BgmEventPath, 0.8f);
        }
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageNyarlathotep : GreatOldOnePageCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<ShadowFromTheSteeplePower>(1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShadowFromTheSteeplePower>()];

    public PageNyarlathotep() : base(1, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        await CommonActions.Apply<ShadowFromTheSteeplePower>(choiceContext, cardPlay.Target, this, DynamicVars["ShadowFromTheSteeplePower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageShubNiggurath : GreatOldOnePageCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heal", 5m),
        new BlockVar(10m, ValueProp.Move),
    ];

    public PageShubNiggurath() : base(1, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageYogSothoth : GreatOldOnePageCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Stun)];

    public PageYogSothoth() : base(3, CardType.Skill, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        ICombatState? combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (Creature enemy in combatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToList())
        {
            await CreatureCmd.Stun(enemy);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageHastur : GreatOldOnePageCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("Hits", 5m),
    ];

    public PageHastur() : base(2, CardType.Attack, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        ICombatState? combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponentsCompat(combatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Hits"].UpgradeValueBy(2m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageCthugha : GreatOldOnePageCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(25m, ValueProp.Move)];

    public PageCthugha() : base(3, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;

        if (side == CombatSide.Player && Pile?.Type == PileType.Exhaust)
        {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class PageCthulhu : GreatOldOnePageCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CallOfCthulhuPower>()];

    public PageCthulhu() : base(2, CardType.Skill, TargetType.AnyPlayer)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;

        Player? targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null)
        {
            return;
        }

        await CommonActions.Apply<CallOfCthulhuPower>(choiceContext, targetPlayer.Creature, this, 1m);
        if (IsUpgraded && targetPlayer != Owner)
        {
            await CommonActions.Apply<CallOfCthulhuPower>(choiceContext, Owner.Creature, this, 1m);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
