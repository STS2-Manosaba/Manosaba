using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.Common.Powers;

public sealed class FleetingStrengthPower : ManosabaTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<BrokenCrossbowEvidence>();
    public override LocString Title => new("powers", Id.Entry + ".title");
    public override LocString Description => new("powers", Id.Entry + ".description");
    protected override string SmartDescriptionLocKey => Id.Entry + ".smartDescription";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
}

public sealed class FleetingDexterityPower : ManosabaTemporaryStatPower<DexterityPower>
{
    public override AbstractModel OriginModel => ModelDb.Card<NoahsButterflyDrawing>();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];
}
