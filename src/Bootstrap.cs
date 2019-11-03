
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public static class Bootstrap
{
    public class Result
    {
        public float gps;
        public string name;
    }

    public class CollectibleCombination
    {
        public string item;
        public int xp;
    }

    public enum SortMethod
    {
        Order,
        Profit,
    }

    public struct CraftingInfo
    {
        public string name;
        public int minlevel;
        public int maxhqlevel;
        public int maxlevel;
        public int craftsmanship;
        public int control;
    }

    public static void Main(string[] args)
    {
        Def.Config.InfoHandler = Dbg.Inf;
        Def.Config.WarningHandler = Dbg.Wrn;
        Def.Config.ErrorHandler = Dbg.Err;
        Def.Config.ExceptionHandler = Dbg.Ex;

        Cache.Init();
        Db.Init();
        Cherenkov.Session.Init();

        {
            var parser = new Def.Parser();
            foreach (var file in new DirectoryInfo(@"xml").GetFiles("*.xml"))
            {
                parser.AddString(File.ReadAllText(file.FullName), file.Name);
            }
            parser.Finish();
        }

        //DoRecipeAnalysis("goldsmith", 1, 0, 5, SortMethod.Order);
        //CraftingCalculator.Process();
        /*
        DoCollectibleCombinationMath(new CollectibleCombination[] {
            new CollectibleCombination() { item = "Chimerical Felt Hose of Aiming", xp = 279936 },
            new CollectibleCombination() { item = "Chimerical Felt Klobuk of Healing", xp = 205632 },
            new CollectibleCombination() { item = "Hallowed Ramie Sash of Casting", xp = 155520 },
            new CollectibleCombination() { item = "Ramie Cloth", xp = 166320 },
            new CollectibleCombination() { item = "Holy Rainbow Shoes", xp = 140400 },
            new CollectibleCombination() { item = "Holy Rainbow Shirt of Scouting", xp = 116640 },
            new CollectibleCombination() { item = "Rainbow Sash of Healing", xp = 79380 },
        });
        */

        Prompt.Run();
    }

    public static void DoGCScripAnalysis()
    {
        var results = new List<Result>();
        var inspected = new HashSet<int>();
        foreach (var scripEntry in Db.GetSheet2<SaintCoinach.Xiv.GCScripShopItem>())
        {
            var item = scripEntry.Item;

            if (item == null || item.Key == 0)
            {
                continue;
            }

            if (item.IsUntradable)
            {
                continue;
            }
            
            int id = item.Key;

            if (!inspected.Contains(id))
            {
                inspected.Add(id);

                float gps = Commerce.MarketProfitAdjuster(Commerce.ValueSell(id, false, Market.Latency.Standard) / scripEntry.GCSealsCost, id, 40000 / scripEntry.GCSealsCost, Market.Latency.Standard);

                results.Add(new Result() { gps = gps, name = item.Name });
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    readonly static string[] People = new string[] {"***REMOVED***", "***REMOVED***", "***REMOVED***"};
    public static Util.Twopass.Result EvaluateItem(SaintCoinach.Xiv.Recipe recipe, bool hq, bool canQuickSynth, Market.Latency latency, bool includeSolo, bool includeBulk)
    {
        var result = recipe.ResultItem;
        float expectedRevenue = Commerce.ValueSell(result.Key, hq, latency) * recipe.ResultCount;
        string readable = $"\n{recipe.ClassJob.Name} {recipe.ResultItem.Name} {(hq ? "HQ" : "NQ")} ({recipe.ResultItem.Key}): expected revenue {Commerce.ValueSell(result.Key, hq, latency):F0}";
        float tcost = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, latency, out string source);
            readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

            tcost += ingredient.Count * cost;
        }

        float profit = expectedRevenue - tcost;

        // Can't bulk-produce HQ, unfortunately
        bool allowBulkProduction = includeBulk && canQuickSynth && !hq;

        // This is the amount that we're allowed to sell per day
        float maxSellPerDay = Math.Min(Math.Min(Commerce.MarketSalesPerDay(result.Key, latency), Commerce.MarketExpectedStackSale(result.Key, latency)), Math.Min(result.StackSize, 99));
        if (!allowBulkProduction)
        {
            maxSellPerDay = Math.Min(maxSellPerDay, 3);
        }

        if (!includeSolo && maxSellPerDay <= 3)
        {
            return new Util.Twopass.Result() { value = float.MinValue, display = "{REMOVED}" };
        }
        
        // Adjust profit
        float profitTimeAdjusted = (profit / recipe.ResultCount) * maxSellPerDay;
        
        readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitTimeAdjusted:F0}";

        if (latency == Market.Latency.Immediate && Market.IsSelling(result.Key, People))
        {
            readable = readable.Replace("\n", "    \n");
        }

        return new Util.Twopass.Result() { value = profitTimeAdjusted, display = readable };
    }

    public static void DoRecipeAnalysis(CraftingInfo[] craftingInfo, SortMethod sortMethod, bool includeSolo, bool includeBulk)
    {
        var evaluators = new List<Util.Twopass.Input>();

        foreach (var recipe in Db.GetSheet<SaintCoinach.Xiv.Recipe>())
        {
            var result = recipe.ResultItem;
            int resultId = result.Key;
            
            if (resultId == 0)
            {
                continue;
            }

            if (!result.IsMarketable())
            {
                continue;
            }
            
            // filter out ixal
            if (recipe.RequiredItem.Key != 0)
            {
                continue;
            }

            string className = recipe.ClassJob.Name;
            int classLevel = recipe.RecipeLevelTable.ClassJobLevel;

            bool canHq = false;

            bool canQuickSynth = false;
            {
                // validate availability and see if we're allowed to try HQ'ing it
                bool validated = false;
                foreach (var crafttype in craftingInfo)
                {
                    if (recipe.ClassJob.Name == crafttype.name && classLevel >= crafttype.minlevel && classLevel <= crafttype.maxlevel
                        && (crafttype.craftsmanship == 0 || recipe.RequiredCraftsmanship <= crafttype.craftsmanship)
                        && (crafttype.control == 0 || recipe.RequiredControl <= crafttype.control)
                    )
                    {
                        validated = true;
                        if (classLevel <= crafttype.maxhqlevel)
                        {
                            canHq = true;
                        }
                        if (recipe.QuickSynthCraftsmanship <= crafttype.craftsmanship)
                        {
                            canQuickSynth = true;
                        }
                    }
                }

                if (!validated)
                {
                    continue;
                }
            }

            if (canHq && result.CanBeHq)
            {
                evaluators.Add(new Util.Twopass.Input() { evaluator = immediate => EvaluateItem(recipe, true, false, immediate ? Market.Latency.Immediate : Market.Latency.Standard, includeSolo, includeBulk), unique = result });
            }

            evaluators.Add(new Util.Twopass.Input() { evaluator = immediate => EvaluateItem(recipe, false, canQuickSynth, immediate ? Market.Latency.Immediate : Market.Latency.Standard, includeSolo, includeBulk), unique = result });
        }

        if (sortMethod == SortMethod.Order)
        {
            // ToArray forces it to be evaluated before printing so we don't interlace with debug output
            foreach (var output in evaluators.ProgressBar().Select(item => item.evaluator(false).display).ToArray())
            {
                Dbg.Inf(output);
            }
        }
        else if (sortMethod == SortMethod.Profit)
        {
            Util.Twopass.Process(evaluators, 20);
        }
    }

    public static void DoCollectibleCombinationMath(CollectibleCombination[] combos)
    {
        foreach (var combo in combos)
        {
            var recipe = Db.Recipe(Db.Item(combo.item));

            string readable = $"{combo.item}:";
            float tcost = 0;
            foreach (var ingredient in recipe.Ingredients)
            {
                float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, Market.Latency.Standard, out string source);
                readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

                tcost += ingredient.Count * cost;
            }
            readable += "\n" + $"  Total cost {tcost}, total xp per gil: {combo.xp / tcost:F2}";

            Dbg.Inf(readable);
        }
    }
}
