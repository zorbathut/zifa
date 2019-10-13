
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
    private static Regex RetainerGatherRegex = new Regex("^retainergather (?<role>(dow|btn|min|fsh)) (?<skill>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex CraftSourceRegex = new Regex("^craftsource (?<role>[a-zA-Z]+) (?<levelmin>[0-9]+) (?<levelmax>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        while (true)
        {
            Dbg.Inf("");
            Dbg.Inf("");
            Dbg.Inf("Options:");
            Dbg.Inf("  gpoint wind coin - finds the most profitable item to acquire at a gathering point, given some items names");
            Dbg.Inf("  gatherbest (bot) (mine) - finds the best items to gather, given botanist and miner levels");
            Dbg.Inf("  vendornet 2000 tomestone poetic - finds the best way to turn an item into money, given a quantity of that item and the item's name");
            Dbg.Inf("  acquirenet rakshasa token - finds the best way to acquire an item, given the item name");
            Dbg.Inf("  analyze craftsman vi - dumps various crafting and market info on an item");
            Dbg.Inf("  rewards nickel turban high steel fending - chooses the best quest reward, given some items names");
            Dbg.Inf("  vendormarket - finds the best items to be purchased from vendors and marketed");
            Dbg.Inf("  gathercalc (lchance) (hqchance) (maxgp) (attempts) (hqonly) - calculates the best way to gather items given current stats");
            Dbg.Inf("  retainergather {dow/btn/min/fsh} {skill} - calculates the best items for retainers to gather");
            Dbg.Inf("  craftsource {crafter} {levelmin} {levelmax} - figures out where to acquire a set of items from based on a level range for crafters");
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
            else if (RetainerGatherRegex.Match(instr) is var rgmatch && rgmatch.Success)
            {
                string role = rgmatch.Groups["role"].Value;
                int skill = int.Parse(rgmatch.Groups["skill"].Value);

                DoRetainerGatherAnalysis(role, skill);
            }
            else if (CraftSourceRegex.Match(instr) is var csmatch && csmatch.Success)
            {
                string role = csmatch.Groups["role"].Value;
                int levelmin = int.Parse(csmatch.Groups["levelmin"].Value);
                int levelmax = int.Parse(csmatch.Groups["levelmax"].Value);

                DoCraftSourceAnalysis(role, levelmin, levelmax);
            }
            else
            {
                Dbg.Inf("nope nope nope");
            }
        }
    }

    public static void DoPurchasableAnalysis(int itemId, int amount)
    {
        foreach (var result in PurchasableAnalysisWorker(itemId, amount, true).OrderBy(item => item.gps))
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static IEnumerable<Bootstrap.Result> PurchasableAnalysisWorker(int itemId, float amountAcquired, bool pb = false)
    {
        var inspected = new HashSet<int>();
        IEnumerable<SaintCoinach.Xiv.SpecialShop> items = Db.GetSheet<SaintCoinach.Xiv.SpecialShop>();
        if (pb)
        {
            items = items.ProgressBar();
        }
        foreach (var shop in items)
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

                // Always include this, because this is how we calculate vendor prices
                {
                    float valueBase = Commerce.ValueSell(reward.Item.Key, reward.IsHq, Market.Latency.Standard) * reward.Count;
                    float valueAdjusted = Commerce.MarketProfitAdjuster(valueBase, reward.Item.Key, amountAcquired / cost  * reward.Count, Market.Latency.Standard);
                    yield return new Bootstrap.Result() { gps = valueAdjusted / cost, name = label };
                }

                // Branch out if we can't sell it on the market; there might be more lucrative options!
                if (!reward.Item.IsMarketable())
                {
                    foreach (var elem in PurchasableAnalysisWorker(reward.Item.Key, amountAcquired / cost * reward.Count))
                    {
                        yield return new Bootstrap.Result() { gps = elem.gps / cost * reward.Count, name = $"{label} -> {elem.name}" };
                    }
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
        // Let's just init some stuff . . .
        Commerce.GenerateMarketables();
        Api.InitCherenkov();

        foreach (var item in items)
        {
            Dbg.Inf("");
            Dbg.Inf($"{item.Name}:");
            Dbg.Inf($"  Compiled market data:");
            Dbg.Inf($"    Immediate: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Immediate, Market.Latency.Immediate)}");
            Dbg.Inf($"    Immediate HQ: {Commerce.ValueMarket(item.Key, true, Commerce.TransactionType.Immediate, Market.Latency.Immediate)}");
            Dbg.Inf($"    Longterm: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Longterm, Market.Latency.Immediate)}");
            Dbg.Inf($"    Fastsell: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Fastsell, Market.Latency.Immediate)}");
            Dbg.Inf($"    Fastsell HQ: {Commerce.ValueMarket(item.Key, true, Commerce.TransactionType.Fastsell, Market.Latency.Immediate)}");
            Dbg.Inf($"    Sales per day: {Commerce.MarketSalesPerDay(item.Key, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (1): {Commerce.MarketProfitAdjuster(1, item.Key, 1, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (10): {Commerce.MarketProfitAdjuster(1, item.Key, 10, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (99): {Commerce.MarketProfitAdjuster(1, item.Key, 99, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (stack): {Commerce.MarketProfitAdjuster(1, item.Key, item.StackSize, Market.Latency.Immediate)}");
            Dbg.Inf("");

            bool recipeHeadered = false;
            foreach (var recipe in Db.GetSheet<SaintCoinach.Xiv.Recipe>())
            {
                if (recipe.ResultItem == item)
                {
                    if (!recipeHeadered)
                    {
                        Dbg.Inf("  Crafting:");
                        recipeHeadered = true;
                    }
                    Dbg.Inf("  " + Bootstrap.EvaluateItem(recipe, false, true, Market.Latency.Immediate).Item2.Replace("\n", "\n  "));
                    Dbg.Inf("");
                }
            }

            if (Commerce.SellersForItem(item.Key).Any())
            {
                Dbg.Inf("  Vendors:");
                foreach (var seller in Commerce.SellersForItem(item.Key))
                {
                    Dbg.Inf($"    {seller.ToZifaString()}");
                }
                Dbg.Inf("");
            }

            if (item.IsMarketable())
            {
                Dbg.Inf("  Pricing:");
                foreach (var market in Market.Prices(item.Key, Market.Latency.Immediate).entries)
                {
                    Dbg.Inf($"    {market.sellPrice}: {market.stack}x {(market.hq ? "HQ" : "")}");
                }

                Dbg.Inf("");
                Dbg.Inf("  History:");
                foreach (var market in Market.History(item.Key, Market.Latency.Immediate).history)
                {
                    Dbg.Inf($"    {market.sellPrice}: {market.stack}x {(market.hq ? "HQ" : "")} {(DateTimeOffset.Now - DateTimeOffset.FromUnixTimeMilliseconds(market.buyRealDate)).TotalDays:F2}d");
                }
                Dbg.Inf("");
            }
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
            float profit = Commerce.MarketProfitAdjuster(Commerce.ValueMarket(id, false, Commerce.TransactionType.Fastsell, Market.Latency.Standard) - item.Ask, id, stack, Market.Latency.Standard);

            results.Add(new MarketInfo() { profit = profit * stack, text = $"{Math.Round(profit * stack)}: {item.Name}"});
        }

        foreach (var result in results.OrderBy(mi => mi.profit))
        {
            Dbg.Inf(result.text);
        }
    }

    enum GatherType
    {
        Warrior,
        Gatherer,
        Fisher,
    }
    public static void DoRetainerGatherAnalysis(string role, int skill)
    {
        GatherType gatherer = GatherType.Gatherer;

        if (role == "min") role = "miner";
        if (role == "btn") role = "botanist";
        if (role == "dow") { role = "rogue"; gatherer = GatherType.Warrior; } // close enough
        if (role == "fsh") { role = "fisher"; gatherer = GatherType.Fisher; }

        var results = new List<MarketInfo>();
        foreach (var task in Db.GetSheet<SaintCoinach.Xiv.RetainerTask>().ProgressBar())
        {
            if (!task.ClassJobCategory.ClassJobs.Any(cj => cj.Name == role))
            {
                continue;
            }

            if (gatherer == GatherType.Gatherer && task.RequiredGathering > skill)
            {
                continue;
            }

            if (gatherer == GatherType.Warrior && task.RequiredItemLevel > skill)
            {
                continue;
            }

            if (gatherer == GatherType.Fisher && task.RequiredGathering > skill)
            {
                continue;
            }

            var items = task.Items.ToArray();
            if (items.Length != 1)
            {
                continue;
            }

            int itemId = items[0].Key;

            // Gotta figure out how many we expect to get.
            var parameter = task["RetainerTaskParameter"] as SaintCoinach.Xiv.XivRow;
            
            int midthresh;
            int highthresh;

            if (gatherer == GatherType.Fisher)
            {
                midthresh = parameter.AsInt32("Gathering{FSH}[0]");
                highthresh = parameter.AsInt32("Gathering{FSH}[1]");
            }
            else if (gatherer == GatherType.Warrior)
            {
                midthresh = parameter.AsInt32("ItemLevel{DoW}[0]");
                highthresh = parameter.AsInt32("ItemLevel{DoW}[1]");
            }
            else
            {
                midthresh = parameter.AsInt32("Gathering{DoL}[0]");
                highthresh = parameter.AsInt32("Gathering{DoL}[1]");
            }

            var normal = task.Task as SaintCoinach.Xiv.RetainerTaskNormal;
            if (normal == null)
            {
                continue;
            }

            int quantity;
            if (highthresh <= skill)
            {
                quantity = normal.AsInt32("Quantity[2]");
            }
            else if (midthresh <= skill)
            {
                quantity = normal.AsInt32("Quantity[1]");
            }
            else
            {
                quantity = normal.AsInt32("Quantity[0]");
            }

            int throughput = quantity * 4; // penalize things that don't have enough daily sales

            float profit = 0;
            if (items[0].CanBeHq)
            {
                // 20% HQ; vague estimate
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(itemId, true, Commerce.TransactionType.Fastsell, Market.Latency.Standard), itemId, throughput * 0.2f, Market.Latency.Standard) * quantity * 0.2f;
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(itemId, false, Commerce.TransactionType.Fastsell, Market.Latency.Standard), itemId, throughput * 0.8f, Market.Latency.Standard) * quantity * 0.8f;
            }
            else
            {
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(itemId, false, Commerce.TransactionType.Fastsell, Market.Latency.Standard), itemId, throughput, Market.Latency.Standard) * quantity;
            }

            results.Add(new MarketInfo() { profit = profit, text = $"{profit}: {quantity}x {items[0].Name}" });
        }

        foreach (var result in results.OrderBy(mi => mi.profit))
        {
            Dbg.Inf(result.text);
        }
    }

    private static string CraftSourceFormatter(SaintCoinach.Xiv.Item item, int count, float gil)
    {
        if (gil > 0)
        {
            return $"  {item.Name} x{count} (~{gil:F0}g ea)\n";
        }
        else
        {
            return $"  {item.Name} x{count}\n";
        }
    }

    private static HashSet<SaintCoinach.Xiv.Recipe> standardRecipes;
    public static void DoCraftSourceAnalysis(string role, int minlevel, int maxlevel)
    {
        if (standardRecipes == null)
        {
            // init the list of standard recipes
            standardRecipes = new HashSet<SaintCoinach.Xiv.Recipe>();
            foreach (var recipeBank in Db.Realm.GameData.GetSheet("RecipeNotebookList"))
            {
                if (recipeBank.Key >= 1000)
                    continue;

                for (int i = 0; true; ++i)
                {
                    var recipe = (SaintCoinach.Xiv.Recipe)recipeBank[$"Recipe[{i}]"];

                    if (recipe == null)
                    {
                        break;
                    }

                    standardRecipes.Add(recipe);
                }
            }
        }

        var items = new Dictionary<SaintCoinach.Xiv.Item, int>();
        foreach (var recipe in standardRecipes.Where(recipe => {
                var result = recipe.ResultItem;
                int resultId = result.Key;
            
                if (resultId == 0)
                {
                    return false;
                }

                if (!result.IsMarketable())
                {
                    return false;
                }
            
                // filter out ixal
                if (recipe.RequiredItem.Key != 0)
                {
                    return false;
                }

                string className = recipe.ClassJob.Name;
                int classLevel = recipe.RecipeLevelTable.ClassJobLevel;

                if (recipe.ClassJob.Name != role)
                {
                    return false;
                }

                if (classLevel < minlevel || classLevel > maxlevel)
                {
                    return false;
                }

                return true;
            }).ProgressBar())
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!items.ContainsKey(ingredient.Item))
                {
                    items[ingredient.Item] = 0;
                }

                items[ingredient.Item] = items[ingredient.Item] + ingredient.Count;
            }

            bool careAboutHQ = false;
            if (recipe.ResultItem.CanBeHq)
            {
                float basePrice = Commerce.ValueSell(recipe.ResultItem.Key, false, Market.Latency.Standard);
                float hqPrice = Commerce.ValueSell(recipe.ResultItem.Key, true, Market.Latency.Standard);

                careAboutHQ = !(hqPrice - 100 <= basePrice || hqPrice / 1.2f <= basePrice);
            }
            
            if (careAboutHQ)
            {
                if (!items.ContainsKey(recipe.ResultItem))
                {
                    items[recipe.ResultItem] = 0;
                }

                items[recipe.ResultItem] = items[recipe.ResultItem] - recipe.ResultCount;
            }
        }

        // Strip out negatives
        items = items.Where(kvp => kvp.Value > 0).ToDictionary();

        {
            string result = "Market-procured:\n";

            var remaining = new HashSet<SaintCoinach.Xiv.Item>();

            foreach (var itemcombo in items.OrderBy(itemcombo => itemcombo.Key.Name).ProgressBar(false))
            {
                // First filter out the market purchases

                float value = Commerce.ValueBuy(itemcombo.Key.Key, false, Commerce.TransactionType.Immediate, Market.Latency.Immediate, out var source);
                if (source == "market")
                {
                    result += CraftSourceFormatter(itemcombo.Key, itemcombo.Value, value);
                }
                else
                {
                    remaining.Add(itemcombo.Key);
                }
            }

            while (true)
            {
                bool found = false;
                foreach (var item in remaining)
                {
                    if (Commerce.SellersForItem(item.Key).Count() == 1)
                    {
                        remaining = SubsumeItems(remaining, items, Commerce.SellersForItem(item.Key).First(), ref result);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    break;
                }
            }

            while (remaining.Count > 0)
            {
                var npcCounts = new Dictionary<SaintCoinach.Xiv.ENpc, int>();
                foreach (var item in remaining)
                {
                    foreach (var npc in Commerce.SellersForItem(item.Key))
                    {
                        if (!npcCounts.ContainsKey(npc))
                        {
                            npcCounts[npc] = 0;
                        }

                        npcCounts[npc] = npcCounts[npc] + 1;
                    }
                }

                if (npcCounts.Count == 0)
                {
                    result += "\nCan't be found:\n";
                    foreach (var item in remaining)
                    {
                        result += CraftSourceFormatter(item, items[item], -1);
                    }
                    break;
                }

                var bestNpc = npcCounts.MaxBy(kv => kv.Value * 100000 + Commerce.ItemCountInShop(kv.Key)).Key;

                // Grab things
                remaining = SubsumeItems(remaining, items, bestNpc, ref result);
            }

            Dbg.Inf(result);
        }
    }

    private static HashSet<SaintCoinach.Xiv.Item> SubsumeItems(HashSet<SaintCoinach.Xiv.Item> remaining, Dictionary<SaintCoinach.Xiv.Item, int> items, SaintCoinach.Xiv.ENpc npc, ref string result)
    {
        result += $"\n{npc.ToZifaString()}:\n";

        var newRemaining = new HashSet<SaintCoinach.Xiv.Item>();
        foreach (var item in remaining.OrderBy(item => item.Name))
        {
            if (Commerce.SellersForItem(item.Key).Contains(npc))
            {
                result += CraftSourceFormatter(item, items[item], -1);
            }
            else
            {
                newRemaining.Add(item);
            }
        }

        return newRemaining;
    }
}
