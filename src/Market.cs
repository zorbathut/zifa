
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Market
{
    public class Pricing
    {
        private Cherenkov.Session.MarketPriceResponse pricing;
        private SaintCoinach.Xiv.Item item;
        private readonly static List<Cherenkov.Session.MarketPriceResponse.Entry> NullEntries = new List<Cherenkov.Session.MarketPriceResponse.Entry>();
    
        public struct Bracket
        {
            public bool containsMarket;
            public bool containsVendor;

            public float marketMin;
            public float marketMax;
            public int marketCount;

            public float vendorPrice;
            public int vendorCount;

            public float totalMin;
            public float totalMax;

            public float incrementalPrice;
            public float fullStackPrice;
        }

        public List<Cherenkov.Session.MarketPriceResponse.Entry> Entries
        {
            get
            {
                return pricing?.entries ?? NullEntries;
            }
        }

        public SaintCoinach.Xiv.Item Item
        {
            get
            {
                return item;
            }
        }

        public Pricing(Cherenkov.Session.MarketPriceResponse pricing, SaintCoinach.Xiv.Item item)
        {
            this.pricing = pricing;
            this.item = item;
        }

        public int MarketQuantity()
        {
            float vendorPrice = item.VendorPrice();

            int quantity = 0;
            foreach (var entry in Entries)
            {
                if (entry.sellPrice >= vendorPrice)
                {
                    break;
                }

                quantity += entry.stack;
            }

            return quantity;
        }

        public float PriceForRange(int start, int end)
        {
            return PriceForQuantity(end) - PriceForQuantity(start);
        }

        public float PriceForRangeFullStack(int start, int end)
        {
            return PriceForQuantityFullStack(end) - PriceForQuantityFullStack(start);
        }

        public float PriceForRange(int start, int end, out Bracket bracket)
        {
            bracket = new Bracket();

            if (start == end)
            {
                return 0;
            }

            bracket.incrementalPrice = PriceForRange(start, end);
            bracket.fullStackPrice = PriceForRangeFullStack(start, end);

            float vendorPrice = item.VendorPrice();
            int marketQuantity = MarketQuantity();
            if (marketQuantity >= end - start)
            {
                // Must be entirely market.
                bracket.containsMarket = true;
                bracket.totalMin = bracket.marketMin = Entries[0].sellPrice;
                bracket.totalMax = bracket.marketMax = PriceForRange(end - 1, end);
                bracket.marketCount = end - start;
            }
            else if (marketQuantity == 0)
            {
                // Must be entirely vendor.
                bracket.containsVendor = true;
                bracket.totalMin = bracket.totalMax = bracket.vendorPrice = item.VendorPrice();
                bracket.vendorCount = end - start;
            }
            else
            {
                // weird spooky hybrid
                bracket.containsMarket = true;
                bracket.containsVendor = true;
                bracket.totalMin = bracket.marketMin = Entries[0].sellPrice;
                bracket.totalMax = bracket.marketMax = vendorPrice; // not accurate, kind of?
                bracket.marketCount = MarketQuantity();
                bracket.vendorPrice = vendorPrice;
                bracket.vendorCount = end - start - bracket.marketCount;
            }

            return bracket.incrementalPrice;
        }

        public Bracket BracketForRange(int start, int end)
        {
            var result = new Bracket();
            PriceForRange(start, end, out result);
            return result;
        }

        public float PriceForQuantity(int quantity)
        {
            float vendorPrice = item.VendorPrice();

            int quantityRemaining = quantity;
            float moneySpent = 0;

            foreach (var entry in Entries)
            {
                if (entry.sellPrice >= vendorPrice)
                {
                    break;
                }

                if (quantityRemaining <= 0)
                {
                    break;
                }

                int purchased = Math.Min(quantityRemaining, entry.stack);
                moneySpent += entry.sellPrice * purchased;
                quantityRemaining -= purchased;
            }

            // it's OK if this NaN's us
            if (quantityRemaining > 0)
            {
                moneySpent += quantityRemaining * vendorPrice;
            }

            return moneySpent;
        }

        public float PriceForQuantityFullStack(int quantity)
        {
            float vendorPrice = item.VendorPrice();

            int quantityRemaining = quantity;
            float moneySpent = 0;

            foreach (var entry in Entries)
            {
                if (entry.sellPrice >= vendorPrice)
                {
                    break;
                }

                if (quantityRemaining <= 0)
                {
                    break;
                }

                moneySpent += entry.sellPrice * entry.stack;
                quantityRemaining -= entry.stack;
            }

            // it's OK if this NaN's us
            if (quantityRemaining > 0)
            {
                moneySpent += quantityRemaining * vendorPrice;
            }

            return moneySpent;
        }
    }

    public enum Latency
    {
        Standard,
        Immediate,
        CacheOnly,
    }

    private static TimeSpan AuctionInvalidationDuration(SaintCoinach.Xiv.Item item)
    {
        var cached = History(item, Latency.CacheOnly, out DateTimeOffset retrievalTime);

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
                return TimeSpan.FromDays(1);
            }

            var firstDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.First().buyRealDate / 1000);
            var lastDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.Last().buyRealDate / 1000);
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 4);

            var medianAge = TimeSpan.FromSeconds(cached.history.Select(history => new Util.Element { value = firstDate.ToUnixTimeSeconds() - history.buyRealDate / 1000, count = history.stack }).Median());

            float minimumSeconds = (float)TimeSpan.FromDays(1).TotalSeconds;
            float maximumSeconds = (float)TimeSpan.FromDays(7).TotalSeconds;

            invalidationTime = TimeSpan.FromSeconds(MathUtil.Clamp(Math.Min(halfSpan.TotalSeconds, medianAge.TotalSeconds), minimumSeconds, maximumSeconds));
        }
        else
        {
            invalidationTime = TimeSpan.Zero;
        }

        return invalidationTime;
    }

    public static Cherenkov.Session.MarketHistoryResponse History(SaintCoinach.Xiv.Item item, Latency latency)
    {
        DateTimeOffset _;
        return History(item, latency, out _);
    }

    public static TimeSpan GetCacheRefreshTime(SaintCoinach.Xiv.Item item, Latency latency)
    {
        if (latency == Latency.Standard)
        {
            return AuctionInvalidationDuration(item);
        }
        else if (latency == Latency.Immediate)
        {
            var recacheDuration = DateTimeOffset.Now - Cache.GetImmediateRecachePoint();
            if (recacheDuration.TotalHours < 1)
            {
                return recacheDuration;
            }
            else
            {
                return TimeSpan.FromHours(1);
            }
        }
        else if (latency == Latency.CacheOnly)
        {
            return TimeSpan.MaxValue;
        }

        Dbg.Err("what's going on with a bad latency?");
        return GetCacheRefreshTime(item, Latency.Standard);
    }

    public static Cherenkov.Session.MarketHistoryResponse History(SaintCoinach.Xiv.Item item, Latency latency, out DateTimeOffset retrievalTime)
    {
        if (!item.IsMarketable())
        {
            retrievalTime = DateTimeOffset.Now;
            return null;
        }

        var apiresult = Api.RetrieveHistory(item, GetCacheRefreshTime(item, latency), out retrievalTime);
        
        return apiresult;
    }

    public static Pricing Prices(SaintCoinach.Xiv.Item item, Latency latency, out DateTimeOffset retrievalTime)
    {
        if (!item.IsMarketable())
        {
            retrievalTime = DateTimeOffset.Now;
            return new Pricing(null, item);
        }

        return Api.RetrievePricing(item, GetCacheRefreshTime(item, latency), out retrievalTime);
    }

    public static Pricing Prices(SaintCoinach.Xiv.Item item, Latency latency)
    {
        DateTimeOffset _;
        return Prices(item, latency, out _);
    }

    public static bool IsSelling(SaintCoinach.Xiv.Item item)
    {
        var prices = Prices(item, Latency.Immediate);

        foreach (var price in prices.Entries)
        {
            string poster = price.sellRetainerName;
            if (ZifaConfigDefs.Global.retainers.Any(r => r.name == poster))
            {
                return true;
            }
        }

        return false;
    }
}
