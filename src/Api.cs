
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public static class Api
{
    private static Cherenkov.Session s_Cherenkov;
    private static TimeSpan s_CherenkovInitTime = new TimeSpan();

    public static void InitCherenkov()
    {
        if (s_Cherenkov == null)
        {
            var start = DateTimeOffset.Now;

            Cherenkov.Config.InfoHandler = Dbg.Inf;
            Cherenkov.Config.WarningHandler = Dbg.Wrn;
            Cherenkov.Config.ErrorHandler = Dbg.Err;
            Cherenkov.Config.ExceptionHandler = Dbg.Ex;
            
            s_Cherenkov = new Cherenkov.Session();

            s_CherenkovInitTime += DateTimeOffset.Now - start;
        }
    }

    public static TimeSpan InitCherenkovTime()
    {
        return s_CherenkovInitTime;
    }

    public static Cherenkov.Session.MarketHistoryResponse RetrieveHistory(int id, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{id}:history";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            InitCherenkov();

            result = s_Cherenkov.GetMarketHistory(id);
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
            InitCherenkov();

            result = s_Cherenkov.GetMarketPrices(id);
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
