using BaseLib.Utils;
using Manosaba.Characters.Common.Commands;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Manosaba.Characters.Common.Cards;

/// <summary>Developer previews only; deliberately no upgrade or gameplay effect.</summary>
public abstract class RefuteCutInTestCard : PathCustomCardModel
{
    protected RefuteCutInTestCard() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false) { }

    protected abstract RefuteCutInVariant Variant { get; }
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await RefuteCutInCmd.Play(Variant);
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInEma1 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Ema1;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInEma2 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Ema2;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInHiro1 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Hiro1;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInHiro2 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Hiro2;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInHiro3 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Hiro3;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInWitchHiro1 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.WitchHiro1;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInWitchHiro2 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.WitchHiro2;
}

[Pool(typeof(TokenCardPool))]
public sealed class TestRefuteCutInWitchHiro3 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.WitchHiro3;
}
