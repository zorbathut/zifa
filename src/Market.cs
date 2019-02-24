
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
    }

    private static TimeSpan AuctionInvalidationDuration(int id)
    {
        string key = $"/market/midgardsormr/items/{id}/history";
        var cached = Api.Retrieve(key, invalidation: TimeSpan.MaxValue);

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
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 2);

            var medianAge = TimeSpan.FromSeconds(history.Select(item => new Util.Element { value = firstDate.ToUnixTimeSeconds() - item["PurchaseDate"].Value<long>(), count = item["Quantity"].Value<int>() }).Median());

            invalidationTime = TimeSpan.FromSeconds(MathUtil.Clamp(Math.Min(halfSpan.TotalSeconds, medianAge.TotalSeconds), 60 * 60 * 24, 60 * 60 * 24 * 14));
        }
        else
        {
            invalidationTime = TimeSpan.Zero;
        }

        // poke the history with the new invalidation time, just to properly cache the data we're using to generate this data

        Api.Retrieve(key, invalidation: invalidationTime);

        return invalidationTime;
    }

    public static JObject History(int id, Latency latency)
    {
        return Api.Retrieve($"/market/midgardsormr/items/{id}/history", invalidation: latency == Latency.Standard ? AuctionInvalidationDuration(id) : TimeSpan.FromHours(1));
    }

    public static JObject Prices(int id, Latency latency)
    {
        return Api.Retrieve($"/market/midgardsormr/items/{id}", invalidation: latency == Latency.Standard ? AuctionInvalidationDuration(id) : TimeSpan.FromHours(1));
    }
}
