using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class StarSplash : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int TotalHitCount = 8;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // Use a single Damage var so the game can correctly preview dynamic damage.
        new DamageVar(1m, ValueProp.Move),
        new RepeatVar(TotalHitCount),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.SwordTechnique];

    public StarSplash()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        // Single damage per hit for now (avoids interactive issues).
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        /*
        // Old per-part VFX sequence (kept for possible future fix).
        // This previously caused issues with some interactive/preview behavior.
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(3)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_big_slash")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitFx("vfx/vfx_flying_slash")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
        */
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
