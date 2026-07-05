using BaseLib.Utils;
using Manosaba.Characters.SawatariCoco.Equipment;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SawatariCoco.Cards;

public static class EquipmentPieceTokenRegistry
{
    private static readonly IReadOnlyDictionary<Type, Type> EquipmentCardToToken = new Dictionary<Type, Type>
    {
        [typeof(PunkCatHeadwear)] = typeof(PunkCatHeadwearToken),
        [typeof(PunkCatTop)] = typeof(PunkCatTopToken),
        [typeof(PunkCatSkirt)] = typeof(PunkCatSkirtToken),
        [typeof(PunkCatShoes)] = typeof(PunkCatShoesToken),
        [typeof(CybercatHeadwear)] = typeof(CybercatHeadwearToken),
        [typeof(CybercatTop)] = typeof(CybercatTopToken),
        [typeof(CybercatSkirt)] = typeof(CybercatSkirtToken),
        [typeof(CybercatShoes)] = typeof(CybercatShoesToken),
        [typeof(MysteriousCatHeadwear)] = typeof(MysteriousCatHeadwearToken),
        [typeof(MysteriousCatTop)] = typeof(MysteriousCatTopToken),
        [typeof(MysteriousCatPants)] = typeof(MysteriousCatPantsToken),
        [typeof(MysteriousCatShoes)] = typeof(MysteriousCatShoesToken),
        [typeof(CutieCatsHeadwear)] = typeof(CutieCatsHeadwearToken),
        [typeof(CutieCatsTop)] = typeof(CutieCatsTopToken),
        [typeof(CutieCatsPants)] = typeof(CutieCatsPantsToken),
        [typeof(CutieCatsShoes)] = typeof(CutieCatsShoesToken),
    };

    private static readonly IReadOnlyDictionary<(EquipmentSeries Series, EquipmentSlot Slot), Type> SeriesSlotToToken =
        new Dictionary<(EquipmentSeries, EquipmentSlot), Type>
        {
            [(EquipmentSeries.PunkCat, EquipmentSlot.Headwear)] = typeof(PunkCatHeadwearToken),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Top)] = typeof(PunkCatTopToken),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Skirt)] = typeof(PunkCatSkirtToken),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Shoes)] = typeof(PunkCatShoesToken),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Headwear)] = typeof(CybercatHeadwearToken),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Top)] = typeof(CybercatTopToken),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Skirt)] = typeof(CybercatSkirtToken),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Shoes)] = typeof(CybercatShoesToken),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Headwear)] = typeof(MysteriousCatHeadwearToken),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Top)] = typeof(MysteriousCatTopToken),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Skirt)] = typeof(MysteriousCatPantsToken),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Shoes)] = typeof(MysteriousCatShoesToken),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Headwear)] = typeof(CutieCatsHeadwearToken),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Top)] = typeof(CutieCatsTopToken),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Skirt)] = typeof(CutieCatsPantsToken),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Shoes)] = typeof(CutieCatsShoesToken),
        };

    private static readonly IReadOnlyDictionary<(EquipmentSeries Series, EquipmentSlot Slot), Type> SeriesSlotToEquipmentCard =
        new Dictionary<(EquipmentSeries, EquipmentSlot), Type>
        {
            [(EquipmentSeries.PunkCat, EquipmentSlot.Headwear)] = typeof(PunkCatHeadwear),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Top)] = typeof(PunkCatTop),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Skirt)] = typeof(PunkCatSkirt),
            [(EquipmentSeries.PunkCat, EquipmentSlot.Shoes)] = typeof(PunkCatShoes),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Headwear)] = typeof(CybercatHeadwear),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Top)] = typeof(CybercatTop),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Skirt)] = typeof(CybercatSkirt),
            [(EquipmentSeries.Cybercat, EquipmentSlot.Shoes)] = typeof(CybercatShoes),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Headwear)] = typeof(MysteriousCatHeadwear),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Top)] = typeof(MysteriousCatTop),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Skirt)] = typeof(MysteriousCatPants),
            [(EquipmentSeries.MysteriousCat, EquipmentSlot.Shoes)] = typeof(MysteriousCatShoes),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Headwear)] = typeof(CutieCatsHeadwear),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Top)] = typeof(CutieCatsTop),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Skirt)] = typeof(CutieCatsPants),
            [(EquipmentSeries.CutieCats, EquipmentSlot.Shoes)] = typeof(CutieCatsShoes),
        };

    public static bool TryGetTokenType(Type equipmentCardType, out Type tokenType)
        => EquipmentCardToToken.TryGetValue(equipmentCardType, out tokenType!);

    public static bool TryGetTokenType(EquipmentSeries series, EquipmentSlot slot, out Type tokenType)
        => SeriesSlotToToken.TryGetValue((series, slot), out tokenType!);

    public static bool TryGetEquipmentCardType(EquipmentSeries series, EquipmentSlot slot, out Type equipmentCardType)
        => SeriesSlotToEquipmentCard.TryGetValue((series, slot), out equipmentCardType!);
}

public abstract class EquipmentPieceTokenModel : PathCustomCardModel
{
    protected abstract int EquipmentScore { get; }

    protected EquipmentPieceTokenModel()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false)
    {
    }

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("EquipmentScore", EquipmentScore)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}

[Pool(typeof(TokenCardPool))]
public sealed class PunkCatHeadwearToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class PunkCatTopToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 5000;
}

[Pool(typeof(TokenCardPool))]
public sealed class PunkCatSkirtToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class PunkCatShoesToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 1000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CybercatHeadwearToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 5000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CybercatTopToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CybercatSkirtToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 1000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CybercatShoesToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class MysteriousCatHeadwearToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 1000;
}

[Pool(typeof(TokenCardPool))]
public sealed class MysteriousCatTopToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class MysteriousCatPantsToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 5000;
}

[Pool(typeof(TokenCardPool))]
public sealed class MysteriousCatShoesToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CutieCatsHeadwearToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CutieCatsTopToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 1000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CutieCatsPantsToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 2000;
}

[Pool(typeof(TokenCardPool))]
public sealed class CutieCatsShoesToken : EquipmentPieceTokenModel
{
    protected override int EquipmentScore => 5000;
}
