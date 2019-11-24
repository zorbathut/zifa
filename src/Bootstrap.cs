
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
        Gc,
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
        var inspected = new HashSet<SaintCoinach.Xiv.Item>();
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

            if (!inspected.Contains(item))
            {
                inspected.Add(item);

                float gps = Commerce.MarketProfitAdjuster(Commerce.ValueSell(item, false, Market.Latency.Standard) / scripEntry.GCSealsCost, item, 40000 / scripEntry.GCSealsCost, Market.Latency.Standard);

                results.Add(new Result() { gps = gps, name = item.Name });
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    private struct IngredientData
    {
        public SaintCoinach.Xiv.Item item;
        public int countForEach;
        public Market.Pricing prices;

        public float GetCostForCraft(int crafts)
        {
            return prices.PriceForRange(crafts * countForEach, (crafts + 1) * countForEach);
        }

        public string GetSourceString(int crafts)
        {
            prices.PriceForRange(0, crafts * countForEach, out var bracket);

            string source;
            if (bracket.containsMarket && bracket.containsVendor)
            {
                source = "market+vendor";
            }
            else if (bracket.containsMarket)
            {
                source = "market";
            }
            else if (bracket.containsVendor)
            {
                source = "vendor";
            }
            else
            {
                source = "wtf";
            }

            string coststring;
            if (bracket.totalMin == bracket.totalMax)
            {
                coststring = bracket.totalMin.ToString("F0");
            }
            else
            {
                coststring= $"{bracket.totalMin:F0}-{bracket.totalMax:F0}";
            }

            return $"buy from {source} for {coststring} x{crafts * countForEach}";
        }
    }

    public static Util.Twopass.Result EvaluateItem(SaintCoinach.Xiv.Recipe recipe, bool hq, bool canQuickSynth, Market.Latency latency, bool includeSolo, bool includeBulk, SortMethod sortMethod)
    {
        const float expectedProfitMargin = 1.5f;

        var result = recipe.ResultItem;
        float expectedRevenue = Commerce.ValueSell(result, hq, latency) * recipe.ResultCount;
        
        // Build our ingredient lists
        var ingredients = recipe.Ingredients.Select(ingredient => new IngredientData() { item = ingredient.Item, countForEach = ingredient.Count, prices = Market.Prices(ingredient.Item, latency) }).ToArray();

        int toSell = 0;
        float totalCost = 0;
        float maxSellPerDay;
        {
            if (sortMethod == SortMethod.Gc)
            {
                // We're presumably going to be making a bunch, so make sure it's worth our time
                maxSellPerDay = 10;
            }
            else
            {
                // Can't bulk-produce HQ, unfortunately
                bool allowBulkProduction = includeBulk && canQuickSynth && !hq;

                // This is the amount that we're allowed to sell per day
                maxSellPerDay = Math.Min(Math.Min(Commerce.MarketSalesPerDay(result, latency), Commerce.MarketExpectedStackSale(result, latency)), Math.Min(result.StackSize, 99)) / recipe.ResultCount;
                if (!allowBulkProduction)
                {
                    maxSellPerDay = Math.Min(maxSellPerDay, 1);
                }

                if (!includeSolo && maxSellPerDay <= 1)
                {
                    return new Util.Twopass.Result() { value = float.MinValue, display = "{REMOVED}" };
                }
            }

            for (int i = 0; i < (int)Math.Ceiling(maxSellPerDay); ++i)
            {
                float itemCost = 0;
                foreach (var ingredient in ingredients)
                {
                    itemCost += ingredient.GetCostForCraft(i);
                }

                if (float.IsNaN(itemCost))
                {
                    // we actually have no items here
                    break;
                }
                else if (toSell == 0 || sortMethod == SortMethod.Gc || itemCost * expectedProfitMargin < expectedRevenue)
                {
                    totalCost += itemCost;
                    toSell++;
                }
                else
                {
                    break;
                }
            }
        }

        string readable = $"\n{recipe.ClassJob.Name}({recipe.RecipeLevelTable.ClassJobLevel}) {recipe.ResultItem.Name} {(hq ? "HQ" : "NQ")} x{toSell} ({recipe.ResultItem.Key}): expected revenue {expectedRevenue * toSell:F0}, {expectedRevenue / recipe.ResultCount:F0}/ea";

        foreach (var ingredient in ingredients)
        {
            readable += "\n" + $"  {ingredient.item.Name}: {ingredient.GetSourceString(toSell)}";
        }

        float value;
        if (sortMethod == SortMethod.Order || sortMethod == SortMethod.Profit)
        {
            float profit = expectedRevenue * toSell - totalCost;
            float adjustedProfit = toSell == 0 ? 0 : ( profit / toSell * maxSellPerDay );

            readable += "\n" + $"  Total cost: {totalCost:F0}, total profit {profit:F0}, adjusted profit {adjustedProfit:F0}";

            if (totalCost * 1f > adjustedProfit)
            {
                readable += "  == RISKY ==";
            }

            value = adjustedProfit;
        }
        else if (sortMethod == SortMethod.Gc)
        {
            int seals = (result as SaintCoinach.Xiv.Items.Equipment).ExpertDeliverySeals;
            float costPerItem = totalCost / toSell;
            value = seals / costPerItem;

            readable += "\n" + $"  Cost per item: {costPerItem:F0}, seals per item {seals:F0}, gil/venture {1 / value * 200:F0}";
        }
        else
        {
            Dbg.Err("Invalid sort method?");
            value = 0;
        }
        

        if (latency == Market.Latency.Immediate && Market.IsSelling(result))
        {
            readable = readable.Replace("\n", "\n    ");
        }

        return new Util.Twopass.Result() { value = value, display = readable };
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

            // if we're trying to make things for GC seals, strip out anything that can't be turned in for seals
            if (sortMethod == SortMethod.Gc)
            {
                if (!(result is SaintCoinach.Xiv.Items.Equipment))
                {
                    continue;
                }

                if ((result as SaintCoinach.Xiv.Items.Equipment).ExpertDeliverySeals == 0)
                {
                    continue;
                }
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

            if (canHq && result.CanBeHq && sortMethod != SortMethod.Gc)
            {
                evaluators.Add(new Util.Twopass.Input() { evaluator = immediate => EvaluateItem(recipe, true, false, immediate ? Market.Latency.Immediate : Market.Latency.Standard, includeSolo, includeBulk, sortMethod), unique = result });
            }

            evaluators.Add(new Util.Twopass.Input() { evaluator = immediate => EvaluateItem(recipe, false, canQuickSynth, immediate ? Market.Latency.Immediate : Market.Latency.Standard, includeSolo, includeBulk, sortMethod), unique = result });
        }

        if (sortMethod == SortMethod.Order)
        {
            // ToArray forces it to be evaluated before printing so we don't interlace with debug output
            foreach (var output in evaluators.ProgressBar().Select(item => item.evaluator(false).display).ToArray())
            {
                Dbg.Inf(output);
            }
        }
        else if (sortMethod == SortMethod.Profit || sortMethod == SortMethod.Gc)
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
                float cost = Commerce.ValueBuy(ingredient.Item, false, Commerce.TransactionType.Immediate, Market.Latency.Standard, out string source);
                readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

                tcost += ingredient.Count * cost;
            }
            readable += "\n" + $"  Total cost {tcost}, total xp per gil: {combo.xp / tcost:F2}";

            Dbg.Inf(readable);
        }
    }
}
