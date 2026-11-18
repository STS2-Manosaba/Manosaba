using System.Linq;
using BaseLib.Utils;
using manosaba.Characters.Common;
using Manosaba.Extensions;
using MeruruCharacter = manosaba.Characters.HikamiMeruru.HikamiMeruru;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class FutureOfPotions : PathCustomCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const float wrongPotionChance = 0.05f;
    private const decimal healFractionOfDamageDealt = 0.2m;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m, ValueProp.Move)];

    public FutureOfPotions() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        bool meruruInRun = Owner.RunState.Players.Any(p => p.Character is MeruruCharacter);
        decimal damage = DynamicVars.Damage.BaseValue;

        decimal damageDealt = await MeasureDamageToTarget(
            target,
            () => DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext));

        if (damageDealt > 0m)
        {
            await CreatureCmd.Heal(ownerCreature, damageDealt * healFractionOfDamageDealt);
        }

        if (!meruruInRun && Owner.RunState.Rng.CombatCardGeneration.NextFloat() < wrongPotionChance)
        {
            decimal selfDamage = ownerCreature.CurrentHp;
            if (selfDamage > 0m)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    ownerCreature,
                    selfDamage,
                    ValueProp.Unpowered,
                    ownerCreature,
                    this, null);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }

    private static async Task<decimal> MeasureDamageToTarget(Creature target, Func<Task> dealDamageAsync)
    {
        decimal beforeHp = target.CurrentHp;
        decimal beforeBlock = target.Block;
        await dealDamageAsync();
        return Math.Max(0m, (beforeHp - target.CurrentHp) + (beforeBlock - target.Block));
    }
}
