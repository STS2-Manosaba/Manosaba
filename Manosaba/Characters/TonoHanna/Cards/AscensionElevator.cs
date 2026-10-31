using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Extensions;
using Manosaba.Characters.TonoHanna.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class AscensionElevator : PathCustomCardModel
{
    private const int ElevatorFace = 5;
    private const int EelFace = 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BaseMultiplier", 2m),
        new DynamicVar("ElevatorMultiplier", 3m),
        new DynamicVar("DoubleElevatorDamage", 36m),
        new DynamicVar("DoubleEelDamage", 10m),
    ];

    public AscensionElevator() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        // Use the synchronized combat RNG stream so each peer resolves the same two die faces.
        int firstFace = Owner.RunState.Rng.CombatTargets.NextInt(6) + 1;
        int secondFace = Owner.RunState.Rng.CombatTargets.NextInt(6) + 1;
        await HannaDiceRollVfx.PlayTwoAtCombatCenterAsync(firstFace, secondFace);

        if (firstFace == EelFace && secondFace == EelFace)
        {
            await DamageCmd.Attack(DynamicVars["DoubleEelDamage"].BaseValue)
                .FromCard(this)
                .Targeting(ownerCreature)
                .Execute(choiceContext);
            return;
        }

        if (firstFace == ElevatorFace && secondFace == ElevatorFace)
        {
            await DamageCmd.Attack(DynamicVars["DoubleElevatorDamage"].BaseValue)
                .FromCard(this)
                .Targeting(target)
                .Execute(choiceContext);
            return;
        }

        int faceTotal = GetNumericFaceValue(firstFace) + GetNumericFaceValue(secondFace);
        decimal damage = faceTotal * DynamicVars["BaseMultiplier"].BaseValue;
        if (firstFace == ElevatorFace || secondFace == ElevatorFace)
        {
            damage *= DynamicVars["ElevatorMultiplier"].BaseValue;
        }

        if (damage > 0m)
        {
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BaseMultiplier"].UpgradeValueBy(1m);
    }

    private static int GetNumericFaceValue(int face) => face is >= 1 and <= 4 ? face : 0;
}
