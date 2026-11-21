using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using Manosaba.Compatibility;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class StarburstStream : PathCustomCardModel
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int HitCount = 16;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(HitCount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SecondSwordPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.SwordTechnique, ManosabaKeywords.TwoSwords];

    protected override bool IsPlayable
    {
        get
        {
            if (Owner?.Creature == null)
            {
                return false;
            }

            decimal secondSwordAmount = Owner.Creature.GetPowerAmount<SecondSwordPower>();
            decimal tempSecondSwordAmount = Owner.Creature.GetPowerAmount<TreeBranchSecondSwordPower>();
            if (secondSwordAmount <= 0m && tempSecondSwordAmount <= 0m)
            {
                return false;
            }

            return true;
        }
    }

    public StarburstStream()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        // Exhaust the hand (excluding this card, which is no longer in hand when played).
        List<CardModel> cardsToExhaust = PileType.Hand.GetPile(Owner).Cards.ToList();
        int exhaustedCount = cardsToExhaust.Count;
        foreach (CardModel card in cardsToExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        DynamicVars.Damage.BaseValue = 3m + exhaustedCount;

        decimal beforeAllHp = target.CurrentHp;
        decimal beforeAllBlock = target.Block;

        // Single damage per hit for now (avoids interactive issues).
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(HitCount)
            .WithHitFx("vfx/vfx_big_slash")
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        decimal afterAllHp = target.CurrentHp;
        decimal afterAllBlock = target.Block;
        decimal totalDamageDealt = Math.Max(0m, (beforeAllHp - afterAllHp) + (beforeAllBlock - afterAllBlock));

        /*
        // Old per-part VFX sequence (kept for possible future fix).
        // This previously caused issues with some interactive/preview behavior.
        decimal totalDamageDealt = 0m;
        for (int i = 0; i < 14; i++)
        {
            decimal beforeHp = target.CurrentHp;
            decimal beforeBlock = target.Block;

            string vfx = (i % 2 == 0) ? "vfx/vfx_big_slash" : "vfx/vfx_flying_slash";
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitFx(vfx)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext);

            decimal afterHp = target.CurrentHp;
            decimal afterBlock = target.Block;
            totalDamageDealt += Math.Max(0m, (beforeHp - afterHp) + (beforeBlock - afterBlock));
        }

        {
            decimal beforeHp = target.CurrentHp;
            decimal beforeBlock = target.Block;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(2)
                .WithHitFx("vfx/vfx_dramatic_stab")
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext);

            decimal afterHp = target.CurrentHp;
            decimal afterBlock = target.Block;
            totalDamageDealt += Math.Max(0m, (beforeHp - afterHp) + (beforeBlock - afterBlock));
        }
        */

        if (IsUpgraded && Owner?.Creature is { } dealer && target.IsAlive)
        {
            decimal extraDamage = decimal.Ceiling(totalDamageDealt * 0.3m);
            if (extraDamage > 0m)
            {
                // Small pause + big-hit VFX before the 17th (upgrade) hit.
                await Cmd.CustomScaledWait(0.5f, 0.5f);
                GameApiCompat.PlayVfxOnCreature(target, "vfx/vfx_big_slash");
                await CreatureCmd.Damage(choiceContext, target, extraDamage, ValueProp.Unpowered, dealer, this, null);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
