
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public enum TransactionType
    {
        Longterm,
        Immediate,
        Fastsell,
    }

    public static float ValueMarket(SaintCoinach.Xiv.Item item, bool hq, TransactionType type, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        if (!item.IsMarketable())
        {
            return float.NaN;
        }

        float lqp;
        float hqp;
        float unfiltered;

        if (type == TransactionType.Longterm)
        {
            var results = Market.History(item, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry entry) => new Util.Element { value = entry.sellPrice, count = entry.stack };

            lqp = results.history.Where(entry => entry.hq == false).Select(builder).Median();
            hqp = results.history.Where(entry => entry.hq == true).Select(builder).Median();
            unfiltered = results.history.Select(builder).Median();
        }
        else if (type == TransactionType.Immediate)
        {
            var results = Market.Prices(item, latency);

            lqp = results.Entries.Where(entry => entry.hq == false).Select(entry => (float)entry.sellPrice).FirstOrDefault(float.NaN);
            hqp = results.Entries.Where(entry => entry.hq == true).Select(entry => (float)entry.sellPrice).FirstOrDefault(float.NaN);
            unfiltered = results.Entries.Select(entry => (float)entry.sellPrice).FirstOrDefault(float.NaN);
        }
        else if (type == TransactionType.Fastsell)
        {
            var resulthistory = Market.History(item, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry entry) => new Util.Element { value = entry.sellPrice, count = entry.stack };

            float hlqp = resulthistory.history.Where(entry => entry.hq == false).Select(builder).Median();
            float hhqp = resulthistory.history.Where(entry => entry.hq == true).Select(builder).Median();
            float hunfiltered = resulthistory.history.Select(builder).Median();

            var resultprices = Market.Prices(item, latency);

            float phqp = resultprices.Entries.Where(entry => entry.hq == true).Select(entry => (float)entry.sellPrice).FirstOrDefault(float.NaN);
            float punfiltered = resultprices.Entries.Select(entry => (float)entry.sellPrice).FirstOrDefault(float.NaN);

            lqp = Util.MinWithoutNan(hlqp, punfiltered);
            hqp = Util.MinWithoutNan(hhqp, punfiltered);
            unfiltered = Util.MinWithoutNan(hunfiltered, punfiltered);
        }
        else
        {
            Dbg.Err("uhhhh wut");
            return float.NaN;
        }

        // If we have no data for either set, give it the other's data
        if (float.IsNaN(lqp))
        {
            lqp = hqp;
        }
        if (float.IsNaN(hqp))
        {
            hqp = lqp;
        }

        // If LQP is more expensive than HQP, return the unfiltered results
        if (lqp > hqp)
        {
            return unfiltered;
        }

        return hq ? hqp : lqp;
    }

    public static float MarketSalesPerDay(SaintCoinach.Xiv.Item item, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        if (item.IsUntradable)
        {
            return float.NaN;
        }

        DateTimeOffset retrievalTime;
        var results = Market.History(item, latency, out retrievalTime);

        if (results.history.Count == 0)
        {
            // welp
            return float.NaN;
        }

        long lastDate = results.history.Last().buyRealDate / 1000;
        long span = retrievalTime.ToUnixTimeSeconds() - lastDate;

        int totalQuantity = results.history.Sum(entry => entry.stack);

        return (float)totalQuantity / span * 60 * 60 * 24;
    }

    public static float MarketExpectedStackSale(SaintCoinach.Xiv.Item item, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        if (item.IsUntradable)
        {
            return float.NaN;
        }

        var results = Market.History(item, latency);

        return Math.Min(Math.Min(results.history.Select(entry => entry.stack).Percentile(0.9f) * 2, item.StackSize), 99);
    }

    public static float MarketProfitAdjuster(float profit, SaintCoinach.Xiv.Item item, float acquired, Market.Latency latency)
    {
        if (profit < 0)
        {
            return profit;
        }

        if (!item.IsMarketable())
        {
            return 0;
        }

        float salesPerSlotDay = Math.Min(MarketSalesPerDay(item, latency), MarketExpectedStackSale(item, latency));

        float daysToSell = acquired / salesPerSlotDay;

        return profit / Math.Max(daysToSell, 1);
    }

    public static float ValueSell(SaintCoinach.Xiv.Item item, bool hq, Market.Latency latency, out string destination)
    {
        // This is always available (I think)
        float bestprice = item.Bid;
        if (hq)
        {
            // This seems to be the right equation
            bestprice = (float)Math.Ceiling(bestprice * 1.1f);
        }
        destination = "vendor";

        float market = ValueMarket(item, hq, Commerce.TransactionType.Fastsell, latency) * 0.95f;
        if (market > 0 && market > bestprice)
        {
            bestprice = market;
            destination = "market";
        }

        return bestprice;
    }

    public static float ValueSell(SaintCoinach.Xiv.Item item, bool hq, Market.Latency latency)
    {
        string _;
        return ValueSell(item, hq, latency, out _);
    }

    public static float ValueBuy(SaintCoinach.Xiv.Item item, bool hq, TransactionType type, Market.Latency latency, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return ValueMarket(item, hq, type, latency) * 1.05f;
        }

        float bestprice = ValueMarket(item, hq, type, latency) * 1.05f;
        source = "market";

        if (CanBuyFromVendor(item))
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            float vendorprice = item.Ask;
            if (vendorprice > 0 && (float.IsNaN(bestprice) || vendorprice <= bestprice))
            {
                bestprice = vendorprice;
                source = "vendor";
            }
        }

        return bestprice;
    }

    public static float ValueBuy(SaintCoinach.Xiv.Item item, bool hq, TransactionType type, Market.Latency latency)
    {
        string _;
        return ValueBuy(item, hq, type, latency, out _);
    }

    
    public static bool CanBuyFromVendor(SaintCoinach.Xiv.Item item)
    {
        return Marketables().Contains(item);
    }

    public static float VendorPrice(this SaintCoinach.Xiv.Item item)
    {
        return CanBuyFromVendor(item) ? item.Ask : float.NaN;
    }

    private static Dictionary<SaintCoinach.Xiv.Item, SaintCoinach.Xiv.ENpc[]> itemToNPC;
    private static Dictionary<SaintCoinach.Xiv.ENpc, int> npcToItemCount;
    private static string[] blacklistedMerchants = new string[]
    {
        
    };

    private static void ConsumeData(SaintCoinach.Xiv.GilShop shop, Dictionary<SaintCoinach.Xiv.Item, List<SaintCoinach.Xiv.ENpc>> marketablesTemp, SaintCoinach.Xiv.ENpc npc)
    {
        foreach (var element in shop.Items)
        {
            var item = element.Item;

            if (!marketablesTemp.ContainsKey(item))
            {
                marketablesTemp[item] = new List<SaintCoinach.Xiv.ENpc>();
            }

            marketablesTemp[item].Add(npc);
        }
    }

    private static void ConsumeData(SaintCoinach.Xiv.XivRow row, Dictionary<SaintCoinach.Xiv.Item, List<SaintCoinach.Xiv.ENpc>> marketablesTemp, SaintCoinach.Xiv.ENpc npc)
    {
        for (int i = 0; i < 10; ++i)    // 10 is hardcoded in the db format
        {
            var data = row[SaintCoinach.Xiv.XivRow.BuildColumnName("Shop", i)] as SaintCoinach.Xiv.GilShop;

            if (data != null)
            {
                ConsumeData(data, marketablesTemp, npc);
            }
        }
    }

    public static void GenerateMarketables()
    {
        if (itemToNPC == null)
        {
            Dbg.Inf("Generating marketables cache . . .");
            var start = DateTimeOffset.Now;

            var gsi = new List<SaintCoinach.Xiv.GilShopItem>();
            foreach (var shopItem in Db.GetSheet2<SaintCoinach.Xiv.GilShopItem>())
            {
                gsi.Add(shopItem);
            }

            var housingEmployables = new HashSet<int>();
            foreach (var row in Db.Realm.GameData.GetSheet("HousingEmploymentNpcList"))
            {
                var sourceRow = row.SourceRow as SaintCoinach.Ex.Variant2.RelationalDataRow;
                foreach (var subrow in sourceRow.SubRows)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        var employee = subrow[SaintCoinach.Xiv.XivRow.BuildColumnName("ENpcBase", i)] as SaintCoinach.Xiv.ENpcBase;
                        if (employee != null)
                        {
                            housingEmployables.Add(employee.Key);
                        }
                    }
                }
            }

            // TAKE 2
            var marketablesTemp = new Dictionary<SaintCoinach.Xiv.Item, List<SaintCoinach.Xiv.ENpc>>();
            foreach (var npc in Db.Realm.GameData.ENpcs.ProgressBar())
            {
                if (housingEmployables.Contains(npc.Key))
                {
                    continue;
                }

                if (blacklistedMerchants.Any(kst => npc.Singular.ToString().Contains(kst)))
                {
                    continue;
                }

                foreach (var data in npc.Base.AssignedData)
                {
                    if (data is SaintCoinach.Xiv.GilShop)
                    {
                        ConsumeData(data as SaintCoinach.Xiv.GilShop, marketablesTemp, npc);
                    }
                    else if (data.GetType() == typeof(SaintCoinach.Xiv.XivRow))
                    {
                        var xivrow = data as SaintCoinach.Xiv.XivRow;

                        if (xivrow.Sheet.Header.Name == "TopicSelect")
                        {
                            ConsumeData(data as SaintCoinach.Xiv.XivRow, marketablesTemp, npc);
                        }
                    }
                }
            }

            itemToNPC = new Dictionary<SaintCoinach.Xiv.Item, SaintCoinach.Xiv.ENpc[]>();
            npcToItemCount = new Dictionary<SaintCoinach.Xiv.ENpc, int>();
            foreach (var kv in marketablesTemp)
            {
                itemToNPC[kv.Key] = kv.Value.Distinct().ToArray();

                foreach (var npc in itemToNPC[kv.Key])
                {
                    if (!npcToItemCount.ContainsKey(npc))
                    {
                        npcToItemCount[npc] = 0;
                    }

                    npcToItemCount[npc] = npcToItemCount[npc] + 1;
                }
            }

            Api.AddInitTime(DateTimeOffset.Now - start);
            Dbg.Inf("Done");
        }
    }

    public static IEnumerable<SaintCoinach.Xiv.Item> Marketables()
    {
        GenerateMarketables();

        return itemToNPC.Select(x => x.Key);
    }

    public static IEnumerable<SaintCoinach.Xiv.ENpc> SellersForItem(SaintCoinach.Xiv.Item item)
    {
        return itemToNPC.ContainsKey(item) ? itemToNPC[item] : Enumerable.Empty<SaintCoinach.Xiv.ENpc>();
    }

    public static int ItemCountInShop(SaintCoinach.Xiv.ENpc seller)
    {
        return npcToItemCount.TryGetValue(seller);
    }
}
