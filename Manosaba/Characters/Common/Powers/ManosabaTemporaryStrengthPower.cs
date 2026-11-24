using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.Common.Powers;

public abstract class ManosabaTemporaryStrengthPower : ManosabaTemporaryStatPower<StrengthPower>
{
    public override LocString Title
    {
        get
        {
            AbstractModel originModel = OriginModel ?? InternallyAppliedPower;
            if (!(originModel is CardModel cardModel))
            {
                if (!(originModel is PotionModel potionModel))
                {
                    if (originModel is RelicModel relicModel)
                    {
                        return relicModel.Title;
                    }
                    if (originModel is OrbModel orbModel)
                    {
                        return orbModel.Title;
                    }

                    throw new InvalidOperationException();
                }

                return potionModel.Title;
            }

            return cardModel.TitleLocString;
        }
    }

    public override LocString Description => new LocString("powers", IsPositive ? "TEMPORARY_STRENGTH_POWER.description" : "TEMPORARY_STRENGTH_DOWN.description");

    protected override string SmartDescriptionLocKey
    {
        get
        {
            if (!IsPositive)
            {
                return "TEMPORARY_STRENGTH_DOWN.smartDescription";
            }

            return "TEMPORARY_STRENGTH_POWER.smartDescription";
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> list = new List<IHoverTip>();
            List<IHoverTip> list2 = list;
            AbstractModel originModel = OriginModel ?? InternallyAppliedPower;
            IEnumerable<IHoverTip> collection;
            if (!(originModel is CardModel card))
            {
                if (!(originModel is PotionModel model))
                {
                    if (!(originModel is RelicModel relic))
                    {
                        if (!(originModel is OrbModel orbModel))
                        {
                            throw new InvalidOperationException();
                        }

                        collection = [orbModel.DumbHoverTip];
                    }
                    else
                    {
                        collection = HoverTipFactory.FromRelic(relic);
                    }
                }
                else
                {
                    collection = [HoverTipFactory.FromPotion(model)];
                }
            }
            else
            {
                collection = [HoverTipFactory.FromCard(card)];
            }

            list2.AddRange(collection);
            list.Add(HoverTipFactory.FromPower<StrengthPower>());
            return list;
        }
    }
}
