using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Characters.TonoHanna.Vfx;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class RiskDice : PathCustomCardModel
{
    private const int DieFaces = 20;

    private sealed class RiskDiceCalculatedVar : CalculatedVar
    {
        private readonly Func<RiskDicePower?, decimal> _getValue;

        public RiskDiceCalculatedVar(string name, Func<RiskDicePower?, decimal> getValue)
            : base(name)
        {
            _getValue = getValue;
            WithMultiplier((card, _) => _getValue(card.Owner?.Creature?.GetPower<RiskDicePower>()));
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;
            _ = target;
            _ = runGlobalHooks;
            PreviewValue = _getValue(card.Owner?.Creature?.GetPower<RiskDicePower>());
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar("SelfDamageMultiplier", 3m),
        new RiskDiceCalculatedVar("GoodChance", static power => power?.GoodChance ?? RiskDicePower.InitialGoodChance),
        new RiskDiceCalculatedVar("UsesSinceMisfortune", static power => power?.UsesSinceMisfortune ?? 0m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<RiskDicePower>()];

    public RiskDice() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        RiskDicePower? power = ownerCreature.GetPower<RiskDicePower>();
        if (power == null)
        {
            await CommonActions.Apply<RiskDicePower>(choiceContext, ownerCreature, this, 1m);
            power = ownerCreature.GetPower<RiskDicePower>();
        }

        if (power == null)
        {
            return;
        }

        power.RecordUse();
        int goodFaces = (int)(power.GoodChance * DieFaces / 100m);
        int face = Owner.RunState.Rng.CombatTargets.NextInt(DieFaces) + 1;
        bool greatLuck = face <= goodFaces;
        await HannaRiskDiceRollVfx.PlayAtCombatCenterAsync(face, goodFaces);
        if (greatLuck)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(target)
                .Execute(choiceContext);

            power.RecordSuccess();
            await CardPileCmd.Add(this, PileType.Hand);
            return;
        }

        decimal selfDamage = power.UsesSinceMisfortune * DynamicVars["SelfDamageMultiplier"].BaseValue;
        await CreatureCmd.Damage(choiceContext, ownerCreature, selfDamage, ValueProp.Unpowered, ownerCreature, this);
        power.ResetAfterMisfortune();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["SelfDamageMultiplier"].UpgradeValueBy(1m);
    }
}
