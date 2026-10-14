using BaseLib.Utils;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class CorpsePhotoCard<TPower> : EvidenceCard where TPower : PowerModel
{
    protected CorpsePhotoCard() : base(CardType.Skill, TargetType.Self) { }
    public override int MaxUpgradeLevel => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<TPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CommonActions.Apply<TPower>(context, Owner.Creature, this, 1);
        if (IsUpgraded) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class NoahCorpsePhoto : CorpsePhotoCard<RedButterflyPower>
{
    public override string PortraitPath => "res://Manosaba/images/cards/noah_corpse_photo.png";
}

[Pool(typeof(TokenCardPool))]
public sealed class MiriaCorpsePhoto : CorpsePhotoCard<AlternationPower>
{
    public override string PortraitPath => "res://Manosaba/images/cards/miria_corpse_photo.png";
}

[Pool(typeof(TokenCardPool))]
public sealed class HannaCorpsePhoto : CorpsePhotoCard<LightnessPower>
{
    public override string PortraitPath => "res://Manosaba/images/cards/hanna_corpse_photo.png";
}

[Pool(typeof(TokenCardPool))]
public sealed class AlisaCorpsePhoto : CorpsePhotoCard<WarmFlamePower>
{
    public override string PortraitPath => "res://Manosaba/images/cards/alisa_corpse_photo.png";
}

[Pool(typeof(TokenCardPool))]
public sealed class NanokaCorpsePhoto : CorpsePhotoCard<ResistancePower>
{
    public override string PortraitPath => "res://Manosaba/images/cards/nanoka_corpse_photo.png";
}
