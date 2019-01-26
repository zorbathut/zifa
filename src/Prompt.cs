
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Prompt
{
    private static Regex PointRegex = new Regex("^gpoint( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex ValueRegex = new Regex("^vendornet( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        while (true)
        {
            Dbg.Inf("");
            Dbg.Inf("");
            Dbg.Inf("Options:");
            Dbg.Inf("  gpoint wind coin");
            Dbg.Inf("  vendornet tomestone poetic");
            Dbg.Inf("");

            string instr = Console.ReadLine();
            if (PointRegex.Match(instr) is var pmatch && pmatch.Success)
            {
                GatherpointCalculator(pmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
            }
            else if (ValueRegex.Match(instr) is var vmatch && vmatch.Success)
            {
                DoPurchasableAnalysis(Db.ItemLoose(vmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray()).Key);
            }
        }
    }

    public static void DoPurchasableAnalysis(int itemId)
    {
        var results = new List<Bootstrap.Result>();
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

                float value = 0;
                string label = "";
                foreach (var reward in listing.Rewards)
                {
                    if (reward.Item.Key == 0 || reward.Item.IsUntradable)
                    {
                        continue;
                    }

                    value += Commerce.ValueSell(reward.Item.Key, reward.IsHq) * reward.Count / Commerce.MarketProfitDelayQuotient(reward.Item.Key);
                    label += $"{reward.Item.Name}{(reward.Count > 1 ? $" x{reward.Count}" : "")}{(reward.IsHq ? " HQ" : "")} ";
                }

                if (value > 0)
                {
                    results.Add(new Bootstrap.Result() { gps = value / tomestones, name = label });
                }
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
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
}
