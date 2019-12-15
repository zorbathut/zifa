
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

                float gps = Commerce.MarketProfitAdjuster(Commerce.ValueSell(item, false, Market.Latency.Standard) / scripEntry.GCSealsCost, item, false, 40000 / scripEntry.GCSealsCost, Market.Latency.Standard);

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
            var bracket = prices.BracketForRange(0, crafts * countForEach);

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

            string result = $"buy from {source} for {coststring} x{crafts * countForEach}";

            // how much extra we're willing to buy before we start complaining
            const float bracketExtensionFactor = 4;

            if (bracket.incrementalPrice < bracket.fullStackPrice / bracketExtensionFactor)
            {
                result += $" (full stack warning x{bracket.fullStackPrice / bracket.incrementalPrice:F2})";
            }

            return result;
        }
    }

    public struct EvaluationSettings
    {
        // this is the only part that really has to be set
        public Market.Latency latency;

        // evaluation setup
        public bool forGc;
        public bool ignoreIngredients;

        // allowable abilities
        public bool allowReuse;
        public bool canQuickSynth;

        // allowable recipe types
        public bool disallowSolo;
        public bool disallowBulk;
    }
    public static Util.Multipass.Result EvaluateItem(SaintCoinach.Xiv.Recipe recipe, bool hq, EvaluationSettings settings)
    {
        const float expectedProfitMargin = 1.5f;

        if (hq)
        {
            settings.canQuickSynth = false;
        }

        var result = recipe.ResultItem;
        float expectedRevenue = Commerce.ValueSell(result, hq, settings.latency) * recipe.ResultCount;
        
        // Build our ingredient lists
        IngredientData[] ingredients;

        if (!settings.ignoreIngredients)
        {
            ingredients = recipe.Ingredients.Select(ingredient => new IngredientData() { item = ingredient.Item, countForEach = ingredient.Count, prices = Market.Prices(ingredient.Item, settings.latency) }).ToArray();
        }
        else
        {
            ingredients = new IngredientData[0];
            settings.allowReuse = false;    // otherwise it explodes messily
        }

        int toSell = 0;
        float totalCost = 0;
        float effortBaseCost = settings.forGc ? 50000 : 0;
        float maxSellPerDay;
        {
            if (settings.forGc)
            {
                // "sell"
                if (settings.canQuickSynth)
                {
                    maxSellPerDay = 100;
                }
                else
                {
                    maxSellPerDay = 10;
                }
            }
            else
            {
                // Can't bulk-produce HQ, unfortunately
                bool allowBulkProduction = !settings.disallowBulk && settings.canQuickSynth && !hq;

                // This is the number of recipe productions that we're allowed to sell per day
                maxSellPerDay = Math.Min(Math.Min(Commerce.MarketSalesPerDay(result, hq, settings.latency), Commerce.MarketExpectedStackSale(result, settings.latency)), Math.Min(result.StackSize, 99)) / recipe.ResultCount;
                if (!allowBulkProduction)
                {
                    maxSellPerDay = Math.Min(maxSellPerDay, 1);
                }
            }

            if (settings.disallowSolo && maxSellPerDay <= 1)
            {
                return new Util.Multipass.Result() { value = float.MinValue, display = "{REMOVED}" };
            }

            if (maxSellPerDay > 1)
            {
                settings.allowReuse = false;
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
                    // we actually have no more items here
                    break;
                }

                bool accept = false;

                if (toSell == 0)
                {
                    // we need at least one!
                    accept = true;
                }
                else if (!settings.forGc && itemCost * expectedProfitMargin < expectedRevenue)
                {
                    accept = true;
                }
                else if (settings.forGc)
                {
                    // mathematically, these should both be multiplied by (result as SaintCoinach.Xiv.Items.Equipment).ExpertDeliverySeals
                    // but obviously that doesn't change the outcome of the equation
                    float costpergcpre = (totalCost + effortBaseCost) / toSell;
                    float costpergcpost = (totalCost + effortBaseCost + itemCost) / (toSell + 1);

                    if (costpergcpre >= costpergcpost)
                    {
                        accept = true;
                    }
                }

                if (accept)
                {
                    totalCost += itemCost;
                    toSell++;
                }
                else
                {
                    // not authorized to continue, so we end
                    break;
                }
            }
        }

        string readable = $"\n{recipe.ClassJob.Name}({recipe.RecipeLevelTable.ClassJobLevel}) {recipe.ResultItem.Name} {(hq ? "HQ" : "NQ")} x{toSell} ({recipe.ResultItem.Key}): expected revenue {expectedRevenue * toSell:F0}, {expectedRevenue / recipe.ResultCount:F0}/ea";

        if (toSell == 0)
        {
            // let's not and say we didn't
            return new Util.Multipass.Result() { value = 0, display = readable };
        }

        // if we have any ingredients, we must have crystals, and prefix it with crystal shorthand
        if (ingredients.Length > 0)
        {
            readable += "\n  " + string.Join(", ", ingredients.Where(ing => ing.item.IsCrystal()).Select(ing => $"{ing.item.Name} x{ing.countForEach}"));
        }

        foreach (var ingredient in ingredients)
        {
            if (ingredient.item.IsCrystal())
            {
                continue;
            }

            readable += "\n" + $"  {ingredient.item.Name}: {ingredient.GetSourceString(toSell)}";

            // Strip out 30% of an instance of the first ingredient; base it on the cheapest one because we still have to buy the most expensive
            if (settings.allowReuse)
            {
                totalCost -= 0.3f * ingredients[0].prices.PriceForQuantity(1);
                readable += " (REUSE)";
                settings.allowReuse = false;
            }
        }

        float value;
        if (!settings.forGc)
        {
            float profit = expectedRevenue * toSell - totalCost;

            readable += "\n" + $"  Total cost: {totalCost:F0}, total profit {profit:F0}";

            float adjustedProfit = profit;
            if (adjustedProfit > 0 && maxSellPerDay < toSell)
            {
                // scale down in case we have less sales than we're making
                adjustedProfit = adjustedProfit / toSell * maxSellPerDay;
                readable += $", TA profit {adjustedProfit:F0}";
            }

            if (adjustedProfit > 0 && totalCost > adjustedProfit)
            {
                float riskiness = totalCost / adjustedProfit;
                adjustedProfit /= riskiness;

                readable += $", TRA profit {adjustedProfit:F0}";
            }

            value = adjustedProfit;
        }
        else
        {
            int seals = (result as SaintCoinach.Xiv.Items.Equipment).ExpertDeliverySeals;
            float baseCostPerItem = totalCost / toSell;
            float baseValue = seals / baseCostPerItem;

            float adjCostPerItem = (effortBaseCost + totalCost) / toSell;
            float adjValue = seals / adjCostPerItem;

            readable += "\n" + $"  Cost per item: {baseCostPerItem:F0}, seals per item {seals:F0}, gil/venture {1 / baseValue * 200:F0} (with effort {1 / adjValue * 200:F0})";

            value = adjValue;
        }

        if (settings.latency == Market.Latency.Immediate && Market.IsSelling(result))
        {
            readable = readable.Replace("\n", "\n    ");
        }

        return new Util.Multipass.Result() { value = value, display = readable };
    }

    public enum EvaluationMode
    {
        HistoryPrepass,
        Ingredientless,
        Cached,
        Immediate,
    }
    public static void DoRecipeAnalysis(CraftingInfo[] craftingInfo, SortMethod sortMethod, bool includeSolo, bool includeBulk)
    {
        var evaluators = new List<Util.Multipass.Input<EvaluationMode>>();

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
            bool canReuse = false;
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
                        if (recipe.CanQuickSynth && recipe.QuickSynthCraftsmanship <= crafttype.craftsmanship && recipe.QuickSynthControl <= crafttype.control && recipe.RecipeLevelTable.SuggestedCraftsmanship <= crafttype.craftsmanship)
                        {
                            canQuickSynth = true;
                        }
                        if (crafttype.maxlevel >= 74)
                        {
                            canReuse = true;
                        }
                    }
                }

                if (!validated)
                {
                    continue;
                }
            }

            Util.Multipass.Result ProcessWorker(EvaluationMode mode, bool hq)
            {
                var latency = Market.Latency.Standard;
                if (mode == EvaluationMode.HistoryPrepass)
                {
                    latency = Market.Latency.CacheOnly;
                }
                else if (mode == EvaluationMode.Immediate)
                {
                    latency = Market.Latency.Immediate;
                }

                var evaluationSettings = new EvaluationSettings()
                {
                    latency = latency,

                    forGc = sortMethod == SortMethod.Gc,
                    ignoreIngredients = mode <= EvaluationMode.Ingredientless,

                    allowReuse = canReuse,
                    canQuickSynth = canQuickSynth,

                    disallowSolo = !includeSolo,
                    disallowBulk = !includeBulk,
                };

                var processResult = EvaluateItem(recipe, hq, evaluationSettings);

                if (mode == EvaluationMode.HistoryPrepass)
                {
                    Market.History(result, Market.Latency.CacheOnly, out var retrievalTime);

                    var cacheRefreshTime = Market.GetCacheRefreshTime(result, Market.Latency.Standard);

                    var cacheAge = DateTimeOffset.Now - retrievalTime;

                    // multiply it by 2^(how many cache multiples we're old)
                    float factor = (float)(cacheAge.TotalDays / cacheRefreshTime.TotalDays - 1);

                    if (factor > 0)
                    {
                        processResult.value *= (float)Math.Pow(2, factor);
                        processResult.display += $"\n  Cache invalidation logfactor {factor:F2}";
                    }
                }

                return processResult;
            }

            if (canHq && result.CanBeHq && sortMethod != SortMethod.Gc)
            {
                evaluators.Add(new Util.Multipass.Input<EvaluationMode>() { evaluator = mode => ProcessWorker(mode, true), unique = result });
            }

            evaluators.Add(new Util.Multipass.Input<EvaluationMode>() { evaluator = mode => ProcessWorker(mode, false), unique = result });
        }

        if (sortMethod == SortMethod.Order)
        {
            // ToArray forces it to be evaluated before printing so we don't interlace with debug output
            foreach (var output in evaluators.ProgressBar().Select(item => item.evaluator(EvaluationMode.Cached).display).ToArray())
            {
                Dbg.Inf(output);
            }
        }
        else if (sortMethod == SortMethod.Profit || sortMethod == SortMethod.Gc)
        {
            Util.Multipass.Process(evaluators, new EvaluationMode[] { EvaluationMode.HistoryPrepass, EvaluationMode.Ingredientless, EvaluationMode.Cached, EvaluationMode.Immediate }, 20);
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
