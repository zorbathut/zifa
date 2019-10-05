
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public static class Api
{
    public static Cherenkov.Session.MarketHistoryResponse RetrieveHistory(int id, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{id}:history";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            result = Bootstrap.s_Cherenkov.GetMarketHistory(id);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }

        return JsonConvert.DeserializeObject<Cherenkov.Session.MarketHistoryResponse>(result);
    }

    public static Cherenkov.Session.MarketPriceResponse RetrievePricing(int id, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{id}:pricing";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            result = Bootstrap.s_Cherenkov.GetMarketPrices(id);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }

        return JsonConvert.DeserializeObject<Cherenkov.Session.MarketPriceResponse>(result);
    }

    public static Cherenkov.Session.MarketPriceResponse RetrievePricing(int id, TimeSpan invalidation)
    {
        DateTimeOffset _;
        return RetrievePricing(id, invalidation, out _);
    }
}
