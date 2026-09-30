using BaseLib.Abstracts;
using Godot;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Relics;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma;

public class SakurabaEma : PlaceholderCharacterModel
{
    public const string CharacterId = "sakuraba_ema";

    public static readonly Color Color = new("E8A0BF");
    public override Color MapDrawingColor => Color;
    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeSakurabaEma>(),
        ModelDb.Card<StrikeSakurabaEma>(),
        ModelDb.Card<StrikeSakurabaEma>(),
        ModelDb.Card<DefendSakurabaEma>(),
        ModelDb.Card<DefendSakurabaEma>(),
        ModelDb.Card<DefendSakurabaEma>(),
        ModelDb.Card<DefendSakurabaEma>(),
        ModelDb.Card<TraumaSakurabaEma>(),
        ModelDb.Card<SearchSakurabaEma>(),
        ModelDb.Card<SearchSakurabaEma>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<LegIronsSakurabaEma>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<SakurabaEmaCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<SakurabaEmaRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SakurabaEmaPotionPool>();

    public override string CustomIconTexturePath => (CharacterId + "_map.png").CharacterImgPath(CharacterId);
    public override string CustomCharacterSelectIconPath => (CharacterId + "_char_select.png").CharacterImgPath(CharacterId);
    public override string CustomMapMarkerPath => (CharacterId + "_map.png").CharacterImgPath(CharacterId);
    public override string CustomCharacterSelectBg => (CharacterId + "_bg.tscn").CharacterScenePath(CharacterId);
    public override string CustomVisualPath => (CharacterId + ".tscn").CharacterScenePath(CharacterId);
    public override string CustomIconPath => (CharacterId + "_icon.tscn").CharacterScenePath(CharacterId);
    public override string CustomMerchantAnimPath => (CharacterId + "_merchant.tscn").CharacterScenePath(CharacterId);
    public override string CustomArmPointingTexturePath => (CharacterId + "_arm_pointing.png").CharacterImgPath(CharacterId);
    public override string CustomArmScissorsTexturePath => (CharacterId + "_arm_scissors.png").CharacterImgPath(CharacterId);
    public override string CustomEnergyCounterPath => (CharacterId + "_energy_counter.tscn").CharacterScenePath(CharacterId);

    public override string CharacterSelectSfx => ManosabaCharacterSfx.CharacterSelectEvent(CharacterId);
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
}
