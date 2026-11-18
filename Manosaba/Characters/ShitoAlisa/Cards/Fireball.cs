using System;
using System.Collections.Generic;
using BaseLib.Utils;
using manosaba.Characters.ShitoAlisa;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.CardPools;

using Manosaba.Utils;

namespace Manosaba.Characters.ShitoAlisa.Cards;

/// <summary>è¡ç??ç??ã??ç«çE?ï¼?æ¶?è??ã?E? æ?å?·å®³E?å?ç´?æ??å?¨é«?æ?µäººã?E/summary>
[Pool(typeof(TokenCardPool))]
public sealed class Fireball : ShitoAlisaCardModel
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const bool shouldShowInCardLibrary = true;

    /// <summary>Only from explicit card effects (e.g. RestrictionRelease); exclude from transforms / discovery pools.</summary>
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        WithCombust(0, new DamageVar(5m, ValueProp.Move));

    public override TargetType TargetType => IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

    public Fireball() : base(energyCost, type, rarity, TargetType.AnyEnemy, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? state = CombatState;
        if (state == null)
            return;

        if (IsUpgraded)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponentsCompat(state)
                .Execute(choiceContext);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

    }
}
