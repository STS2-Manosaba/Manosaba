using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class PsychicPhotograph : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(100m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MajokaPower>(),
        HoverTipFactory.FromKeyword(ManosabaKeywords.Unique),
    ];

    public PsychicPhotograph() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        decimal? legRingMajokaMultiplier = null;
        LegRingMajokaPower? legRingMajokaPower = Owner.Creature.GetPower<LegRingMajokaPower>();
        if (legRingMajokaPower != null)
        {
            legRingMajokaMultiplier = legRingMajokaPower.Amount;
            await PowerCmd.Remove(legRingMajokaPower);
        }

        try
        {
            decimal majokaToApply = DynamicVars["MajokaPower"].BaseValue - Owner.Creature.GetPowerAmount<MajokaPower>();
            if (majokaToApply > 0m)
            {
                await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, majokaToApply);
            }
        }
        finally
        {
            if (legRingMajokaMultiplier.HasValue)
            {
                await PowerCmd.Apply<LegRingMajokaPower>(
                    choiceContext,
                    Owner.Creature,
                    legRingMajokaMultiplier.Value,
                    Owner.Creature,
                    this);
            }
        }

        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
