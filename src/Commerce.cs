
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
        Fastsell,
    }

    public static float ValueMarket(int id, bool hq, TransactionType type, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (!itemdb.IsMarketable())
        {
            return float.NaN;
        }

        float lqp;
        float hqp;
        float unfiltered;

        if (type == TransactionType.Longterm)
        {
            var results = Market.History(id, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry item) => new Util.Element { value = item.sellPrice, count = item.stack };

            lqp = results.history.Where(item => item.hq == false).Select(builder).Median();
            hqp = results.history.Where(item => item.hq == true).Select(builder).Median();
            unfiltered = results.history.Select(builder).Median();
        }
        else if (type == TransactionType.Immediate)
        {
            var results = Market.Prices(id, latency);

            lqp = results.entries.Where(item => item.hq == false).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            hqp = results.entries.Where(item => item.hq == true).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            unfiltered = results.entries.Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
        }
        else if (type == TransactionType.Fastsell)
        {
            var resulthistory = Market.History(id, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry item) => new Util.Element { value = item.sellPrice, count = item.stack };

            float hlqp = resulthistory.history.Where(item => item.hq == false).Select(builder).Median();
            float hhqp = resulthistory.history.Where(item => item.hq == true).Select(builder).Median();
            float hunfiltered = resulthistory.history.Select(builder).Median();

            var resultprices = Market.Prices(id, latency);

            float phqp = resultprices.entries.Where(item => item.hq == true).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            float punfiltered = resultprices.entries.Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);

            // TODO MIN OR NAN
            lqp = Math.Min(hlqp, punfiltered);
            hqp = Math.Min(hhqp, punfiltered);
            unfiltered = Math.Min(hunfiltered, punfiltered);
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

    public static float MarketSalesPerDay(int id, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        DateTimeOffset retrievalTime;
        var results = Market.History(id, latency, out retrievalTime);

        if (results.history.Count == 0)
        {
            // welp
            return float.NaN;
        }

        long lastDate = results.history.Last().buyRealDate / 1000;
        long span = retrievalTime.ToUnixTimeSeconds() - lastDate;

        int totalQuantity = results.history.Sum(item => item.stack);

        return (float)totalQuantity / span * 60 * 60 * 24;
    }

    public static float MarketProfitAdjuster(float profit, int id, float acquired, Market.Latency latency)
    {
        if (profit < 0)
        {
            return profit;
        }

        if (!Db.Item(id).IsMarketable())
        {
            return 0;
        }

        float salesPerDay = MarketSalesPerDay(id, latency);

        float daysToSell = acquired / salesPerDay;

        return profit / Math.Max(daysToSell, 1);
    }

    public static float ValueSell(int id, bool hq, Market.Latency latency, out string destination)
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

        float market = ValueMarket(id, hq, Commerce.TransactionType.Fastsell, latency) * 0.95f;
        if (market > 0 && market > bestprice)
        {
            bestprice = market;
            destination = "market";
        }

        return bestprice;
    }

    public static float ValueSell(int id, bool hq, Market.Latency latency)
    {
        string _;
        return ValueSell(id, hq, latency, out _);
    }

    public static float ValueBuy(int id, bool hq, TransactionType type, Market.Latency latency, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return ValueMarket(id, hq, type, latency) * 1.05f;
        }

        float bestprice = ValueMarket(id, hq, type, latency) * 1.05f;
        source = "market";

        var item = Db.Item(id);

        if (CanBuyFromMarket(id))
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            float vendorprice = Math.Min(bestprice, Db.Item(id).Ask);
            if (vendorprice > 0 && (float.IsNaN(bestprice) || vendorprice <= bestprice))
            {
                bestprice = vendorprice;
                source = "vendor";
            }
        }

        return bestprice;
    }

    public static float ValueBuy(int id, bool hq, TransactionType type, Market.Latency latency)
    {
        string _;
        return ValueBuy(id, hq, type, latency, out _);
    }

    
    public static bool CanBuyFromMarket(int id)
    {
        return Marketables().Contains(id);
    }

    private static HashSet<int> marketablesCache;
    public static HashSet<int> Marketables()
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

        return marketablesCache;
    }
}
