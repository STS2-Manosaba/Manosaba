using BaseLib.Utils;
using BaseLib.Extensions;
using manosaba.Characters.NatsumeAnan.Cards;
using Manosaba.Characters.HasumiLeia.Cards;
using Manosaba.Characters.HikamiMeruru.Cards;
using Manosaba.Characters.HoshoMago.Cards;
using Manosaba.Characters.JogasakiNoahCard.Cards;
using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Characters.SaekiMiria.Cards;
using manosaba.Characters.SawatariCoco.Cards;
using manosaba.Characters.SakurabaEma.Cards;
using Manosaba.Characters.ShitoAlisa.Cards;
using Manosaba.Characters.TachibanaSherry.Cards;
using Manosaba.Characters.TonoHanna.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
namespace Manosaba.Characters.Common.Powers
{
    public class MajokaPower : PathCustomPowerModel
    {
        // Damage scaling follows a saturating curve (Hill function) that approaches 4x total damage.
        private const decimal MaxTotalDamageMultiplier = 4m;
        // Tuned anchors:
        // - ~2.75x at 200 stacks
        // - ~3.5x at 400 stacks
        private const decimal CurveHalfSaturationStacks = 166.6m;
        private const decimal CurveExponent = 1.8365m;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => false;

        public static Dictionary<string, Type> mahouCardsMap = new()
        {
            { "nikaido_hiro",  typeof(DeathLoop)},
            { "hikami_meruru", typeof(GrandHeal)},
            { "jogasaki_noah", typeof(LiquidManipulation)},
            { "tachibana_sherry", typeof(SuperPunch)},
            { "saeki_miria", typeof(MemorySharing)},
            { "tono_hanna", typeof(SkyIsland)},
            { "hosho_mago", typeof(Echoes)},
            { "hasumi_leia", typeof(GazeGuiding)},
            { "kurobe_nanoka", typeof(HallucinationMagic)},
            { "shito_alisa", typeof(FireJudgementCourt)},
            { "natsume_anan", typeof(Brainwash)},
            { "sawatari_coco", typeof(Clairvoyance)},
            { "sakuraba_ema", typeof(MagicToKillTheWitch)},
        };

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            if (base.Owner != dealer)
            {
                return 1m;
            }

            if (props.HasFlag(ValueProp.Unpowered))
            {
                return 1m;
            }

            return GetMajokaDamageMultiplier(base.Amount);
        }

        public static decimal GetMajokaDamageMultiplier(decimal stacks)
        {
            if (stacks <= 0m)
            {
                return 1m;
            }

            // ratio = s^p / (s^p + k^p), where:
            // - s = stacks
            // - p = CurveExponent
            // - k = CurveHalfSaturationStacks
            // This yields a smooth monotonic curve that asymptotically approaches the cap.
            double sPow = Math.Pow((double)stacks, (double)CurveExponent);
            double kPow = Math.Pow((double)CurveHalfSaturationStacks, (double)CurveExponent);
            double ratio = sPow / (sPow + kPow);
            decimal maxBonus = MaxTotalDamageMultiplier - 1m;
            return 1m + maxBonus * (decimal)ratio;
        }

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            if (power is MajokaPower)
            {
                if (base.Owner.Player?.Creature is not { } ownerCreature)
                {
                    return;
                }

                int toApplyMI = base.Amount / 100 - base.Owner.GetPowerAmount<MurderousImpulsePower>();
                await CommonActions.Apply<MurderousImpulsePower>(choiceContext, ownerCreature, null, toApplyMI);
                await CheckAndGiveMahouCards();
            }
        }

        private async Task CheckAndGiveMahouCards()
        {
            if (base.Amount >= 100 && Owner.Player != null && Owner.CombatState != null)
            {
                string characterId = (Owner.Player.Character.Id.ToString() ?? string.Empty).RemovePrefix().ToLowerInvariant();
                await TransformSakurabaEmaTraumaIfNeeded(characterId);

                if (!mahouCardsMap.TryGetValue(characterId, out Type? targetType) || targetType == null)
                    return;
                if (Owner.Player.Deck.Cards.Count(c => c.GetType() == targetType) < 1)
                {
                    CardModel cardType = ModelDb.GetById<CardModel>(ModelDb.GetId(targetType));
                    if (cardType == null)
                        return;

                    CardModel cardToDeck = Owner.Player.RunState.CreateCard(cardType, Owner.Player);
                    CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardToDeck, PileType.Deck), 1.2f, CardPreviewStyle.GridLayout);
                    CardModel cardToHand = Owner.CombatState.CreateCard(cardType, Owner.Player);
                    await CardPileCmd.AddGeneratedCardToCombat(cardToHand, PileType.Hand, Owner.Player);
                }
            }
        }

        private async Task TransformSakurabaEmaTraumaIfNeeded(string characterId)
        {
            if (characterId != "sakuraba_ema" || Owner.Player == null)
            {
                return;
            }

            CardModel? trauma = Owner.Player.Deck.Cards.FirstOrDefault(c => c is TraumaSakurabaEma);
            if (trauma == null)
            {
                return;
            }

            bool wasUpgraded = trauma.IsUpgraded;
            CardPileAddResult? result = await CardCmd.TransformTo<TraumaTrueSakurabaEma>(trauma);
            if (wasUpgraded && result is { success: true, cardAdded: { } transformed })
            {
                CardCmd.Upgrade(transformed);
            }
        }

    }
}
