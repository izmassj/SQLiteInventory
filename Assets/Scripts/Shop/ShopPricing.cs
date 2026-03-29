using System;

public static class ShopPricing
{
    public const int DefaultBuyPrice = 100;

    public static int GetBuyPrice(Item item)
    {
        if (item == null)
            return 0;

        if (item.buyPrice > 0)
            return item.buyPrice;

        return DefaultBuyPrice;
    }

    public static int GetSellPrice(Item item)
    {
        if (item == null)
            return 0;

        if (item.sellPrice > 0)
            return item.sellPrice;

        return Math.Max(1, GetBuyPrice(item) / 2);
    }
}
