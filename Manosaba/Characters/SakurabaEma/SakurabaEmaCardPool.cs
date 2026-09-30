using BaseLib.Abstracts;
using Godot;
using manosaba.Characters.Common;
using manosaba.Extensions;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma;

public class SakurabaEmaCardPool : CustomCardPoolModel
{
    public override string Title => SakurabaEma.CharacterId;
    public const string CharacterId = SakurabaEma.CharacterId;

    public override string BigEnergyIconPath => (CharacterId + "_energy.png").CharacterImgPath(CharacterId);
    public override string TextEnergyIconPath => (CharacterId + "_energy_text.png").CharacterImgPath(CharacterId);

    private static readonly (float H, float S, float V) CardBackTint =
        CardPoolTintFromCharacterColor.ToCardBackHsv(SakurabaEma.Color);

    public override float H => CardBackTint.H;
    public override float S => CardBackTint.S;
    public override float V => CardBackTint.V;
    public override Color DeckEntryCardColor => SakurabaEma.Color;
    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards() => ModelDb.CardPool<CommonCardPool>().AllCards.ToArray();
}
