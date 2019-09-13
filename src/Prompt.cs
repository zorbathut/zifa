
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Prompt
{
    private static Regex PointRegex = new Regex("^gpoint( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex GatherRegex = new Regex("^gatherbest (?<gather>[0-9]+) (?<mine>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex ValueRegex = new Regex("^vendornet (?<amount>[0-9]+)( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex AcquireRegex = new Regex("^acquirenet( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex AnalyzeRegex = new Regex("^analyze( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex RewardsRegex = new Regex("^rewards( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex GatherCalcRegex = new Regex("^gathercalc (?<lchance>[0-9]+) (?<hqchance>[0-9]+) (?<maxgp>[0-9]+) (?<attempts>[0-9]+) (?<hqonly>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        while (true)
        {
            Dbg.Inf("");
            Dbg.Inf("");
            Dbg.Inf("Options:");
            Dbg.Inf("  gpoint wind coin");
            Dbg.Inf("  gatherbest 70 70");
            Dbg.Inf("  vendornet 2000 tomestone poetic");
            Dbg.Inf("  acquirenet rakshasa token");
            Dbg.Inf("  analyze craftsman vi");
            Dbg.Inf("  rewards nickel turban high steel fending");
            Dbg.Inf("  vendormarket");
            Dbg.Inf("  gathercalc (lchance) (hqchance) (maxgp) (attempts) (hqonly)");
            Dbg.Inf("");

            string instr = Console.ReadLine();
            if (PointRegex.Match(instr) is var pmatch && pmatch.Success)
            {
                GatherpointCalculator(pmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
            }
            else if (GatherRegex.Match(instr) is var gmatch && gmatch.Success)
            {
                int gather = int.Parse(gmatch.Groups["gather"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                int mine = int.Parse(gmatch.Groups["mine"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                for (int i = 5; i <= Math.Max(gather, mine); i += 5)
                {
                    GatherbestCalculator(Math.Min(gather, i), Math.Min(mine, i));
                    Dbg.Inf($"THAT'S IT UP TO {i}");
                }
                
            }
            else if (ValueRegex.Match(instr) is var vmatch && vmatch.Success)
            {
                int amount = int.Parse(vmatch.Groups["amount"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).First());
                var items = Db.ItemLoose(vmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()).ToArray();
                if (items.Length == 0)
                {
                    Dbg.Inf("can't find :(");
                }
                else if (items.Length > 1)
                {
                    Dbg.Inf("Too many!");
                    foreach (var item in items)
                    {
                        Dbg.Inf($"  {item.Name}");
                    }
                }
                else
                {
                    DoPurchasableAnalysis(items[0].Key, amount);
                }
            }
            else if (AcquireRegex.Match(instr) is var qmatch && qmatch.Success)
            {
                var items = Db.ItemLoose(qmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()).ToArray();
                if (items.Length == 0)
                {
                    Dbg.Inf("can't find :(");
                }
                else if (items.Length > 1)
                {
                    Dbg.Inf("Too many!");
                    foreach (var item in items)
                    {
                        Dbg.Inf($"  {item.Name}");
                    }
                }
                else
                {
                    DoAcquireableAnalysis(items[0].Key, 1);
                }
            }
            else if (AnalyzeRegex.Match(instr) is var amatch && amatch.Success)
            {
                DoItemAnalysis(Db.ItemLoose(amatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()));
            }
            else if (RewardsRegex.Match(instr) is var rmatch && rmatch.Success)
            {
                DoRewardsAnalysis(rmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
            }
            else if (instr == "vendormarket")
            {
                DoVendorMarketAnalysis();
            }
            else if (GatherCalcRegex.Match(instr) is var gcmatch && gcmatch.Success)
            {
                int lchance = int.Parse(gcmatch.Groups["lchance"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                int hqchance = int.Parse(gcmatch.Groups["hqchance"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                int maxgp = int.Parse(gcmatch.Groups["maxgp"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                int attempts = int.Parse(gcmatch.Groups["attempts"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);
                int hqonly = int.Parse(gcmatch.Groups["hqonly"].Captures.OfType<System.Text.RegularExpressions.Capture>().First().Value);

                GatheringCalculator.ProcessLongterm(lchance, hqchance, maxgp, attempts, hqonly != 0);
            }
            else
            {
                Dbg.Inf("nope nope nope");
            }
        }
    }

    public static void DoPurchasableAnalysis(int itemId, int amount)
    {
        foreach (var result in PurchasableAnalysisWorker(itemId, amount).OrderBy(item => item.gps))
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static IEnumerable<Bootstrap.Result> PurchasableAnalysisWorker(int itemId, float amountAcquired)
    {
        var inspected = new HashSet<int>();
        foreach (var shop in Db.GetSheet<SaintCoinach.Xiv.SpecialShop>())
        {
            foreach (var listing in shop.Items)
            {
                int cost = 0;
                foreach (var costElement in listing.Costs)
                {
                    if (costElement.Item == null || costElement.Item.Key != itemId)
                    {
                        cost = -1;
                        break;
                    }

                    cost = costElement.Count;
                }

                if (cost <= 0)
                {
                    continue;
                }

                if (listing.Rewards.Count() > 1)
                {
                    yield return new Bootstrap.Result() { gps = 0, name = "TOO MANY RESULTS" };
                    continue;
                }

                if (listing.Rewards.Count() == 0)
                {
                    continue;
                }

                var reward = listing.Rewards.First();

                if (reward.Item.Key == 0)
                {
                    continue;
                }

                string name = reward.Item.Name;
                var label = $"{name}{(reward.Count > 1 ? $" x{reward.Count}" : "")}{(reward.IsHq ? " HQ" : "")}";

                if (!reward.Item.IsMarketable())
                {
                    foreach (var elem in PurchasableAnalysisWorker(reward.Item.Key, amountAcquired / cost * reward.Count))
                    {
                        yield return new Bootstrap.Result() { gps = elem.gps / cost * reward.Count, name = $"{label} -> {elem.name}" };
                    }
                }
                else
                {
                    float valueBase = Commerce.ValueSell(reward.Item.Key, reward.IsHq, Market.Latency.Standard) * reward.Count;
                    float valueAdjusted = Commerce.MarketProfitAdjuster(valueBase, reward.Item.Key, amountAcquired / cost  * reward.Count, Market.Latency.Standard);
                    yield return new Bootstrap.Result() { gps = valueAdjusted / cost, name = label };
                }
            }
        }
    }

    public static void DoAcquireableAnalysis(int itemId, int amount)
    {
        foreach (var result in AcquireableAnalysisWorker(itemId, amount).OrderByDescending(item => item.gps))
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static IEnumerable<Bootstrap.Result> AcquireableAnalysisWorker(int itemId, float amountNeeded)
    {
        var inspected = new HashSet<int>();
        foreach (var shop in Db.GetSheet<SaintCoinach.Xiv.SpecialShop>())
        {
            foreach (var listing in shop.Items)
            {
                int reward = 0;
                foreach (var rewardElement in listing.Rewards)
                {
                    if (rewardElement.Item?.Key == itemId)
                    {
                        reward = rewardElement.Count;
                    }
                }

                if (reward <= 0)
                {
                    continue;
                }

                if (listing.Costs.Count() > 1)
                {
                    yield return new Bootstrap.Result() { gps = 0, name = "TOO MANY RESULTS" };
                    continue;
                }

                var cost = listing.Costs.First();

                if (cost.Item.Key == 0)
                {
                    continue;
                }

                string name = cost.Item.Name;
                var label = $"{name}{(cost.Count > 1 ? $" x{cost.Count}" : "")}{(cost.IsHq ? " HQ" : "")}";

                if (!cost.Item.IsMarketable())
                {
                    foreach (var elem in AcquireableAnalysisWorker(cost.Item.Key, amountNeeded / reward * cost.Count))
                    {
                        yield return new Bootstrap.Result() { gps = elem.gps / reward * cost.Count, name = $"{label} -> {elem.name}" };
                    }
                }
                else
                {
                    float value = Commerce.ValueBuy(cost.Item.Key, cost.IsHq, Commerce.TransactionType.Immediate, Market.Latency.Standard) * cost.Count;
                    yield return new Bootstrap.Result() { gps = value / reward, name = label };
                }
            }
        }
    }

    public static void GatherpointCalculator(string[] tokens)
    {
        var prospective = new List<SaintCoinach.Xiv.GatheringPointBase>();
        var items = new HashSet<int>();
        foreach (var point in Db.GetSheet<SaintCoinach.Xiv.GatheringPointBase>())
        {
            bool valid = true;
            foreach (var token in tokens)
            {
                bool found = false;
                foreach (var gitem in point.Items)
                {
                    var item = gitem.Item;

                    if (item.Name.ToString().IndexOf(token, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                prospective.Add(point);

                foreach (var gitem in point.Items)
                {
                    if (gitem.Item.Key != 0)
                    {
                        items.Add(gitem.Item.Key);
                    }
                }
            }
        }

        Dbg.Inf($"{prospective.Count} matches");
        foreach (var item in items.Select(id => Tuple.Create(Db.Item(id), Commerce.MarketProfitAdjuster(Commerce.ValueSell(id, false, Market.Latency.Standard), id, 30, Market.Latency.Standard))).OrderByDescending(tup => tup.Item2))
        {
            Dbg.Inf($"  {item.Item1.Name}: {item.Item2:F0}");
        }
    }

    public static void GatherbestCalculator(int gather, int mine)
    {
        var prospective = new HashSet<SaintCoinach.Xiv.GatheringPointBase>();
        foreach (var point in Db.GetSheet<SaintCoinach.Xiv.GatheringPoint>())
        {
            if (point.TerritoryType != null)
            {
                prospective.Add(point.Base);
            }
        }

        var seen = new HashSet<int>();
        seen.Add(0); // yes yes it's a hack
        var results = new List<Tuple<float, string>>();
        foreach (var point in prospective)
        {
            string pointtype = point.Type.Name;
            bool valid = false;
            if ((pointtype == "Mining" || pointtype == "Quarrying") && point.GatheringLevel <= gather) valid = true;
            if ((pointtype == "Harvesting" || pointtype == "Logging") && point.GatheringLevel <= mine) valid = true;

            if (!valid)
            {
                continue;
            }

            foreach (var gitem in point.Items)
            {
                if (seen.Contains(gitem.Item.Key))
                {
                    continue;
                }
                seen.Add(gitem.Item.Key);

                if (gitem.Item is SaintCoinach.Xiv.Item item)
                {
                    if (item.IsUntradable)
                    {
                        continue;
                    }

                    float value = Commerce.MarketProfitAdjuster(Commerce.ValueSell(item.Key, false, Market.Latency.Standard), item.Key, point.IsLimited ? 10 : 99, Market.Latency.Standard);
                    string usp = point.IsLimited ? "USP" : "   ";
                    results.Add(Tuple.Create(value, $"{usp} {value}: {item.Name}"));
                }
                
            }
        }

        Dbg.Inf($"{results.Count} matches");
        foreach (var item in results.OrderBy(tup => tup.Item1))
        {
            Dbg.Inf(item.Item2);
        }
    }

    public static void DoItemAnalysis(IEnumerable<SaintCoinach.Xiv.Item> items)
    {
        foreach (var item in items)
        {
            Dbg.Inf("");
            Dbg.Inf($"{item.Name}:");
            Dbg.Inf($"  Market immediate: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Immediate, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market longterm: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Longterm, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market fastsell: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Fastsell, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market sales per day: {Commerce.MarketSalesPerDay(item.Key, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market profit adjustment (1): {Commerce.MarketProfitAdjuster(1, item.Key, 1, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market profit adjustment (10): {Commerce.MarketProfitAdjuster(1, item.Key, 10, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market profit adjustment (99): {Commerce.MarketProfitAdjuster(1, item.Key, 99, Market.Latency.Immediate)}");
            Dbg.Inf($"  Market profit adjustment (stack): {Commerce.MarketProfitAdjuster(1, item.Key, item.StackSize, Market.Latency.Immediate)}");

            foreach (var recipe in Db.GetSheet<SaintCoinach.Xiv.Recipe>())
            {
                if (recipe.ResultItem == item)
                {
                    Dbg.Inf("");
                    Dbg.Inf(Bootstrap.EvaluateItem(recipe, false, true, Market.Latency.Immediate).Item2);
                }
            }

            Dbg.Inf("");
        }
    }

    public static void DoRewardsAnalysis(string[] tokens)
    {
        var prospective = new List<SaintCoinach.Xiv.Quest>();
        var rewards = new HashSet<SaintCoinach.Xiv.QuestRewardItem>();
        foreach (var quest in Db.GetSheet<SaintCoinach.Xiv.Quest>())
        {
            bool valid = true;
            foreach (var token in tokens)
            {
                bool found = false;
                foreach (var group in quest.Rewards.Items)
                {
                    foreach (var item in group.Items)
                    {
                        if (item.Item.Name.ToString().IndexOf(token, StringComparison.CurrentCultureIgnoreCase) != -1)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                prospective.Add(quest);

                foreach (var group in quest.Rewards.Items)
                {
                    foreach (var reward in group.Items)
                    {
                        if (reward.Item.Key != 0)
                        {
                            rewards.Add(reward);
                        }
                    }
                }
            }
        }

        Dbg.Inf($"{prospective.Count} matches");
        foreach (var item in rewards.Select(reward => Tuple.Create(reward.Item, Commerce.MarketProfitAdjuster(Commerce.ValueSell(reward.Item.Key, false, Market.Latency.Standard), reward.Item.Key, reward.Counts[0], Market.Latency.Standard) * reward.Counts[0])).OrderByDescending(tup => tup.Item2))
        {
            Dbg.Inf($"  {item.Item1.Name}: {item.Item2:F0}");
        }
    }

    struct MarketInfo
    {
        public float profit;
        public string text;
    }
    public static void DoVendorMarketAnalysis()
    {
        int[] marketables = Commerce.Marketables().ToArray();
        var results = new List<MarketInfo>();

        for (int i = 0; i < marketables.Length; ++i)
        {
            Dbg.Inf($"{i}/{marketables.Length}");

            var id = marketables[i];
            var item = Db.Item(id);
            if (!item.IsMarketable())
            {
                continue;
            }

            int stack = Math.Min(item.StackSize, 99);
            float profit = Commerce.MarketProfitAdjuster(Commerce.ValueMarket(id, false, Commerce.TransactionType.Fastsell, Market.Latency.Immediate) - item.Ask, id, stack, Market.Latency.Immediate);

            results.Add(new MarketInfo() { profit = profit * stack, text = $"{Math.Round(profit * stack)}: {item.Name}"});
        }

        foreach (var result in results.OrderBy(mi => mi.profit))
        {
            Dbg.Inf(result.text);
        }
    }
}
