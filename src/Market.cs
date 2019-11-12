
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

        public float PriceForRange(int start, int end, out Bracket bracket)
        {
            if (start == end)
            {
                bracket = new Bracket();
                return 0;
            }

            float totalPrice = PriceForRange(start, end);
            float vendorPrice = item.VendorPrice();
            int marketQuantity = MarketQuantity();
            if (marketQuantity >= end - start)
            {
                // Must be entirely market.
                bracket = new Bracket();
                bracket.containsMarket = true;
                bracket.totalMin = bracket.marketMin = Entries[0].sellPrice;
                bracket.totalMax = bracket.marketMax = PriceForRange(end - 1, end);
                bracket.marketCount = end - start;
            }
            else if (marketQuantity == 0)
            {
                // Must be entirely vendor.
                bracket = new Bracket();
                bracket.containsVendor = true;
                bracket.totalMin = bracket.totalMax = bracket.vendorPrice = item.VendorPrice();
                bracket.vendorCount = end - start;
            }
            else
            {
                // weird spooky hybrid
                bracket = new Bracket();
                bracket.containsMarket = true;
                bracket.containsVendor = true;
                bracket.totalMin = bracket.marketMin = Entries[0].sellPrice;
                bracket.totalMax = bracket.marketMax = vendorPrice; // not accurate, kind of?
                bracket.marketCount = MarketQuantity();
                bracket.vendorPrice = vendorPrice;
                bracket.vendorCount = end - start - bracket.marketCount;
            }

            return totalPrice;
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

                int purchased = Math.Min(quantityRemaining, entry.stack);
                moneySpent += entry.sellPrice * purchased;
                quantityRemaining -= purchased;

                if (quantityRemaining == 0)
                {
                    break;
                }
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
                return TimeSpan.FromDays(2);
            }

            var firstDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.First().buyRealDate / 1000);
            var lastDate = DateTimeOffset.FromUnixTimeSeconds(cached.history.Last().buyRealDate / 1000);
            var halfSpan = TimeSpan.FromSeconds((firstDate - lastDate).TotalSeconds / 4);

            var medianAge = TimeSpan.FromSeconds(cached.history.Select(history => new Util.Element { value = firstDate.ToUnixTimeSeconds() - history.buyRealDate / 1000, count = history.stack }).Median());

            float minimumSeconds = 60 * 60 * 24;
            float maximumSeconds = 60 * 60 * 24 * 14;

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

    private static TimeSpan GetCacheTime(SaintCoinach.Xiv.Item item, Latency latency)
    {
        if (latency == Latency.Standard)
        {
            return AuctionInvalidationDuration(item);
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
        return GetCacheTime(item, Latency.Standard);
    }

    public static Cherenkov.Session.MarketHistoryResponse History(SaintCoinach.Xiv.Item item, Latency latency, out DateTimeOffset retrievalTime)
    {
        if (!item.IsMarketable())
        {
            retrievalTime = DateTimeOffset.Now;
            return null;
        }

        var apiresult = Api.RetrieveHistory(item, GetCacheTime(item, latency), out retrievalTime);
        
        return apiresult;
    }

    public static Pricing Prices(SaintCoinach.Xiv.Item item, Latency latency)
    {
        if (!item.IsMarketable())
        {
            return new Pricing(null, item);
        }

        return Api.RetrievePricing(item, GetCacheTime(item, latency));
    }

    public static bool IsSelling(SaintCoinach.Xiv.Item item)
    {
        var prices = Prices(item, Latency.Immediate);

        foreach (var price in prices.Entries)
        {
            string poster = price.sellRetainerName;
            if (ZifaConfigDefs.Global.retainers.Contains(poster))
            {
                return true;
            }
        }

        return false;
    }
}
