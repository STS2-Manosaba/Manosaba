using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.TonoHanna.Helpers;
using Manosaba.Characters.TonoHanna.Vfx;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class AngelsDice : PathCustomCardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockMultiplier", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public AngelsDice() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyPlayer, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        MegaCrit.Sts2.Core.Entities.Creatures.Creature target = cardPlay.Target ?? ownerCreature;
        int face = Owner!.RunState.Rng.CombatTargets.NextInt(6) + 1;
        await HannaDiceRollVfx.PlayAngelsThrowAtCombatCenterAsync(face);
        int result = HannaDiceResultHelper.ApplyNumericFaceBonus(ownerCreature, face);
        await CreatureCmd.GainBlock(target, result * DynamicVars["BlockMultiplier"].BaseValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockMultiplier"].UpgradeValueBy(1m);
    }
}
