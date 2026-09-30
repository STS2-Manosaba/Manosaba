using BaseLib.Utils;
using Manosaba.Characters.Common.Commands;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class DieSakurabaEma : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new PowerVar<DoomPower>(10m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<NarehatePower>(),
    ];

    public DieSakurabaEma() : base(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = CombatState.GetOpponentsOf(ownerCreature)
            .Where(enemy => enemy.IsHittable)
            .ToList();

        foreach (Creature enemy in enemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }

        foreach (Creature enemy in enemies.Where(enemy => enemy.IsAlive))
        {
            await CommonActions.Apply<DoomPower>(choiceContext, enemy, this, DynamicVars["DoomPower"].BaseValue);
        }

        if (enemies.Count > 0 && enemies.Where(enemy => enemy.IsAlive).All(enemy => enemy.GetPowerAmount<NarehatePower>() > 0m))
        {
            await ManosabaCombatCmd.ForceWinWithoutDeathOrEscape((ICombatState)CombatState);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["DoomPower"].UpgradeValueBy(3m);
    }
}
