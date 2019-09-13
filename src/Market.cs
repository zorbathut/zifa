
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
            var history = cached["History"].OfType<JObject>();

            if (history.Count() == 0)
            {
                return TimeSpan.FromDays(2);
            }

            var firstDate = DateTimeOffset.FromUnixTimeSeconds(history.First()["PurchaseDate"].Value<long>());
            var lastDate = DateTimeOffset.FromUnixTimeSeconds(history.Last()["PurchaseDate"].Value<long>());
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 4);

            var medianAge = TimeSpan.FromSeconds(history.Select(item => new Util.Element { value = firstDate.ToUnixTimeSeconds() - item["PurchaseDate"].Value<long>(), count = item["Quantity"].Value<int>() }).Median());

            invalidationTime = TimeSpan.FromSeconds(MathUtil.Clamp(Math.Min(halfSpan.TotalSeconds, medianAge.TotalSeconds), 60 * 60 * 24, 60 * 60 * 24 * 14));
        }
        else
        {
            invalidationTime = TimeSpan.Zero;
        }

        return invalidationTime;
    }

    public static JObject History(int id, Latency latency)
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

    public static JObject History(int id, Latency latency, out DateTimeOffset retrievalTime)
    {
        if (!Db.Item(id).IsMarketable())
        {
            retrievalTime = DateTimeOffset.Now;
            return null;
        }

        var apiresult = Api.Retrieve(
                $"/market/item/{id}",
                GetCacheTime(id, latency),
                out retrievalTime,
                new Dictionary<string, string>() { { "servers", "Midgardsormr" } }
            )["Midgardsormr"];

        if (apiresult.Contains("Updated"))
        {
            retrievalTime = DateTimeOffset.FromUnixTimeSeconds(apiresult["Updated"].Value<long>());
        }
        else
        {
            retrievalTime = DateTimeOffset.Now;
        }
        
        return apiresult as JObject;
    }

    public static JObject Prices(int id, Latency latency)
    {
        if (!Db.Item(id).IsMarketable())
        {
            return null;
        }

        return Api.Retrieve(
                $"/market/item/{id}",
                GetCacheTime(id, latency),
                new Dictionary<string, string>() { { "servers", "Midgardsormr" } }
            )["Midgardsormr"] as JObject;
    }

    public static bool IsSelling(int id, string[] people)
    {
        var prices = Prices(id, Latency.Immediate);

        foreach (var price in prices["Prices"].OfType<JObject>())
        {
            string poster = price["RetainerName"].Value<string>();
            if (people.Contains(poster))
            {
                return true;
            }
        }

        return false;
    }
}
