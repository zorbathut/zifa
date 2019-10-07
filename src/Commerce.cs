
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

    public static float ValueMarket(int id, bool hq, TransactionType type, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (!itemdb.IsMarketable())
        {
            return float.NaN;
        }

        float lqp;
        float hqp;
        float unfiltered;

        if (type == TransactionType.Longterm)
        {
            var results = Market.History(id, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry item) => new Util.Element { value = item.sellPrice, count = item.stack };

            lqp = results.history.Where(item => item.hq == false).Select(builder).Median();
            hqp = results.history.Where(item => item.hq == true).Select(builder).Median();
            unfiltered = results.history.Select(builder).Median();
        }
        else if (type == TransactionType.Immediate)
        {
            var results = Market.Prices(id, latency);

            lqp = results.entries.Where(item => item.hq == false).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            hqp = results.entries.Where(item => item.hq == true).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            unfiltered = results.entries.Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
        }
        else if (type == TransactionType.Fastsell)
        {
            var resulthistory = Market.History(id, latency);

            Util.Element builder(Cherenkov.Session.MarketHistoryResponse.Entry item) => new Util.Element { value = item.sellPrice, count = item.stack };

            float hlqp = resulthistory.history.Where(item => item.hq == false).Select(builder).Median();
            float hhqp = resulthistory.history.Where(item => item.hq == true).Select(builder).Median();
            float hunfiltered = resulthistory.history.Select(builder).Median();

            var resultprices = Market.Prices(id, latency);

            float phqp = resultprices.entries.Where(item => item.hq == true).Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);
            float punfiltered = resultprices.entries.Select(item => (float)item.sellPrice).FirstOrDefault(float.NaN);

            // TODO MIN OR NAN
            lqp = Math.Min(hlqp, punfiltered);
            hqp = Math.Min(hhqp, punfiltered);
            unfiltered = Math.Min(hunfiltered, punfiltered);
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

    public static float MarketSalesPerDay(int id, Market.Latency latency)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.IsUntradable)
        {
            return float.NaN;
        }

        DateTimeOffset retrievalTime;
        var results = Market.History(id, latency, out retrievalTime);

        if (results.history.Count == 0)
        {
            // welp
            return float.NaN;
        }

        long lastDate = results.history.Last().buyRealDate / 1000;
        long span = retrievalTime.ToUnixTimeSeconds() - lastDate;

        int totalQuantity = results.history.Sum(item => item.stack);

        return (float)totalQuantity / span * 60 * 60 * 24;
    }

    public static float MarketProfitAdjuster(float profit, int id, float acquired, Market.Latency latency)
    {
        if (profit < 0)
        {
            return profit;
        }

        if (!Db.Item(id).IsMarketable())
        {
            return 0;
        }

        float salesPerDay = MarketSalesPerDay(id, latency);

        float daysToSell = acquired / salesPerDay;

        return profit / Math.Max(daysToSell, 1);
    }

    public static float ValueSell(int id, bool hq, Market.Latency latency, out string destination)
    {
        var item = Db.Item(id);

        // This is always available (I think)
        float bestprice = item.Bid;
        if (hq)
        {
            // This seems to be the right equation
            bestprice = (float)Math.Ceiling(bestprice * 1.1f);
        }
        destination = "vendor";

        float market = ValueMarket(id, hq, Commerce.TransactionType.Fastsell, latency) * 0.95f;
        if (market > 0 && market > bestprice)
        {
            bestprice = market;
            destination = "market";
        }

        return bestprice;
    }

    public static float ValueSell(int id, bool hq, Market.Latency latency)
    {
        string _;
        return ValueSell(id, hq, latency, out _);
    }

    public static float ValueBuy(int id, bool hq, TransactionType type, Market.Latency latency, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return ValueMarket(id, hq, type, latency) * 1.05f;
        }

        float bestprice = ValueMarket(id, hq, type, latency) * 1.05f;
        source = "market";

        var item = Db.Item(id);

        if (CanBuyFromVendor(id))
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            float vendorprice = Db.Item(id).Ask;
            if (vendorprice > 0 && (float.IsNaN(bestprice) || vendorprice <= bestprice))
            {
                bestprice = vendorprice;
                source = "vendor";
            }
        }

        return bestprice;
    }

    public static float ValueBuy(int id, bool hq, TransactionType type, Market.Latency latency)
    {
        string _;
        return ValueBuy(id, hq, type, latency, out _);
    }

    
    public static bool CanBuyFromVendor(int id)
    {
        return Marketables().Contains(id);
    }

    private static Dictionary<int, SaintCoinach.Xiv.ENpc[]> itemToNPC;
    private static Dictionary<SaintCoinach.Xiv.ENpc, int> npcToItemCount;
    private static string[] blacklistedMerchants = new string[]
    {
        
    };

    private static void ConsumeData(SaintCoinach.Xiv.GilShop shop, Dictionary<int, List<SaintCoinach.Xiv.ENpc>> marketablesTemp, SaintCoinach.Xiv.ENpc npc)
    {
        foreach (var element in shop.Items)
        {
            int itemid = element.Item.Key;

            if (!marketablesTemp.ContainsKey(itemid))
            {
                marketablesTemp[itemid] = new List<SaintCoinach.Xiv.ENpc>();
            }

            marketablesTemp[itemid].Add(npc);
        }
    }

    private static void ConsumeData(SaintCoinach.Xiv.XivRow row, Dictionary<int, List<SaintCoinach.Xiv.ENpc>> marketablesTemp, SaintCoinach.Xiv.ENpc npc)
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
            var marketablesTemp = new Dictionary<int, List<SaintCoinach.Xiv.ENpc>>();
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

            itemToNPC = new Dictionary<int, SaintCoinach.Xiv.ENpc[]>();
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

            Dbg.Inf("Done");
        }
    }

    public static IEnumerable<int> Marketables()
    {
        GenerateMarketables();

        return itemToNPC.Select(x => x.Key);
    }

    public static IEnumerable<SaintCoinach.Xiv.ENpc> SellersForItem(int itemId)
    {
        return itemToNPC.ContainsKey(itemId) ? itemToNPC[itemId] : Enumerable.Empty<SaintCoinach.Xiv.ENpc>();
    }

    public static int ItemCountInShop(SaintCoinach.Xiv.ENpc seller)
    {
        return npcToItemCount.TryGetValue(seller);
    }
}
