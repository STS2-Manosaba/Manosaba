using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class CUrOi : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<DefendSakurabaEma>().PortraitPath;
    public override bool GainsBlock => true;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new PowerVar<MajokaPower>(20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<DoomPower>()];
    public CUrOi() : base(1, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if (play.Target is not { } target) return;
        bool extra = target.GetPowerAmount<DoomPower>() >= 20;
        await CommonActions.Apply<MajokaPower>(context, target, this, 20);
        if (extra) await CommonActions.Apply<MajokaPower>(context, target, this, 20);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

[Pool(typeof(TokenCardPool))]
public sealed class ShiDes : PathCustomCardModel
{
    private bool _playedThisTurn;
    public override string PortraitPath => ModelDb.Card<DieSakurabaEma>().PortraitPath;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];
    public ShiDes() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, true) { }
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => !ReferenceEquals(card, this) || !_playedThisTurn;
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner) _playedThisTurn = false;
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        _playedThisTurn = true;
        if (play.Target is { } target)
        {
            await PlayOwnerAttackAnimAsync();
            await CreatureCmd.Damage(context, target, target.GetPowerAmount<MajokaPower>(), ValueProp.Move | ValueProp.Unpowered | ValueProp.Unblockable, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(TokenCardPool))]
public sealed class SentKy : EmaTrialCard
{
    public override string PortraitPath => ModelDb.Card<PresentEvidence>().PortraitPath;
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromPower<DeathSentencePower>()];
    public SentKy() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
        if (play.Target is { IsAlive: true } target && !target.HasPower<DeathSentencePower>())
            await CommonActions.Apply<DeathSentencePower>(context, target, this, 1);
    }
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class DArime : EmaTrialCard
{
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Trial, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new TrialDamageVar(10), new DynamicVar("Progress", 20)];
    public DArime() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true) { }
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => !ReferenceEquals(card, this) || Progress(this) >= 20;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { } target) return;
        await EmaTrialPresentation.Play(this);
        while (target.IsAlive && !MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsOverOrEnding)
        {
            CardModel? drawn = await CardPileCmd.Draw(context, Owner);
            if (drawn == null || !drawn.Keywords.Contains(ManosabaKeywords.Trial)) break;
            await CardCmd.Exhaust(context, drawn);
            await CreatureCmd.Damage(context, target, DynamicVars.Damage.BaseValue, TrialMoveValueProp, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Choice cards are displayed by the synchronized native choose-a-card screen.
[Pool(typeof(TokenCardPool))]
public sealed class AcceptExhaustMagic : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<MagicToKillTheWitch>().PortraitPath;
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public AcceptExhaustMagic() : base(0, CardType.Skill, CardRarity.Token, TargetType.None, false) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}

[Pool(typeof(TokenCardPool))]
public sealed class DeclineExhaustMagic : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<MagicToKillTheWitch>().PortraitPath;
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public DeclineExhaustMagic() : base(0, CardType.Skill, CardRarity.Token, TargetType.None, false) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
