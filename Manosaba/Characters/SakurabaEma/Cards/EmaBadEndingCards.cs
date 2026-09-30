using BaseLib.Utils;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using HikamiMeruruCharacter = manosaba.Characters.HikamiMeruru.HikamiMeruru;
using SherryCharacter = manosaba.Characters.TachibanaSherry.TachibanaSherry;
using HannaCharacter = manosaba.Characters.TonoHanna.TonoHanna;

namespace manosaba.Characters.SakurabaEma.Cards;

internal static class EmaBadEndingCards
{
    public static readonly Type[] EmaPool =
    [
        typeof(StandInFrontOfTeammate),
        typeof(LockedPunishmentRoom),
        typeof(ManEatingBook),
        typeof(KillTwoBirdsWithOneStone),
        typeof(FallIntoOldWell),
        typeof(HeartyMeal),
        typeof(MediationPunch),
        typeof(ShowerRoomMirror),
        typeof(SuspicionGhosts),
        typeof(WanderingNarehate),
        typeof(MysteriousLandmineGirl),
        typeof(Elope),
        typeof(NotYourFault),
        typeof(EnaSan),
        typeof(AccompliceSakurabaEma),
    ];

    public static readonly Type[] HiroPool =
    [
        typeof(PickUpFirePoker),
        typeof(HideAndSeekBadEnd),
        typeof(TheMistakeIsYours),
        typeof(RoastEvilLittleDog),
        typeof(AnimalFriends),
        typeof(ExchangedGlances),
        typeof(ScapegoatSwitch),
        typeof(FourTwoSixTwoTenFive),
        typeof(PickUpFirePoker),
        typeof(HideAndSeekBadEnd),
        typeof(TheMistakeIsYours),
        typeof(RoastEvilLittleDog),
        typeof(AnimalFriends),
        typeof(ExchangedGlances),
        typeof(ScapegoatSwitch),
        typeof(FourTwoSixTwoTenFive),
        typeof(KillEma),
        typeof(NoWitchRestRitual),
    ];

    public static List<CardModel> CreateRandomOptions(ICombatState combatState, Player owner, int count)
    {
        List<Type> shuffled = EmaPool
            .OrderBy(_ => owner.RunState.Rng.CombatCardSelection.NextInt(int.MaxValue))
            .Take(count)
            .ToList();

        return shuffled
            .Select(type => CreateCardByType(combatState, owner, type))
            .ToList();
    }

    public static CardModel CreateRandomHiroCard(ICombatState combatState, Player owner)
    {
        Type type = owner.RunState.Rng.CombatCardSelection.NextItem(HiroPool.ToList()) ?? HiroPool[0];
        return CreateCardByType(combatState, owner, type);
    }

    private static CardModel CreateCardByType(ICombatState combatState, Player owner, Type type)
    {
        CardModel cardType = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
        return combatState.CreateCard(cardType, owner);
    }
}

