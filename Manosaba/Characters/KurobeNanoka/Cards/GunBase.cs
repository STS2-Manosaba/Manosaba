using manosaba.Characters.KurobeNanoka.Relics;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.KurobeNanoka.Helpers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Manosaba.Characters.KurobeNanoka.Cards;

public abstract class GunBase : PathCustomCardModel
{
    public bool SpentBulletsThisPlay { get; private set; }

    protected GunBase(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    private bool HasEnoughBulletsForPlay()
    {
        if (!DynamicVars.TryGetValue("BulletCost", out var bulletCostVar))
        {
            return true;
        }

        MagicalGun? magicalGun = Owner.GetRelic<MagicalGun>();
        bool hasEnoughBullets = magicalGun?.HasEnoughBullets(bulletCostVar.IntValue) == true;
        if (!hasEnoughBullets)
        {
            Console.WriteLine($"[GunBase] Bullet check failed. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name} cost={bulletCostVar.IntValue} currentBullets={magicalGun?.DisplayAmount ?? -1}");
        }

        return hasEnoughBullets;
    }

    protected bool TrySpendBulletsOnPlay()
    {
        SpentBulletsThisPlay = false;

        if (!DynamicVars.TryGetValue("BulletCost", out var bulletCostVar))
        {
            SpentBulletsThisPlay = true;
            return true;
        }

        MagicalGun? magicalGun = Owner.GetRelic<MagicalGun>();
        if (magicalGun == null)
        {
            Console.WriteLine($"[GunBase] Bullet spend failed: missing MagicalGun. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name}");
            return false;
        }

        int bulletCost = bulletCostVar.IntValue;
        Console.WriteLine($"[GunBase] Attempting bullet spend. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name} cost={bulletCost} bulletsBefore={magicalGun.DisplayAmount}");
        if (!magicalGun.ConsumeBullets(bulletCost))
        {
            Console.WriteLine($"[GunBase] Bullet spend rejected. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name} cost={bulletCost} bulletsAfter={magicalGun.DisplayAmount}");
            return false;
        }

        SpentBulletsThisPlay = true;
        Console.WriteLine($"[GunBase] Bullet spend succeeded. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name} cost={bulletCost} bulletsAfter={magicalGun.DisplayAmount}");
        return true;
    }

    protected Task ExecuteGunAttack(PlayerChoiceContext choiceContext, Creature target, decimal damage, CardPlay cardPlay)
    {
        Console.WriteLine($"[GunBase] Executing gun attack. card={Id} ownerNetId={Owner?.NetId} ownerName={Owner?.Creature?.Name} target={target?.Name} damage={damage} spentBulletsThisPlay={SpentBulletsThisPlay}");
        return DamageCmd.Attack(damage)
            .WithAttackerFx(vfx: null, sfx: null)
            .WithHitFx(
                vfx: "vfx/vfx_attack_blunt",
                sfx: NanokaHelper.GUN_SHOT_SFX)
            .FromCard(this, cardPlay)
            .Targeting(target!)
            .Execute(choiceContext);
    }

    protected override bool IsPlayable => base.IsPlayable && HasEnoughBulletsForPlay();
}
