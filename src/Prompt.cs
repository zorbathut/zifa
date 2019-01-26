
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Prompt
{
    private static Regex PointRegex = new Regex("^point( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        while (true)
        {
            Dbg.Inf("");
            Dbg.Inf("");
            Dbg.Inf("Options:");
            Dbg.Inf("  point wind coin");
            Dbg.Inf("");

            string instr = Console.ReadLine();
            if (PointRegex.Match(instr) is var match && match.Success)
            {
                // I really feel like there should be a better way to do this
                var tokens = match.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray();

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
                // how many points
            }
        }
    }
}
