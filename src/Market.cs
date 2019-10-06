
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Market
{
    public enum Latency
    {
        Standard,
        Immediate,
        CacheOnly,
    }

    private static TimeSpan AuctionInvalidationDuration(int id)
    {
        var cached = History(id, Latency.CacheOnly, out DateTimeOffset retrievalTime);

        TimeSpan invalidationTime;
        if (cached != null)
        {
            if (cached.history == null)
            {
                Dbg.Err("WHAT WHAT");
                return TimeSpan.Zero;
            }

            if (cached.history.Count == 0)
            {
                return TimeSpan.FromDays(2);
            }

            var firstDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.First().buyRealDate / 1000);
            var lastDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.Last().buyRealDate / 1000);
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 4);

            var medianAge = TimeSpan.FromSeconds(cached.history.Select(item => new Util.Element { value = firstDate.ToUnixTimeSeconds() - item.buyRealDate / 1000, count = item.stack }).Median());

            invalidationTime = TimeSpan.FromSeconds(MathUtil.Clamp(Math.Min(halfSpan.TotalSeconds, medianAge.TotalSeconds), 60 * 60 * 24, 60 * 60 * 24 * 14));
        }
        else
        {
            invalidationTime = TimeSpan.Zero;
        }

        return invalidationTime;
    }

    public static Cherenkov.Session.MarketHistoryResponse History(int id, Latency latency)
    {
        DateTimeOffset _;
        return History(id, latency, out _);
    }

    private static TimeSpan GetCacheTime(int id, Latency latency)
    {
        if (latency == Latency.Standard)
        {
            return AuctionInvalidationDuration(id);
        }
        else if (latency == Latency.Immediate)
        {
            return TimeSpan.FromHours(1);
        }
        else if (latency == Latency.CacheOnly)
        {
            return TimeSpan.MaxValue;
        }

        Dbg.Err("what's going on with a bad latency?");
        return GetCacheTime(id, Latency.Standard);
    }

    public static Cherenkov.Session.MarketHistoryResponse History(int id, Latency latency, out DateTimeOffset retrievalTime)
    {
        if (!Db.Item(id).IsMarketable())
        {
            retrievalTime = DateTimeOffset.Now;
            return null;
        }

        var apiresult = Api.RetrieveHistory(id, GetCacheTime(id, latency), out retrievalTime);
        
        return apiresult;
    }

    public static Cherenkov.Session.MarketPriceResponse Prices(int id, Latency latency)
    {
        if (!Db.Item(id).IsMarketable())
        {
            return null;
        }

        return Api.RetrievePricing(id, GetCacheTime(id, latency));
    }

    public static bool IsSelling(int id, string[] people)
    {
        var prices = Prices(id, Latency.Immediate);

        foreach (var price in prices.entries)
        {
            string poster = price.sellRetainerName;
            if (people.Contains(poster))
            {
                return true;
            }
        }

        return false;
    }
}
