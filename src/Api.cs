
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

    public static Cherenkov.Session.MarketHistoryResponse RetrieveHistory(SaintCoinach.Xiv.Item item, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{item.Key}:history";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            if (Interrupt.ConsumeInterrupt())
            {
                throw new Interrupt();
            }

            InitCherenkov();

            result = s_Cherenkov.GetMarketHistory(item.Key);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }

        return JsonCache.Retrieve<Cherenkov.Session.MarketHistoryResponse>(result, json => JsonConvert.DeserializeObject<Cherenkov.Session.MarketHistoryResponse>(json));
    }

    public static Market.Pricing RetrievePricing(SaintCoinach.Xiv.Item item, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;

        string cacheId = $"{item.Key}:pricing";
        string result = Cache.GetCacheEntry(cacheId, invalidation, out retrievalTime);
        if (result == null)
        {
            if (Interrupt.ConsumeInterrupt())
            {
                throw new Interrupt();
            }

            InitCherenkov();

            result = s_Cherenkov.GetMarketPrices(item.Key);
            Cache.StoreCacheEntry(cacheId, result);
            retrievalTime = DateTimeOffset.Now;
        }
        
        return JsonCache.Retrieve<Market.Pricing>(result, json => new Market.Pricing(JsonConvert.DeserializeObject<Cherenkov.Session.MarketPriceResponse>(json), item));
    }

    public static Market.Pricing RetrievePricing(SaintCoinach.Xiv.Item item, TimeSpan invalidation)
    {
        DateTimeOffset _;
        return RetrievePricing(item, invalidation, out _);
    }
}
