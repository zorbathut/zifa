
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public enum TransactionType
    {
        Longterm,
        Immediate,
    }

    public static float ValueMarket(int id, bool hq, TransactionType type)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        float lqp;
        float hqp;
        float unfiltered;

        if (type == TransactionType.Longterm)
        {
            var results = Market.History(id);

            var history = results["History"].OfType<JObject>();

            Util.Element builder(JObject item) => new Util.Element { value = item["PricePerUnit"].Value<int>(), count = item["Quantity"].Value<int>() };

            lqp = history.Where(item => item["IsHQ"].Value<bool>() == false).Select(builder).Median();
            hqp = history.Where(item => item["IsHQ"].Value<bool>() == true).Select(builder).Median();
            unfiltered = history.Select(builder).Median();
        }
        else if (type == TransactionType.Immediate)
        {
            var results = Market.Prices(id);

            var prices = results["Prices"].OfType<JObject>();

            lqp = prices.Where(item => item["IsHQ"].Value<bool>() == false).Select(item => item["PricePerUnit"].Value<float>()).FirstOrDefault(float.NaN);
            hqp = prices.Where(item => item["IsHQ"].Value<bool>() == true).Select(item => item["PricePerUnit"].Value<float>()).FirstOrDefault(float.NaN);
            unfiltered = prices.Select(item => item["PricePerUnit"].Value<float>()).FirstOrDefault(float.NaN);
        }
        else
        {
            Dbg.Err("uhhhh wut");
            return float.NaN;
        }

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
            return unfiltered;
        }

        return hq ? hqp : lqp;
    }

    public static float MarketSalesPerDay(int id)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        var results = Market.History(id);

        var history = results["History"].OfType<JObject>();
        if (!history.Any())
        {
            // welp
            return float.NaN;
        }

        // due to caching, we may not have up-to-date results, so we pretend we polled on the first item to get useful stats
        long firstDate = history.First()["PurchaseDate"].Value<long>();
        long lastDate = history.Last()["PurchaseDate"].Value<long>();
        if (firstDate == lastDate)
        {
            // this isn't ideal, but okay
            firstDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        long span = firstDate - lastDate;

        // skip the first so we're not biasing very positive
        int totalQuantity = history.Skip(1).Sum(item => item["Quantity"].Value<int>());

        return (float)totalQuantity / span * 60 * 60 * 24;
    }

    public static float MarketProfitAdjuster(float profit, int id, float acquired)
    {
        if (profit < 0)
        {
            return profit;
        }

        float salesPerDay = MarketSalesPerDay(id);

        float daysToSell = acquired / salesPerDay;

        return profit / Math.Max(daysToSell, 1);
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

        float market = ValueMarket(id, hq, Commerce.TransactionType.Longterm) * 0.95f;
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

    public static float ValueBuy(int id, bool hq, TransactionType type, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return ValueMarket(id, hq, type) * 1.05f;
        }

        float bestprice = ValueMarket(id, hq, type) * 1.05f;
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

    public static float ValueBuy(int id, bool hq, TransactionType type)
    {
        string _;
        return ValueBuy(id, hq, type, out _);
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
