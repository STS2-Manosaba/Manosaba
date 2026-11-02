using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using Manosaba.Characters.TonoHanna.Vfx;
using Manosaba.Characters.TonoHanna.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class GiveHannaABeer : PathCustomCardModel
{
    private const int MajokaThreshold = 50;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];
    protected override bool ShouldGlowGoldInternal => HasHighDiceAvailable;

    public GiveHannaABeer() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        bool usesHighDice = HasHighDiceAvailable;
        int minimumFace = usesHighDice ? 4 : 1;
        int firstFace = RollFace(minimumFace);
        int secondFace = RollFace(minimumFace);
        int thirdFace = RollFace(minimumFace);

        await HannaDiceRollVfx.PlayThreeNumberedAtCombatCenterAsync(firstFace, secondFace, thirdFace, usesHighDice);
        int resultTotal = HannaDiceResultHelper.ApplyNumericFaceBonus(ownerCreature, firstFace)
            + HannaDiceResultHelper.ApplyNumericFaceBonus(ownerCreature, secondFace)
            + HannaDiceResultHelper.ApplyNumericFaceBonus(ownerCreature, thirdFace);
        await DamageCmd.Attack(resultTotal)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);
    }

    private bool HasHighDiceAvailable =>
        Owner?.Creature is { } ownerCreature
        && ownerCreature.GetPowerAmount<MajokaPower>() > (IsUpgraded ? 30 : MajokaThreshold);

    private int RollFace(int minimumFace) => Owner!.RunState.Rng.CombatTargets.NextInt(7 - minimumFace) + minimumFace;
}