public abstract class EmaBadEndingTokenBase : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<BadEnd>(),
        HoverTipFactory.FromPower<BadEndQuestionPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    protected EmaBadEndingTokenBase(int energyCost, CardType type, TargetType targetType)
        : base(energyCost, type, CardRarity.Token, targetType, true)
    {
    }

    protected async Task AddBadEndToDrawPile()
    {
        if (CombatState == null)
            return;

        CardModel badEnd = CombatState.CreateCard<BadEnd>(Owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(badEnd, PileType.Draw, Owner);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected async Task RecordTriggeredBadEnd(PlayerChoiceContext choiceContext)
    {
        BadEndQuestionPower? tracker = Owner.Creature.GetPower<BadEndQuestionPower>();
        if (tracker == null)
        {
            tracker = await CommonActions.Apply<BadEndQuestionPower>(choiceContext, Owner.Creature, this, 1m);
            tracker?.Record(GetType());
            return;
        }

        if (tracker.Record(GetType()))
            await CommonActions.Apply<BadEndQuestionPower>(choiceContext, Owner.Creature, this, 1m);
    }

    protected async Task StartBadEnding(PlayerChoiceContext choiceContext)
    {
        await AddBadEndToDrawPile();
        await RecordTriggeredBadEnd(choiceContext);
    }

    protected static async Task AddCardToHand<TCard>(Player owner, bool ethereal = false, bool exhaust = false)
        where TCard : CardModel, new()
    {
        if (owner.Creature?.CombatState == null)
            return;

        CardModel card = owner.Creature.CombatState.CreateCard<TCard>(owner);
        if (ethereal)
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
        if (exhaust)
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected static async Task AddCardToDraw<TCard>(Player owner, bool ethereal = false, bool exhaust = false)
        where TCard : CardModel, new()
    {
        if (owner.Creature?.CombatState == null)
            return;

        CardModel card = owner.Creature.CombatState.CreateCard<TCard>(owner);
        if (ethereal)
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
        if (exhaust)
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner);
        CardCmd.PreviewCardPileAdd(result);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class StandInFrontOfTeammate : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move)];

    public StandInFrontOfTeammate() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await StartBadEnding(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class LockedPunishmentRoom : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public LockedPunishmentRoom() : base(0, CardType.Skill, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, DynamicVars.Cards.IntValue),
            null,
            this);
        await CardCmd.Discard(choiceContext, selected);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class ManEatingBook : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromCard<LivingPage>());

    public ManEatingBook() : base(0, CardType.Attack, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            await AddCardToHand<LivingPage>(Owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class KillTwoBirdsWithOneStone : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move), new DynamicVar("Hits", 2m)];

    public KillTwoBirdsWithOneStone() : base(0, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await StartBadEnding(choiceContext);
        for (int i = 0; i < DynamicVars["Hits"].IntValue; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class FallIntoOldWell : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(10m), new PowerVar<BleedPower>(20m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat([HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<BleedPower>()]);

    public FallIntoOldWell() : base(0, CardType.Skill, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await StartBadEnding(choiceContext);
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);
        await CommonActions.Apply<BleedPower>(choiceContext, cardPlay.Target, this, DynamicVars["BleedPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class HeartyMeal : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Heal", 3m), new PowerVar<VigorPower>(4m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());

    public HeartyMeal() : base(0, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await CommonActions.Apply<VigorPower>(choiceContext, Owner.Creature, this, DynamicVars["VigorPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class MediationPunch : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3m), new PowerVar<SusPower>(2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat([HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<SusPower>()]);

    public MediationPunch() : base(0, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        await CommonActions.Apply<StrengthPower>(choiceContext, Owner.Creature, this, DynamicVars["StrengthPower"].BaseValue);
        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class ShowerRoomMirror : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2), new PowerVar<MirrorPersonPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat([EnergyHoverTip, HoverTipFactory.FromPower<MirrorPersonPower>()]);

    public ShowerRoomMirror() : base(0, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        await CommonActions.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, this, DynamicVars.Energy.BaseValue);
        await CommonActions.Apply<MirrorPersonPower>(choiceContext, Owner.Creature, this, DynamicVars["MirrorPersonPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class SuspicionGhosts : EmaBadEndingTokenBase
{
    public SuspicionGhosts() : base(0, CardType.Power, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class WanderingNarehate : EmaBadEndingTokenBase
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18m, ValueProp.Move)];

    public WanderingNarehate() : base(-1, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await StartBadEnding(choiceContext);
        int hits = ResolveEnergyXValue();
        for (int i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class MysteriousLandmineGirl : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m, ValueProp.Move)];

    public MysteriousLandmineGirl() : base(0, CardType.Attack, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (CombatState == null)
            return;

        await StartBadEnding(choiceContext);
        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class Elope : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<HannaPuppetPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<HannaPuppetPower>());

    public Elope() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        decimal turns = DynamicVars["HannaPuppetPower"].BaseValue;
        await CommonActions.Apply<HannaPuppetPower>(choiceContext, Owner.Creature, this, turns);

        if (CombatState == null)
            return;

        foreach (Creature teammate in CombatState.GetTeammatesOf(Owner.Creature).Where(c => c is { IsAlive: true, CanReceivePowers: true }))
        {
            if (teammate.Player?.Character is SherryCharacter or HannaCharacter)
                await CommonActions.Apply<HannaPuppetPower>(choiceContext, teammate, this, turns);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class NotYourFault : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurnPower>(10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<BurnPower>());

    public NotYourFault() : base(0, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await StartBadEnding(choiceContext);
        await CommonActions.Apply<BurnPower>(choiceContext, cardPlay.Target, this, DynamicVars["BurnPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class EnaSan : EmaBadEndingTokenBase
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat([HoverTipFactory.FromPotion<PotionShapedRock>(), HoverTipFactory.FromPower<WontForgetEveryonePower>()]);
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public EnaSan() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await StartBadEnding(choiceContext);
        await PotionCmd.TryToProcure<PotionShapedRock>(Owner);

        if (CombatState == null)
            return;

        foreach (Player meruru in EmaCardTargeting.FindTeammatesByCharacterIds(CombatState, Owner.Creature, HikamiMeruruCharacter.CharacterId))
            await CommonActions.Apply<WontForgetEveryonePower>(choiceContext, meruru.Creature, this, 1m);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class AccompliceSakurabaEma : EmaBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<AccomplicePower>());
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public AccompliceSakurabaEma() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await StartBadEnding(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (CombatState == null)
            return;

        foreach (Player meruru in EmaCardTargeting.FindTeammatesByCharacterIds(CombatState, Owner.Creature, HikamiMeruruCharacter.CharacterId))
            await PowerCmd.Apply<AccomplicePower>(choiceContext, meruru.Creature, 1m, Owner.Creature, this);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class EmaIsEvil : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public EmaIsEvil() : base(0, CardType.Skill, CardRarity.Token, TargetType.None, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        if (CombatState == null)
            return;

        CardModel card = EmaBadEndingCards.CreateRandomHiroCard(CombatState, Owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(result);
    }
}

public abstract class HiroBadEndingTokenBase : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected HiroBadEndingTokenBase(int energyCost, CardType type, TargetType targetType)
        : base(energyCost, type, CardRarity.Token, targetType, true)
    {
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class PickUpFirePoker : HiroBadEndingTokenBase
{
    public PickUpFirePoker() : base(1, CardType.Skill, TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        CardModel? attack = PileType.Draw.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack && card.GetEnchantedReplayCount() <= 0)
            .OrderBy(_ => Owner.RunState.Rng.CombatCardSelection.NextInt(int.MaxValue))
            .FirstOrDefault();
        if (attack != null)
            attack.BaseReplayCount += 1;
        return Task.CompletedTask;
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class HideAndSeekBadEnd : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DexterityPower>(2m)];

    public HideAndSeekBadEnd() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CommonActions.Apply<DexterityPower>(choiceContext, Owner.Creature, this, DynamicVars["DexterityPower"].BaseValue);
}

[Pool(typeof(TokenCardPool))]
public sealed class TheMistakeIsYours : HiroBadEndingTokenBase
{
    public TheMistakeIsYours() : base(0, CardType.Skill, TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        return Task.CompletedTask;
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class RoastEvilLittleDog : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(20m), new PowerVar<DoomPower>(10m)];

    public RoastEvilLittleDog() : base(0, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);
        if (CombatState == null)
            return;

        foreach (Creature ema in CombatState.Players.Select(player => player.Creature)
                     .Where(creature => creature.Player?.Character.Id.Entry.EndsWith(SakurabaEma.CharacterId, StringComparison.OrdinalIgnoreCase) == true))
        {
            await CommonActions.Apply<DoomPower>(choiceContext, ema, this, DynamicVars["DoomPower"].BaseValue);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class AnimalFriends : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public AnimalFriends() : base(0, CardType.Skill, TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        return CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class ExchangedGlances : HiroBadEndingTokenBase
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<ForgeSpear>()];

    public ExchangedGlances() : base(1, CardType.Skill, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        await AddCardToDraw<ForgeSpear>(Owner);
    }

    private static async Task AddCardToDraw<TCard>(Player owner) where TCard : CardModel, new()
    {
        if (owner.Creature?.CombatState == null)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(owner.Creature.CombatState.CreateCard<TCard>(owner), PileType.Draw, owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class ScapegoatSwitch : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(1m)];

    public ScapegoatSwitch() : base(1, CardType.Power, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        return CommonActions.Apply<IntangiblePower>(choiceContext, Owner.Creature, this, DynamicVars["IntangiblePower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class FourTwoSixTwoTenFive : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip];

    public FourTwoSixTwoTenFive() : base(0, CardType.Power, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class KillEma : HiroBadEndingTokenBase
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new DynamicVar("Threshold", 15m)];

    public KillEma() : base(0, CardType.Skill, TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        return CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class NoWitchRestRitual : HiroBadEndingTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(20m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<LaboursOfHiro>(), HoverTipFactory.FromPower<MajokaPower>()];

    public NoWitchRestRitual() : base(0, CardType.Skill, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);

        CardModel? labours = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault(card => card is LaboursOfHiro);
        if (labours != null)
            await CardCmd.AutoPlay(choiceContext, labours, null);
    }
}
