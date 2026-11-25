using manosaba.Characters.SakurabaEma.Combat;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class EncyclopediaCard(int energyCost, CardType type, CardRarity rarity, TargetType target,
    bool shouldShowInCardLibrary) : PathCustomCardModel(energyCost, type, rarity, target, shouldShowInCardLibrary)
{
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("PlaysPerTurn", WitchEncyclopediaState.PlayLimit(this));
    }
}
