
using Newtonsoft.Json;
using System;

public static class Api
{
    private static Cherenkov.Session s_Cherenkov;
    private static TimeSpan s_InitTime = new TimeSpan();

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

            AddInitTime(DateTimeOffset.Now - start);
        }
    }

    public static TimeSpan InitTime()
    {
        return s_InitTime;
    }

    public static void AddInitTime(TimeSpan time)
    {
        s_InitTime += time;
    }

    public static Cherenkov.Session.MarketHistoryResponse RetrieveHistory(int id, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{id}:history";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            if (Interrupt.ConsumeInterrupt())
            {
                throw new Interrupt();
            }

            InitCherenkov();

            result = s_Cherenkov.GetMarketHistory(id);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }

        return JsonCache.Retrieve<Cherenkov.Session.MarketHistoryResponse>(result, json => JsonConvert.DeserializeObject<Cherenkov.Session.MarketHistoryResponse>(json));
    }

    public static Market.Pricing RetrievePricing(int id, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{id}:pricing";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            if (Interrupt.ConsumeInterrupt())
            {
                throw new Interrupt();
            }

            InitCherenkov();

            result = s_Cherenkov.GetMarketPrices(id);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }
        
        return JsonCache.Retrieve<Market.Pricing>(result, json => new Market.Pricing(JsonConvert.DeserializeObject<Cherenkov.Session.MarketPriceResponse>(json)));
    }

    public static Market.Pricing RetrievePricing(int id, TimeSpan invalidation)
    {
        DateTimeOffset _;
        return RetrievePricing(id, invalidation, out _);
    }
}
