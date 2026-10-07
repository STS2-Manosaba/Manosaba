using BaseLib.Utils;
using Manosaba.Combat;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using FieldEffect = Manosaba.Characters.Common.Powers.TestFieldPower;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class TestFieldPower : PathCustomCardModel
{
    // Intentionally available in solo too: the field affects every side in any combat.
    public TestFieldPower() : base(1, CardType.Power, CardRarity.Token, TargetType.None, false) { }

    // Hidden prototype; still obtainable explicitly with the developer console.
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public override string PortraitPath => ModelDb.Card<Inflame>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FieldDamage", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FieldEffect>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayOwnerCastAnimAsync();
        FieldPowerState.Apply<FieldEffect>(Owner.Creature.CombatState
            ?? throw new InvalidOperationException("Field power requires an active combat."),
            (int)DynamicVars["FieldDamage"].BaseValue);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
