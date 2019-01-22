
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public static float ValueMarket(int id, bool hq)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        var results = Market.History(id);

        var history = results["History"].OfType<JObject>();

        Util.Element builder(JObject item) => new Util.Element { price = item["PricePerUnit"].Value<int>(), count = item["Quantity"].Value<int>() };

        float lqp = history.Where(item => item["IsHQ"].Value<bool>() == false).Select(builder).Median();
        float hqp = history.Where(item => item["IsHQ"].Value<bool>() == true).Select(builder).Median();

        // If we have no data for either set, give it the other's data
        if (float.IsNaN(lqp))
        {
            lqp = hqp;
        }
        if (float.IsNaN(hqp))
        {
            hqp = lqp;
        }

        // If LQP is more expensive than HQP, return the unfiltered results
        if (lqp > hqp)
        {
            return history.Select(builder).Median();
        }

        return hq ? hqp : lqp;
    }

    public static float MarketProfitDelayQuotient(int id)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        var results = Market.History(id);

        var history = results["History"].OfType<JObject>();

        long lastDate = history.Last()["PurchaseDate"].Value<long>();
        long span = DateTimeOffset.Now.ToUnixTimeSeconds() - lastDate;

        int totalQuantity = history.Sum(item => item["Quantity"].Value<int>());

        return Math.Max(1f / Math.Min(99, itemdb.StackSize), (float)span / totalQuantity / 86400);
    }

    public static float ValueSell(int id, bool hq, out string destination)
    {
        var item = Db.Item(id);

        // This is always available (I think)
        float bestprice = item.Bid;
        if (hq)
        {
            // This seems to be the right equation
            bestprice = (float)Math.Ceiling(bestprice * 1.1f);
        }
        destination = "vendor";

        float market = ValueMarket(id, hq) * 0.95f;
        if (market > 0 && market > bestprice)
        {
            bestprice = market;
            destination = "market";
        }

        return bestprice;
    }

    public static float ValueSell(int id, bool hq)
    {
        string _;
        return ValueSell(id, hq, out _);
    }

    public static float ValueBuy(int id, bool hq, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return ValueMarket(id, hq) * 1.05f;
        }

        float bestprice = ValueMarket(id, hq) * 1.05f;
        source = "market";

        var item = Db.Item(id);

        if (CanBuyFromMarket(id))
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            float vendorprice = Math.Min(bestprice, Db.Item(id).Ask);
            if (vendorprice > 0 && vendorprice <= bestprice)
            {
                bestprice = vendorprice;
                source = "vendor";
            }
        }

        return bestprice;
    }

    public static float ValueBuy(int id, bool hq)
    {
        string _;
        return ValueBuy(id, hq, out _);
    }

    private static HashSet<int> marketablesCache;
    public static bool CanBuyFromMarket(int id)
    {
        if (marketablesCache == null)
        {
            marketablesCache = new HashSet<int>();

            foreach (var shopItem in Db.GetSheet2<SaintCoinach.Xiv.GilShopItem>())
            {
                marketablesCache.Add(shopItem.Item.Key);
            }

            Dbg.Inf("Generated marketables cache");
        }

        return marketablesCache.Contains(id);
    }
}
