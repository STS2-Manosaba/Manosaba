using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class Linear : PathCustomCardModel
{
    private const int BaseDamage = 8;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, ManosabaKeywords.SwordTechnique];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
    ];

    public Linear()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, shouldShowInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        if (CombatState is not { } combatState)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        CardModel[] options = ModelDb.AllCards
            .Where(card => card.CanBeGeneratedInCombat && card.CanonicalKeywords.Contains(ManosabaKeywords.SwordTechnique))
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToArray();
        if (options.Length == 0)
            return;

        CardModel? canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(options);
        if (canonical != null)
            await CardPileCmd.AddGeneratedCardToCombat(combatState.CreateCard(canonical, Owner), PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
