using BaseLib.Utils;
using Manosaba.Characters.Common.Commands;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class DieSakurabaEma : PathCustomCardModel
{
    private const decimal VictoryMajokaThreshold = 200m;
    private bool _endTurnAutoPlay;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new PowerVar<DoomPower>(10m),
        new DynamicVar("MajokaThreshold", VictoryMajokaThreshold),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<MajokaPower>(),
    ];

    public DieSakurabaEma() : base(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState is not { } combat || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = combat.GetOpponentsOf(ownerCreature)
            .Where(enemy => enemy.IsHittable)
            .ToList();

        foreach (Creature enemy in enemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }

        foreach (Creature enemy in enemies.Where(enemy => enemy.IsAlive))
        {
            await CommonActions.Apply<DoomPower>(choiceContext, enemy, this, DynamicVars["DoomPower"].BaseValue);
        }

        if (_endTurnAutoPlay && cardPlay.IsAutoPlay && !CombatManager.Instance.IsOverOrEnding
            && HasRequiredMajoka(combat, ownerCreature))
        {
            await EmaSpecialVictoryPresentation.PlayDie();
            if (ownerCreature.CombatState == combat && !CombatManager.Instance.IsOverOrEnding)
                await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape(combat);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || CombatManager.Instance.IsOverOrEnding
            || Pile?.Type is not (PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust)
            || Owner?.Creature is not { IsAlive: true, CombatState: { } combat } owner
            || !HasRequiredMajoka(combat, owner))
            return;

        EmaSpecialVictoryPresentation.TryPlayDieMusic(combat);
        _endTurnAutoPlay = true;
        try { await CardCmd.AutoPlay(context, this, null); }
        finally { _endTurnAutoPlay = false; }
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (amount > 0 && power is MajokaPower && power.Owner.Side == CombatSide.Enemy)
            CheckMusicThreshold();
        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && creature.Side == CombatSide.Enemy)
            CheckMusicThreshold();
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this)
            CheckMusicThreshold();
        return Task.CompletedTask;
    }

    private void CheckMusicThreshold()
    {
        if (!CombatManager.Instance.IsOverOrEnding && Pile?.IsCombatPile == true
            && Owner?.Creature is { IsAlive: true, CombatState: { } combat } owner && HasRequiredMajoka(combat, owner))
            EmaSpecialVictoryPresentation.TryPlayDieMusic(combat);
    }

    private bool HasRequiredMajoka(ICombatState combat, Creature owner)
    {
        var enemies = combat.GetOpponentsOf(owner).Where(enemy => enemy.IsAlive).ToList();
        return enemies.Count > 0
            && enemies.All(enemy => enemy.GetPowerAmount<MajokaPower>() >= DynamicVars["MajokaThreshold"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["DoomPower"].UpgradeValueBy(3m);
    }
}
