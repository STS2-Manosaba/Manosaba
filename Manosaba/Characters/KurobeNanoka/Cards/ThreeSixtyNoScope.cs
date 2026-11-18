using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Commands;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.KurobeNanoka.Helpers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class ThreeSixtyNoScope : GunBase
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const decimal InstaKillChancePercent = 0.1m;
    private const string InstaKillVideoPath = "res://Manosaba/video/GET REKT 2 Ft by Fanta.ogv";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.GunShot];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new CardsVar(1),
        new DynamicVar("BulletCost", 1m)
    ];

    public ThreeSixtyNoScope() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!TrySpendBulletsOnPlay())
        {
            return;
        }

        Creature? target = cardPlay.Target;
        if (target == null)
        {
            return;
        }
        // 0.1% = 1 in 1000.
        int roll = Owner.RunState.Rng.Niche.NextInt(1000);
        if (roll == 0 && target.IsAlive)
        {
            NanokaHelper.PlayAWPSfx();
            for(int i = 0; i < 5; i++)
            {
                VfxCmd.PlayOnCreature(target, "vfx/vfx_attack_lightning");
                await Cmd.Wait(0.3f);
            }
            ManosabaVideoCmd.PlayFullscreenOneShot(InstaKillVideoPath, chromaKeyGreen: true);
            await Cmd.Wait(14f);
            await CreatureCmd.Kill(target);
            return;
        }
        await ExecuteGunAttack(choiceContext, target, DynamicVars.Damage.BaseValue, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }

    private void PlayEvokeSfx()
    {
        string EvokeSfx = "event:/sfx/characters/defect/defect_lightning_evoke";
        if (EvokeSfx != "")
        {
            SfxCmd.Play(EvokeSfx);
        }
    }
}
