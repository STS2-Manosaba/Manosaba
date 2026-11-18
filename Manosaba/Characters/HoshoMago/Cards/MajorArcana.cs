using BaseLib.Utils;
using Godot;
using manosaba.Characters.HoshoMago;
using manosaba.Extensions;
using Manosaba.Characters.Common.Commands;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.HikamiMeruru.Powers;
using Manosaba.Characters.HoshoMago.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.HoshoMago.Cards;

public abstract class HoshoMagoArcanaBase : PathCustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [ManosabaCardTags.Tarot];
    protected HoshoMagoArcanaBase(
        int energyCost = 1,
        CardType cardType = CardType.Skill,
        TargetType targetType = TargetType.Self)
        : base(energyCost, cardType, CardRarity.Ancient, targetType, true)
    {
    }

    // Route Major Arcana cards to their dedicated tarot art folder.
    public override string PortraitPath => Path.Join("tarot", Path.GetFileName(base.PortraitPath)).CardsImagePath();

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }

    protected static bool HasSunMoonStarSynergy(IEnumerable<CardModel>? deckCards)
    {
        if (deckCards == null)
        {
            return false;
        }

        bool hasSun = false;
        bool hasMoon = false;
        bool hasStar = false;
        foreach (CardModel card in deckCards)
        {
            if (!hasSun && card is TheSun)
            {
                hasSun = true;
            }
            else if (!hasMoon && card is TheMoon)
            {
                hasMoon = true;
            }
            else if (!hasStar && card is TheStar)
            {
                hasStar = true;
            }

            if (hasSun && hasMoon && hasStar)
            {
                return true;
            }
        }

        return false;
    }

    protected static bool HasHierophantPriestessJusticeSynergy(IEnumerable<CardModel>? deckCards)
    {
        if (deckCards == null)
        {
            return false;
        }

        bool hasHierophant = false;
        bool hasPriestess = false;
        bool hasJustice = false;
        foreach (CardModel card in deckCards)
        {
            if (!hasHierophant && card is TheHierophant)
            {
                hasHierophant = true;
            }
            else if (!hasPriestess && card is TheHighPriestess)
            {
                hasPriestess = true;
            }
            else if (!hasJustice && card is Justice)
            {
                hasJustice = true;
            }

            if (hasHierophant && hasPriestess && hasJustice)
            {
                return true;
            }
        }

        return false;
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheFool : HoshoMagoArcanaBase
{
    public TheFool() : base(0)
    {
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, HoverTipFactory.FromPower<DrawCardsNextTurnPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2),
        new DynamicVar("WorldEnergy", 10m),
        new DynamicVar("WorldCards", 5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        bool hasTheWorldInDeck = Owner.Deck?.Cards?.Any(card => card is TheWorld) == true;
        if (hasTheWorldInDeck)
        {
            await CommonActions.Apply<EnergyNextTurnPower>(choiceContext, ownerCreature, this, DynamicVars["WorldEnergy"].BaseValue);
            await CommonActions.Apply<DrawCardsNextTurnPower>(choiceContext, ownerCreature, this, DynamicVars["WorldCards"].BaseValue);
        }
        else
        {
            await CommonActions.Apply<EnergyNextTurnPower>(choiceContext, ownerCreature, this, DynamicVars.Energy.IntValue);
        }

        await CommonActions.Apply<TheFoolPower>(choiceContext, ownerCreature, this, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheMagician : HoshoMagoArcanaBase
{
    public TheMagician() : base(1)
    {
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { Creature: { } ownerCreature, Deck.Cards: { } deckCards })
        {
            return;
        }

        int uniqueTarotCardsInDeck = deckCards
            .Where(card => card.Tags.Contains(ManosabaCardTags.Tarot))
            .Select(card => card.Id)
            .Distinct()
            .Count();
        decimal majokaToGain = uniqueTarotCardsInDeck * DynamicVars["MajokaPower"].BaseValue;
        if (majokaToGain <= 0)
        {
            return;
        }

        await CommonActions.Apply<MajokaPower>(choiceContext, ownerCreature, this, majokaToGain);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["MajokaPower"].UpgradeValueBy(2m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheHighPriestess : HoshoMagoArcanaBase
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(7m),
        new CalculationExtraVar(4m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
            PileType.Hand.GetPile(card.Owner).Cards.Count(c => c.Type == CardType.Status))
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        List<CardModel> statusesInHand = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Status)
            .ToList();
        decimal blockToGain = DynamicVars.CalculatedBlock.Calculate(cardPlay.Target);

        foreach (CardModel status in statusesInHand)
        {
            await CardCmd.Exhaust(choiceContext, status);
        }

        await CreatureCmd.GainBlock(ownerCreature, blockToGain, DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheEmpress : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(6m),
        new ExtraDamageVar(3m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
            card.Owner?.Deck?.Cards.Any(deckCard => deckCard is TheEmperor) == true ? 1m : 0m)
    ];

    public TheEmpress() : base(1, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        AttackCommand attackCommand = await DamageCmd
            .Attack(DynamicVars.CalculatedDamage)
            .Targeting(target)
            .FromCard(this, cardPlay)
            .Execute(choiceContext);
        IEnumerable<DamageResult> results = attackCommand.Results.SelectMany(hit => hit);
        int totalUnblockedDamage = results.Where(result => result.Receiver.IsEnemy).Sum(result => result.UnblockedDamage);
        if (totalUnblockedDamage > 0)
        {
            await CreatureCmd.Heal(ownerCreature, totalUnblockedDamage * 0.5m);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1.5m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheEmperor : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(20m),
        new ExtraDamageVar(10m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
            card.Owner?.Deck?.Cards.Any(deckCard => deckCard is TheEmpress) == true ? 1m : 0m)
    ];

    public TheEmperor() : base(2, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(10m);
        DynamicVars.ExtraDamage.UpgradeValueBy(5m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheHierophant : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            context: choiceContext,
            player: Owner,
            filter: null,
            source: this)).FirstOrDefault();

        if (selected != null)
        {
            await CardCmd.Exhaust(choiceContext, selected);
        }

        List<Creature> enemies = combatState.GetOpponentsOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive)
            .ToList();

        foreach (Creature enemy in enemies)
        {
            await CommonActions.Apply<WeakPower>(choiceContext, enemy, this, DynamicVars.Weak.BaseValue);
            await CommonActions.Apply<VulnerablePower>(choiceContext, enemy, this, DynamicVars.Vulnerable.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheLovers : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<TheLoversPower>(25), new DynamicVar("DevilBonus", 10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TheLoversPower>()];

    public TheLovers() : base(1, CardType.Skill, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        decimal loversToApply = DynamicVars["TheLoversPower"].BaseValue;
        if (Owner.Deck?.Cards?.Any(card => card is TheDevil) == true)
        {
            loversToApply += DynamicVars["DevilBonus"].BaseValue;
        }

        await CommonActions.Apply<TheLoversPower>(choiceContext, target, this, loversToApply);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheChariot : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new RepeatVar(3), new DynamicVar("StrengthRepeatBonus", 1m)];

    public TheChariot() : base(2, CardType.Attack, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        int hitCount = DynamicVars.Repeat.IntValue;
        if (Owner.Deck?.Cards?.Any(card => card is Strength) == true)
        {
            hitCount += DynamicVars["StrengthRepeatBonus"].IntValue;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponentsCompat(CombatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(2m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class Strength : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public Strength() : base(1, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<StrengthPower>(choiceContext, ownerCreature, this, DynamicVars["StrengthPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(2m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheHermit : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    public TheHermit() : base(2, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<IntangiblePower>(choiceContext, ownerCreature, this, DynamicVars["IntangiblePower"].BaseValue);

        IEnumerable<CardModel>? deckCards = Owner.Deck?.Cards;
        bool hasMoonAndPriestess = deckCards != null
                                   && deckCards.Any(card => card is TheMoon)
                                   && deckCards.Any(card => card is TheHighPriestess);
        if (!hasMoonAndPriestess || CombatState == null)
        {
            return;
        }

        List<Creature> teammates = CombatState.GetTeammatesOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive && creature.IsPlayer && creature != ownerCreature)
            .ToList();
        if (teammates.Count == 0)
        {
            return;
        }

        Creature? teammate = Owner.RunState.Rng.CombatTargets.NextItem(teammates);
        if (teammate == null)
        {
            return;
        }

        await CommonActions.Apply<IntangiblePower>(choiceContext, teammate, this, DynamicVars["IntangiblePower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class WheelOfFortune : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4), new DynamicVar("HangedManCards", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int drawCount = DynamicVars.Cards.IntValue;
        if (Owner.Deck?.Cards?.Any(card => card is TheHangedMan) == true)
        {
            drawCount += DynamicVars["HangedManCards"].IntValue;
        }

        int handCount = PileType.Hand.GetPile(Owner).Cards.Count;
        int maxSelect = Math.Min(drawCount, handCount);

        if (maxSelect > 0)
        {
            CardSelectorPrefs prefs = new(new LocString("cards", "MANOSABA-WHEEL_OF_FORTUNE.selectionScreenPrompt"), 0, maxSelect);
            IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
            List<CardModel> selectedList = selectedCards.ToList();

            if (selectedList.Count > 0)
            {
                await CardPileCmd.Add(selectedList, PileType.Draw, CardPilePosition.Random, this);
            }
        }

        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class Justice : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<InhibitionPower>(1), new BlockVar("TemperanceBlock", 3m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<InhibitionPower>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Justice() : base(1, CardType.Skill, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> cardsToExhaust = PileType.Hand.GetPile(Owner).Cards
            .ToList();
        int exhaustedCount = cardsToExhaust.Count;

        foreach (CardModel card in cardsToExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        if (exhaustedCount <= 0)
        {
            return;
        }

        decimal perCardInhibition = DynamicVars["InhibitionPower"].BaseValue;
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<InhibitionPower>(choiceContext, ownerCreature, this, exhaustedCount * perCardInhibition);
        if (Owner.Deck?.Cards?.Any(card => card is Temperance) == true)
        {
            await CreatureCmd.GainBlock(ownerCreature, exhaustedCount * DynamicVars["TemperanceBlock"].BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheHangedMan : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public TheHangedMan() : base(0, CardType.Skill, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? target = cardPlay.Target;
        if (target == null)
        {
            return;
        }

        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<StrengthPower>(choiceContext, target, this, -DynamicVars["StrengthPower"].BaseValue);

        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null)
        {
            return;
        }

        float liftY = node.Hitbox.Size.Y;

        Vector2 scale = node.Visuals.GetCurrentBody().Scale;
        node.Visuals.GetCurrentBody().Scale = new Vector2(scale.X, -Mathf.Abs(scale.Y));
        node.Visuals.GetCurrentBody().Position = new Vector2(
            node.Visuals.GetCurrentBody().Position.X,
            node.Visuals.GetCurrentBody().Position.Y - liftY
        );
    }
    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class Death : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AllyDoom", 5m),
        new DynamicVar("EnemyDoom", 20m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public Death() : base(1, CardType.Skill, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        decimal enemyBaseDoom = DynamicVars["EnemyDoom"].BaseValue;
        decimal allyBaseDoom = DynamicVars["AllyDoom"].BaseValue;
        bool hasTheMagicianInDeck = Owner?.Deck?.Cards?.Any(card => card is TheMagician) == true;

        List<Creature> enemies = CombatState.GetOpponentsOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive)
            .ToList();
        List<Creature> allies = CombatState.GetTeammatesOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive && creature.IsPlayer)
            .ToList();

        if (enemies.Count > 0)
        {
            foreach (Creature enemy in enemies)
            {
                await CommonActions.Apply<DoomPower>(choiceContext, enemy, this, enemyBaseDoom);
            }
        }

        if (allies.Count > 0)
        {
            foreach (Creature ally in allies)
            {
                await CommonActions.Apply<DoomPower>(choiceContext, ally, this, allyBaseDoom);
            }
        }

        if (!hasTheMagicianInDeck || enemies.Count <= 0)
        {
            return;
        }

        foreach (Creature enemy in enemies)
        {
            decimal currentDoom = enemy.GetPowerAmount<DoomPower>();
            if (currentDoom <= 0m)
            {
                continue;
            }

            await CommonActions.Apply<DoomPower>(choiceContext, enemy, this, currentDoom * 0.5m);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EnemyDoom"].UpgradeValueBy(10m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class Temperance : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<TemperancePower>(30), new BlockVar("JusticeBlock", 10m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TemperancePower>(), HoverTipFactory.Static(StaticHoverTip.Block)];

    public Temperance() : base(3, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<TemperancePower>(choiceContext, ownerCreature, this, DynamicVars["TemperancePower"].BaseValue);
        if (Owner.Deck?.Cards?.Any(card => card is Justice) == true)
        {
            await CreatureCmd.GainBlock(ownerCreature, DynamicVars["JusticeBlock"].BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheDevil : HoshoMagoArcanaBase
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<TheDevilEyeOfNoEscapeToken>(),
        HoverTipFactory.FromCard<TheDevilAwakenedMadnessPowerToken>(),
        HoverTipFactory.FromCard<TheDevilBestowTrialToken>()
    ];

    public TheDevil() : base(1, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> options =
        [
            combatState.CreateCard<TheDevilEyeOfNoEscapeToken>(Owner),
            combatState.CreateCard<TheDevilAwakenedMadnessPowerToken>(Owner),
            combatState.CreateCard<TheDevilBestowTrialToken>(Owner)
        ];

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
        if (selected == null)
        {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, selected, null);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheTower : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new PowerVar<TheTowerPower>(1), new DynamicVar("DeathDoom", 10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ArtifactPower>(), HoverTipFactory.FromPower<TheTowerPower>(), HoverTipFactory.FromPower<DoomPower>()];

    public TheTower() : base(2, CardType.Attack, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        List<Creature> enemies = combatState.GetOpponentsOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive)
            .ToList();

        if (enemies.Count > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponentsCompat(combatState)
                .Execute(choiceContext);
        }

        bool hasDeathInDeck = Owner.Deck?.Cards?.Any(card => card is Death) == true;
        foreach (Creature enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            await PowerCmd.Remove<ArtifactPower>(enemy);
            await CommonActions.Apply<TheTowerPower>(choiceContext, enemy, this, DynamicVars["TheTowerPower"].BaseValue);
            if (hasDeathInDeck)
            {
                await CommonActions.Apply<DoomPower>(choiceContext, enemy, this, DynamicVars["DeathDoom"].BaseValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["TheTowerPower"].UpgradeValueBy(1m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheStar : HoshoMagoArcanaBase
{
    private const decimal ReflectMultiplier = 1m;

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(16m, ValueProp.Move), new DynamicVar("ReflectMultiplier", ReflectMultiplier)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block), HoverTipFactory.FromPower<CoveredPower>(), HoverTipFactory.FromPower<TheStarPower>()];

    public TheStar() : base(2, CardType.Skill, TargetType.AnyPlayer)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        decimal blockToGain = DynamicVars.Block.BaseValue;
        if (HasSunMoonStarSynergy(Owner?.Deck?.Cards))
        {
            blockToGain *= 2m;
        }

        await CreatureCmd.GainBlock(ownerCreature, blockToGain, DynamicVars.Block.Props, cardPlay);

        if (target != ownerCreature && target.IsPlayer && target.IsAlive)
        {
            await CommonActions.Apply<CoveredPower>(choiceContext, target, this, 1m);
        }

        await CommonActions.Apply<TheStarPower>(choiceContext, ownerCreature, this, TheStarPower.EncodeReflectMultiplier(DynamicVars["ReflectMultiplier"].BaseValue));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars["ReflectMultiplier"].UpgradeValueBy(0.5m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheMoon : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(5m),
        new ExtraDamageVar(25m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? target) =>
        {
            if (target == null)
            {
                return 0m;
            }

            decimal totalStacks = target.GetPowerAmount<PoisonPower>()
                + target.GetPowerAmount<DoomPower>()
                + target.GetPowerAmount<BurnPower>();
            if (totalStacks <= 0m)
            {
                return 0m;
            }

            bool hasSunMoonStar = HasSunMoonStarSynergy(card.Owner?.Deck?.Cards);
            decimal synergyMultiplier = hasSunMoonStar ? 2m : 1m;

            return totalStacks * synergyMultiplier / 100m;
        })
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<PoisonPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<BurnPower>()
    ];

    public TheMoon() : base(1, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? target = cardPlay.Target;
        if (target == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(15m);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheSun : HoshoMagoArcanaBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<TheSunPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TheSunPower>()];

    public TheSun() : base(2, CardType.Power, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        bool hasSunMoonStar = HasSunMoonStarSynergy(Owner?.Deck?.Cards);

        if (!hasSunMoonStar)
        {
            await CommonActions.Apply<TheSunPower>(choiceContext, ownerCreature, this, DynamicVars["TheSunPower"].BaseValue);
            return;
        }

        if (CombatState == null)
        {
            return;
        }

        List<Creature> allies = CombatState.GetTeammatesOf(ownerCreature)
            .Where(creature => creature != null && creature.IsAlive && creature.IsPlayer)
            .ToList();

        foreach (Creature ally in allies)
        {
            await CommonActions.Apply<TheSunPower>(choiceContext, ally, this, DynamicVars["TheSunPower"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class Judgement : HoshoMagoArcanaBase
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public Judgement() : base(1, CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        if (Owner is not { Creature: { } ownerCreature } owner)
        {
            return;
        }

        List<PowerModel> debuffs = ownerCreature.Powers
            .Where(power => power != null && power.Type == PowerType.Debuff)
            .ToList();

        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        List<PowerModel> negBuffs = ownerCreature.Powers
            .Where(power => power != null && power.Type == PowerType.Buff && power.Amount < 0)
            .ToList();

        foreach (PowerModel debuff in negBuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        if (debuffs.Count > 0)
        {
            bool hasHierophantPriestessJustice = HasHierophantPriestessJusticeSynergy(owner.Deck?.Cards);

            int energyGain = debuffs.Count;
            if (hasHierophantPriestessJustice)
            {
                energyGain *= 2;
            }

            await PlayerCmd.GainEnergy(energyGain, owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(HoshoMagoCardPool))]
public sealed class TheWorld : HoshoMagoArcanaBase
{
    private const string VfxScenePath = "res://Manosaba/scenes/hosho_mago/vfx/the_world.tscn";
    private const string BloomBgmEventPath = "event:/Manosaba/audio/bgm/bloom.mp3";
    private bool _bloomReadyLastCheck;
    private bool _bloomReadyCheckQueued;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Innate];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override bool ShouldGlowGoldInternal => IsWorldWinReadyForPlay();

    public TheWorld() : base(2)
    {
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (ReferenceEquals(card, this))
        {
            _bloomReadyLastCheck = false;
            _bloomReadyCheckQueued = false;
        }

        TryPlayBloomBgmWhenReady();
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        _ = oldPileType;
        _ = source;

        if (card.Owner == Owner)
        {
            TryPlayBloomBgmWhenReady();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        _ = choiceContext;
        _ = fromHandDraw;

        if (card.Owner == Owner)
        {
            TryPlayBloomBgmWhenReady();
        }

        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player == Owner)
        {
            TryPlayBloomBgmWhenReady();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ = room;
        _bloomReadyLastCheck = false;
        _bloomReadyCheckQueued = false;
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || !IsWorldWinReadyForPlay())
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        if (PileType.Draw.GetPile(Owner).Cards.Count == 0)
        {
            PlayBloomBgm();
            await ManosabaVfxCmd.PlaySceneAtCombatCenterAndWait(VfxScenePath, fitCoverViewport: true, spriteNodeNames: ["StillA", "StillB"]);
            await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape(CombatState);
            return;
        }

        if (Owner?.PlayerCombatState == null)
        {
            return;
        }

        foreach (CardModel card in Owner.PlayerCombatState.AllCards)
        {
            if (card != this && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private void TryPlayBloomBgmWhenReady()
    {
        bool ready = IsWorldWinReadyForPlay();
        if (ready && !IsWorldPlayableForBloom())
        {
            QueueBloomReadyCheckAfterCurrentActions();
            _bloomReadyLastCheck = false;
            return;
        }

        if (ready && !_bloomReadyLastCheck)
        {
            PlayBloomBgm();
            return;
        }

        _bloomReadyLastCheck = ready;
    }

    private bool IsWorldPlayableForBloom()
    {
        if (RunManager.Instance.ActionQueueSynchronizer.CombatState != ActionSynchronizerCombatState.PlayPhase
            || CombatManager.Instance.IsOverOrEnding || Owner == null)
        {
            return false;
        }

        if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction != null)
        {
            return false;
        }

        if (CombatManager.Instance.IsPlayerReadyToEndTurn(Owner))
        {
            return false;
        }

        return CanPlay();
    }

    private void QueueBloomReadyCheckAfterCurrentActions()
    {
        if (_bloomReadyCheckQueued)
        {
            return;
        }

        ActionExecutor actionExecutor = RunManager.Instance.ActionExecutor;
        if (!actionExecutor.IsRunning)
        {
            return;
        }

        _bloomReadyCheckQueued = true;
        _ = CheckBloomReadyAfterCurrentActions(actionExecutor);
    }

    private async Task CheckBloomReadyAfterCurrentActions(ActionExecutor actionExecutor)
    {
        await actionExecutor.FinishedExecutingActions();
        _bloomReadyCheckQueued = false;
        TryPlayBloomBgmWhenReady();
    }

    private void PlayBloomBgm()
    {
        if (_bloomReadyLastCheck)
        {
            return;
        }

        SfxCmd.Play(BloomBgmEventPath, 0.8f);
        _bloomReadyLastCheck = true;
    }

    private bool IsWorldWinReadyForPlay()
    {
        if (CombatState == null || Owner?.PlayerCombatState == null)
        {
            return false;
        }

        return PileType.Hand.GetPile(Owner).Cards.Contains(this)
               && PileType.Draw.GetPile(Owner).Cards.Count == 0;
    }
}
