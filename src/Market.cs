
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Market
{
    public static JObject History(int id)
    {
        string key = $"/market/midgardsormr/items/{id}/history";
        var cached = Api.Retrieve(key, invalidation: TimeSpan.MaxValue);

        if (cached != null)
        {
            var history = cached["History"].OfType<JObject>();

            var firstDate = DateTimeOffset.FromUnixTimeSeconds(history.First()["PurchaseDate"].Value<long>());
            var lastDate = DateTimeOffset.FromUnixTimeSeconds(history.Last()["PurchaseDate"].Value<long>());
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 2);

            var medianAge = TimeSpan.FromSeconds(history.Select(item => new Util.Element { value = firstDate.ToUnixTimeSeconds() - item["PurchaseDate"].Value<long>(), count = item["Quantity"].Value<int>() }).Median());

            var invalidation = TimeSpan.FromSeconds(MathUtil.Clamp(Math.Min(halfSpan.TotalSeconds, medianAge.TotalSeconds), 60 * 60 * 24, 60 * 60 * 24 * 14));

            //Dbg.Inf($"Market request for {Db.Item(id).Name}: Halfspan is {halfSpan}, median is {medianAge}, final is {invalidation}");
            return Api.Retrieve(key, invalidation: invalidation);
        }

        return Api.Retrieve(key, invalidation: TimeSpan.Zero);
    }
}
