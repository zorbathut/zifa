
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Prompt
{
    private static Regex PointRegex = new Regex("^gpoint( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex ValueRegex = new Regex("^vendornet( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex AnalyzeRegex = new Regex("^analyze( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        while (true)
        {
            Dbg.Inf("");
            Dbg.Inf("");
            Dbg.Inf("Options:");
            Dbg.Inf("  gpoint wind coin");
            Dbg.Inf("  vendornet tomestone poetic");
            Dbg.Inf("  analyze craftsman vi");
            Dbg.Inf("");

            string instr = Console.ReadLine();
            if (PointRegex.Match(instr) is var pmatch && pmatch.Success)
            {
                GatherpointCalculator(pmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
            }
            else if (ValueRegex.Match(instr) is var vmatch && vmatch.Success)
            {
                DoPurchasableAnalysis(Db.ItemLoose(vmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()).First().Key);
            }
            else if (AnalyzeRegex.Match(instr) is var amatch && amatch.Success)
            {
                DoItemAnalysis(Db.ItemLoose(amatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()));
            }
        }
    }

    public static void DoPurchasableAnalysis(int itemId)
    {
        foreach (var result in PurchasableAnalysisWorker(itemId).OrderBy(item => item.gps))
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static IEnumerable<Bootstrap.Result> PurchasableAnalysisWorker(int itemId)
    {
        var inspected = new HashSet<int>();
        foreach (var shop in Db.GetSheet<SaintCoinach.Xiv.SpecialShop>())
        {
            foreach (var listing in shop.Items)
            {
                int tomestones = 0;
                foreach (var cost in listing.Costs)
                {
                    if (cost.Item == null || cost.Item.Key != itemId)
                    {
                        tomestones = -1;
                        break;
                    }

                    tomestones = cost.Count;
                }

                if (tomestones <= 0)
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

                var label = $"{reward.Item.Name}{(reward.Count > 1 ? $" x{reward.Count}" : "")}{(reward.IsHq ? " HQ" : "")}";

                if (reward.Item.IsUntradable)
                {
                    foreach (var elem in PurchasableAnalysisWorker(reward.Item.Key))
                    {
                        yield return new Bootstrap.Result() { gps = elem.gps / tomestones, name = $"{label} -> {elem.name}" };
                    }
                }
                else
                {
                    float value = Commerce.ValueSell(reward.Item.Key, reward.IsHq) * reward.Count / Commerce.MarketProfitDelayQuotient(reward.Item.Key);
                    yield return new Bootstrap.Result() { gps = value / tomestones, name = label };
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
        foreach (var item in items.Select(id => Tuple.Create(Db.Item(id), Commerce.ValueSell(id, false))).OrderByDescending(tup => tup.Item2))
        {
            Dbg.Inf($"  {item.Item1.Name}: {item.Item2:F0}");
        }
    }

    public static void DoItemAnalysis(IEnumerable<SaintCoinach.Xiv.Item> items)
    {
        foreach (var item in items)
        {
            Dbg.Inf("");
            Dbg.Inf($"{item.Name}:");
            Dbg.Inf($"  Market immediate: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Immediate)}:");
            Dbg.Inf($"  Market longterm: {Commerce.ValueMarket(item.Key, false, Commerce.TransactionType.Longterm)}:");
            Dbg.Inf($"  Market delay quotient: {Commerce.MarketProfitDelayQuotient(item.Key)}:");
        }
    }
}
