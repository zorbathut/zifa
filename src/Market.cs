
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
            var span = firstDate - lastDate;

            var cachelen = TimeSpan.FromSeconds(MathUtil.Clamp(span.TotalSeconds / 2, 60 * 60 * 24, 60 * 60 * 24 * 14));

            //Dbg.Inf($"Market request for {Db.Item(id).Name}: Timeout is {cachelen}");
            return Api.Retrieve(key, invalidation: cachelen);
        }

        return Api.Retrieve(key, invalidation: TimeSpan.Zero);
    }
}
