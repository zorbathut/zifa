
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Prompt
{
    private static Regex PointRegex = new Regex("^gpoint( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex GatherRegex = new Regex("^gatherbest (?<gathermin>[0-9]+) (?<gathermax>[0-9]+) (?<minemin>[0-9]+) (?<minemax>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex ValueRegex = new Regex("^vendornet (?<amount>[0-9]+)( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex AcquireRegex = new Regex("^acquirenet( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex AnalyzeRegex = new Regex("^analyze( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex RewardsRegex = new Regex("^rewards( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex GatherCalcRegex = new Regex("^gathercalc (?<lchance>[0-9]+) (?<hqchance>[0-9]+) (?<maxgp>[0-9]+) (?<attempts>[0-9]+) (?<hqonly>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex CofferRegex = new Regex("^coffer (?<ilevel>[0-9]+) (?<slot>[^ ]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex Overmeld = new Regex("^overmeld (?<slots>[0-9]+) (?<cp>[0-9]+) (?<crafts>[0-9]+) (?<control>[0-9]+)( (?<craftsval>[0-9]+) (?<controlval>[0-9]+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    private static Regex RetainerGatherRegex = new Regex("^retainergather (?<role>(dow|btn|min|fsh)) (?<level>[0-9]+) (?<skill>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    private static Regex RecipeAnalysisCache = new Regex("^recipeanalysiscache (?<solo>(true|false)) (?<bulk>(true|false))$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex RecipeAnalysisSegment = new Regex("^recipeanalysissegment (?<solo>(true|false)) (?<bulk>(true|false)) (?<level>[0-9]+) (?<role>[a-zA-Z]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex RecipeAnalysisLevel = new Regex("^recipeanalysislevel (?<solo>(true|false)) (?<bulk>(true|false)) (?<level>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex RecipeAnalysisMax = new Regex("^recipeanalysismax (?<solo>(true|false)) (?<bulk>(true|false))$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    private static Regex SourceAddRegex = new Regex("^sourceadd (?<count>[0-9]+)( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex SourceRemoveRegex = new Regex("^sourceremove (?<count>[0-9]+)( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex SourceCraftRegex = new Regex("^sourcecraft (?<count>[0-9]+)( (?<token>[^ ]+))+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex SourceCraftRangeRegex = new Regex("^sourcecraftrange (?<role>[a-zA-Z]+) (?<levelmin>[0-9]+) (?<levelmax>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static void Run()
    {
        Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
            if (!Interrupt.ConsumeInterrupt())
            {
                e.Cancel = true;
                Interrupt.QueueInterrupt();
            }
        };

        while (true)
        {
            try
            {
                Dbg.Inf("");
                Dbg.Inf("");
                Dbg.Inf("Options:");
                Dbg.Inf("  Core commands:");
                Dbg.Inf("    gpoint wind coin - finds the most profitable item to acquire at a gathering point, given some items names");
                Dbg.Inf("    gatherbest (botmin) (botmax) (minemin) (minemax) - finds the best items to gather, given botanist and miner ranges");
                Dbg.Inf("    vendornet 2000 tomestone poetic - finds the best way to turn an item into money, given a quantity of that item and the item's name");
                Dbg.Inf("    acquirenet rakshasa token - finds the best way to acquire an item, given the item name");
                Dbg.Inf("    analyze craftsman vi - dumps various crafting and market info on an item");
                Dbg.Inf("    rewards nickel turban high steel fending - chooses the best quest reward, given some items names");
                Dbg.Inf("    vendormarket - finds the best items to be purchased from vendors and marketed");
                Dbg.Inf("    gathercalc (lchance) (hqchance) (maxgp) (attempts) (hqonly) - calculates the best way to gather items given current stats");
                Dbg.Inf("    retainergather {dow/btn/min/fsh} {skill} - calculates the best items for retainers to gather");
                Dbg.Inf("    coffer {ilevel} {slot} - calculates the value of results from adaptive coffers");
                Dbg.Inf("    overmeld (slots) (cp) (crafts) (control) [(craftsval) (controlval)] - minmaxes crafting overmeld values");
                Dbg.Inf("    craftingfood - does stuff to evaluate crafting food? I dunno man, this one isn't really planned out");
                Dbg.Inf("    customdelivery - evaluates the value of crafting vs. gathering custom deliveries");
                Dbg.Inf("");
                Dbg.Inf("  Stat-based commands:");
                Dbg.Inf("    retainergathercache - does various retainergather queries that I've predefined to follow my own characters");
                Dbg.Inf("    retainergathermax - does various retainergather queries that assume godlike retainers of infinite power");
                Dbg.Inf("    recipeanalysiscache (solo) (bulk) - does various recipe analysis queries that I've predefined to follow my own characters");
                Dbg.Inf("    recipeanalysiscachegc - calculates GC seals per gil from crafting");
                Dbg.Inf("    recipeanalysissegment (solo) (bulk) (level) (role) - does various recipe analysis queries assuming you can craft everything up to a given level in a single profession");
                Dbg.Inf("    recipeanalysislevel (solo) (bulk) (level) - does various recipe analysis queries assuming you can craft everything up to a given level");
                Dbg.Inf("    recipeanalysismax (solo) (bulk) - does various recipe analysis queries that assume godlike crafters of infinite power");
                Dbg.Inf("");
                Dbg.Inf("  Sourcing commands:");
                Dbg.Inf("    sourcereset - clears the sourcing db");
                Dbg.Inf("    sourceadd {count} {itemdescr} - adds an item to the sourcing list");
                Dbg.Inf("    sourceremove {count} {itemdescr} - removes an item from the sourcing list");
                Dbg.Inf("    sourcecraft {count} {itemdescr} - adds components to craft an item to the sourcing list");
                Dbg.Inf("    sourcecraftrange {crafter} {levelmin} {levelmax} - adds items based on a level range for crafters");
                Dbg.Inf("");
                Dbg.Inf("  Misc commands:");
                Dbg.Inf("    recachepoint - resets immediate recache timing");

                Dbg.Inf("");

                string instr = Console.ReadLine();
                if (PointRegex.Match(instr) is var pmatch && pmatch.Success)
                {
                    GatherpointCalculator(pmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                }
                else if (GatherRegex.Match(instr) is var gmatch && gmatch.Success)
                {
                    GatherbestCalculator(
                        int.Parse(gmatch.Groups["gathermin"].Value),
                        int.Parse(gmatch.Groups["gathermax"].Value),
                        int.Parse(gmatch.Groups["minemin"].Value),
                        int.Parse(gmatch.Groups["minemax"].Value));
                }
                else if (ValueRegex.Match(instr) is var vmatch && vmatch.Success)
                {
                    int amount = int.Parse(vmatch.Groups["amount"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).First());
                    var item = Db.ItemLooseSingle(vmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                    if (item != null)
                    {
                        DoPurchasableAnalysis(item.Key, amount);
                    }
                }
                else if (AcquireRegex.Match(instr) is var qmatch && qmatch.Success)
                {
                    var item = Db.ItemLooseSingle(qmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                    if (item != null)
                    {
                        DoAcquireableAnalysis(item.Key, 1);
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
                    int level = int.Parse(rgmatch.Groups["level"].Value);
                    int skill = int.Parse(rgmatch.Groups["skill"].Value);

                    DoRetainerGatherAnalysis(role, level, skill);
                }
                else if (instr == "sourcereset")
                {
                    Sourced.Clear();
                    Dbg.Inf("(☞ﾟヮﾟ)☞");
                }
                else if (SourceAddRegex.Match(instr) is var samatch && samatch.Success)
                {
                    var item = Db.ItemLooseSingle(samatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                    if (item != null)
                    {
                        bool sourceNotEmpty = Sourced.Count != 0;

                        if (!Sourced.ContainsKey(item))
                        {
                            Sourced[item] = 0;
                        }

                        Sourced[item] = Sourced[item] + int.Parse(samatch.Groups["count"].Value);

                        SourceDoAnalysis();
                        if (sourceNotEmpty)
                        {
                            Dbg.Wrn("Source was not empty at the beginning!");
                        }
                    }
                }
                else if (SourceRemoveRegex.Match(instr) is var srmatch && srmatch.Success)
                {
                    var item = Db.ItemLooseSingle(srmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                    if (item != null)
                    {
                        bool sourceNotEmpty = Sourced.Count != 0;

                        if (!Sourced.ContainsKey(item))
                        {
                            Sourced[item] = 0;
                        }

                        Sourced[item] = Sourced[item] - int.Parse(srmatch.Groups["count"].Value);

                        SourceDoAnalysis();
                        if (sourceNotEmpty)
                        {
                            Dbg.Wrn("Source was not empty at the beginning!");
                        }
                    }
                }
                else if (SourceCraftRegex.Match(instr) is var scmatch && scmatch.Success)
                {
                    var item = Db.ItemLooseSingle(scmatch.Groups["token"].Captures.OfType<System.Text.RegularExpressions.Capture>().Select(cap => cap.Value).ToArray());
                    var recipes = Db.GetSheet<SaintCoinach.Xiv.Recipe>().Where(rec => rec.ResultItem == item).ToArray();
                    var recipe = recipes.FirstOrDefault();
                    if (recipes.Length != 1)
                    {
                        Dbg.Err("Could not find an isolated recipe");
                    }
                    if (item != null && recipes.Length == 1 && recipe != null)
                    {
                        bool sourceNotEmpty = Sourced.Count != 0;

                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (!Sourced.ContainsKey(ingredient.Item))
                            {
                                Sourced[ingredient.Item] = 0;
                            }

                            Sourced[ingredient.Item] = Sourced[ingredient.Item] + ingredient.Count;
                        }

                        SourceDoAnalysis();
                        if (sourceNotEmpty)
                        {
                            Dbg.Wrn("Source was not empty at the beginning!");
                        }
                    }
                }
                else if (SourceCraftRangeRegex.Match(instr) is var scrmatch && scrmatch.Success)
                {
                    string role = scrmatch.Groups["role"].Value;
                    int levelmin = int.Parse(scrmatch.Groups["levelmin"].Value);
                    int levelmax = int.Parse(scrmatch.Groups["levelmax"].Value);

                    bool sourceNotEmpty = Sourced.Count != 0;

                    SourceAddCraftElements(role, levelmin, levelmax);

                    SourceDoAnalysis();
                    if (sourceNotEmpty)
                    {
                        Dbg.Wrn("Source was not empty at the beginning!");
                    }
                }
                else if (CofferRegex.Match(instr) is var cmatch && cmatch.Success)
                {
                    CofferAnalyze(int.Parse(cmatch.Groups["ilevel"].Value), cmatch.Groups["slot"].Value);
                }
                else if (instr == "retainergathercache")
                {
                    // we do it twice just to get all the output dumped in one place after it's cached :V
                    for (int i = 0; i < 2; ++i)
                    {
                        Dbg.Inf("\n\n");
                        foreach (var retainer in ZifaConfigDefs.Global.retainers)
                        {
                            Dbg.Inf("vv   " + retainer.name);
                            DoRetainerGatherAnalysis(retainer.profession, retainer.level, retainer.skill);
                            Dbg.Inf("^^   " + retainer.name);
                        }
                    }
                }
                else if (instr == "retainergathermax")
                {
                    // we do it twice just to get all the output dumped in one place after it's cached :V
                    for (int i = 0; i < 2; ++i)
                    {
                        Dbg.Inf("\n\n");
                        DoRetainerGatherAnalysis("dow", int.MaxValue, int.MaxValue);
                        DoRetainerGatherAnalysis("min", int.MaxValue, int.MaxValue);
                        DoRetainerGatherAnalysis("btn", int.MaxValue, int.MaxValue);
                        DoRetainerGatherAnalysis("fsh", int.MaxValue, int.MaxValue);
                    }
                }
                else if (RecipeAnalysisCache.Match(instr) is var racmatch && racmatch.Success)
                {
                    Bootstrap.DoRecipeAnalysis(
                        ZifaConfigDefs.Global.professions.Select(
                            p => new Bootstrap.CraftingInfo { name = p.name, minlevel = 1, maxhqlevel = p.level, maxlevel = p.level, craftsmanship = p.craftsmanship, control = p.control }).ToArray(),
                        Bootstrap.SortMethod.Profit, bool.Parse(racmatch.Groups["solo"].Value), bool.Parse(racmatch.Groups["bulk"].Value));
                }
                else if (instr == "recipeanalysiscachegc")
                {
                    Bootstrap.DoRecipeAnalysis(
                        ZifaConfigDefs.Global.professions.Select(
                            p => new Bootstrap.CraftingInfo { name = p.name, minlevel = 1, maxhqlevel = p.level, maxlevel = p.level, craftsmanship = p.craftsmanship, control = p.control }).ToArray(),
                        Bootstrap.SortMethod.Gc, true, false);
                }
                else if (RecipeAnalysisSegment.Match(instr) is var rasmatch && rasmatch.Success)
                {
                    int level = int.Parse(rasmatch.Groups["level"].Value);
                    Bootstrap.DoRecipeAnalysis(new Bootstrap.CraftingInfo[] {
                        new Bootstrap.CraftingInfo() { name = rasmatch.Groups["role"].Value, minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                    }, Bootstrap.SortMethod.Profit, bool.Parse(rasmatch.Groups["solo"].Value), bool.Parse(rasmatch.Groups["bulk"].Value));
                }
                else if (RecipeAnalysisLevel.Match(instr) is var ralmatch && ralmatch.Success)
                {
                    int level = int.Parse(ralmatch.Groups["level"].Value);
                    Bootstrap.DoRecipeAnalysis(new Bootstrap.CraftingInfo[] {
                        new Bootstrap.CraftingInfo() { name = "carpenter", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "blacksmith", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "armorer", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "goldsmith", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "leatherworker", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "weaver", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "alchemist", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "culinarian", minlevel = 1, maxhqlevel = level, maxlevel = level, craftsmanship = int.MaxValue, control = int.MaxValue },
                    }, Bootstrap.SortMethod.Profit, bool.Parse(ralmatch.Groups["solo"].Value), bool.Parse(ralmatch.Groups["bulk"].Value));
                }
                else if (RecipeAnalysisMax.Match(instr) is var raxmatch && raxmatch.Success)
                {
                    Bootstrap.DoRecipeAnalysis(new Bootstrap.CraftingInfo[] {
                        new Bootstrap.CraftingInfo() { name = "carpenter", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "blacksmith", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "armorer", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "goldsmith", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "leatherworker", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "weaver", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "alchemist", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                        new Bootstrap.CraftingInfo() { name = "culinarian", minlevel = 1, maxhqlevel = int.MaxValue, maxlevel = int.MaxValue, craftsmanship = int.MaxValue, control = int.MaxValue },
                    }, Bootstrap.SortMethod.Profit, bool.Parse(raxmatch.Groups["solo"].Value), bool.Parse(raxmatch.Groups["bulk"].Value));
                }
                else if (Overmeld.Match(instr) is var omatch && omatch.Success)
                {
                    DoOvermeld(
                        int.Parse(omatch.Groups["slots"].Value),
                        int.Parse(omatch.Groups["cp"].Value),
                        int.Parse(omatch.Groups["crafts"].Value),
                        int.Parse(omatch.Groups["control"].Value),
                        omatch.Groups["craftsval"].Length > 0 ? int.Parse(omatch.Groups["craftsval"].Value) : 50000,
                        omatch.Groups["controlval"].Length > 0 ? int.Parse(omatch.Groups["controlval"].Value) : 200000);
                }
                else if (instr == "craftingfood")
                {
                    AnalyzeCraftingFood();
                }
                else if (instr == "recachepoint")
                {
                    Cache.SetImmediateRecachePoint();
                }
                else if (instr == "customdelivery")
                {
                    float yg = DoPurchasableAnalysis(Db.ItemLooseSingle(new string[] { "yellow", "gatherer", "scrip" }).Key, 2000);
                    float wg = DoPurchasableAnalysis(Db.ItemLooseSingle(new string[] { "white", "gatherer", "scrip" }).Key, 2000);

                    float yc = DoPurchasableAnalysis(Db.ItemLooseSingle(new string[] { "yellow", "crafter", "scrip" }).Key, 2000);
                    float wc = DoPurchasableAnalysis(Db.ItemLooseSingle(new string[] { "white", "crafter", "scrip" }).Key, 2000);

                    Dbg.Inf($"Gatherer delivery: {yg * 143 + wg * 93}");
                    Dbg.Inf($"Crafter delivery: {yc * 143 + wc * 93}");
                }
                else
                {
                    Dbg.Inf("nope nope nope");
                }
            }
            catch (Interrupt)
            {
                Dbg.Wrn("Interrupted!");
            }
        }
    }

    private struct PurchasableOption
    {
        public string name;
        public Func<bool, Util.Multipass.Result> evaluator;
    }
    public static float DoPurchasableAnalysis(int itemId, int amount)
    {
        var dedupOptions = new Dictionary<string, Util.Multipass.Input<bool>>();
        foreach (var item in PurchasableAnalysisWorker(itemId, amount))
        {
            if (!dedupOptions.ContainsKey(item.name))
            {
                dedupOptions[item.name] = new Util.Multipass.Input<bool>() { evaluator = item.evaluator };
            }
        }

        return Util.Multipass.Process(dedupOptions.Values.Select<Util.Multipass.Input<bool>, Util.Multipass.Input<bool>>(input => new Util.Multipass.Input<bool>() { evaluator = immediate =>
        {
            var result = input.evaluator(immediate);
            return new Util.Multipass.Result() { value = result.value, display = $"{result.value:F2}: {result.display}" };
        } }), new bool[] { false, true }, 10);
    }

    private static IEnumerable<PurchasableOption> PurchasableAnalysisWorker(int itemId, float amountAcquired)
    {
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
                    Dbg.Err("TOO MANY RESULTS");
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
                    yield return new PurchasableOption() { name = label, evaluator = immediate => {
                        float valueBase = Commerce.ValueSell(reward.Item, reward.IsHq, immediate ? Market.Latency.Immediate : Market.Latency.Standard) * reward.Count;
                        float valueAdjusted = Commerce.MarketProfitAdjuster(valueBase, reward.Item, reward.IsHq, amountAcquired / cost  * reward.Count, immediate ? Market.Latency.Immediate : Market.Latency.Standard);
                        return new Util.Multipass.Result() { value = valueAdjusted / cost, display = label };
                    }};
                }

                // Branch out if we can't sell it on the market; there might be more lucrative options!
                if (!reward.Item.IsMarketable())
                {
                    foreach (var elem in PurchasableAnalysisWorker(reward.Item.Key, amountAcquired / cost * reward.Count))
                    {
                        string nestedlabel = $"{label} -> {elem.name}";
                        yield return new PurchasableOption() { name = nestedlabel, evaluator = immediate => {
                            return new Util.Multipass.Result() { value = elem.evaluator(immediate).value / cost * reward.Count, display = nestedlabel };
                        }};
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
                    float value = Commerce.ValueBuy(cost.Item, cost.IsHq, Commerce.TransactionType.Immediate, Market.Latency.Standard) * cost.Count;
                    yield return new Bootstrap.Result() { gps = value / reward, name = label };
                }
            }
        }
    }

    public static void GatherpointCalculator(string[] tokens)
    {
        var prospective = new List<SaintCoinach.Xiv.GatheringPointBase>();
        var items = new HashSet<SaintCoinach.Xiv.Item>();
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
                        items.Add(gitem.Item as SaintCoinach.Xiv.Item);
                    }
                }
            }
        }

        Dbg.Inf($"{prospective.Count} matches");
        foreach (var item in items.Select(item => Tuple.Create(item, Commerce.MarketProfitAdjuster(Commerce.ValueSell(item, false, Market.Latency.Standard), item, false, 30, Market.Latency.Standard))).OrderByDescending(tup => tup.Item2))
        {
            Dbg.Inf($"  {item.Item1.Name}: {item.Item2:F0}");
        }
    }

    public static void GatherbestCalculator(int gathermin, int gathermax, int minemin, int minemax)
    {
        var transientSheet = Db.Realm.GameData.GetSheet("GatheringPointTransient");

        var prospective = new HashSet<SaintCoinach.Xiv.Item>();
        var unlimited = new HashSet<SaintCoinach.Xiv.Item>();
        foreach (var point in Db.GetSheet<SaintCoinach.Xiv.GatheringPoint>())
        {
            if (point.TerritoryType == null || point.TerritoryType.Name == "")
            {
                continue;
            }

            if (point.PlaceName == null || point.PlaceName.Name == "")
            {
                continue;
            }

            var pbase = point.Base;
            string pointtype = pbase.Type.Name;
            bool valid = false;
            if ((pointtype == "Mining" || pointtype == "Quarrying") && pbase.GatheringLevel >= minemin && pbase.GatheringLevel <= minemax) valid = true;
            if ((pointtype == "Harvesting" || pointtype == "Logging") && pbase.GatheringLevel >= gathermin && pbase.GatheringLevel <= gathermax) valid = true;

            if (!valid)
            {
                continue;
            }

            var transient = transientSheet[point.Key];

            foreach (var gitem in pbase.Items)
            {
                if (gitem.Item is SaintCoinach.Xiv.Item item)
                {
                    if (item.Key == 0 || item.IsUntradable)
                    {
                        continue;
                    }

                    prospective.Add(item);

                    if (Convert.ToUInt32(transient.GetRaw(2)) == 0)
                    {
                        unlimited.Add(item);
                    }
                }
            }
        }

        Util.Multipass.Input<bool> function(SaintCoinach.Xiv.Item item) => new Util.Multipass.Input<bool>() { evaluator = immediate =>
        {
            bool lim = !unlimited.Contains(item);
            var latency = immediate ? Market.Latency.Immediate : Market.Latency.Standard;
            float value = Commerce.MarketProfitAdjuster(Commerce.ValueSell(item, false, latency), item, false, lim ? 10 : 99, latency);
            string badge = lim ? "LIM" : "   ";
            return new Util.Multipass.Result() { value = value, display = $"{badge} {value}: {item.Name}" };
        } };

        Util.Multipass.Process(prospective.Where(item => unlimited.Contains(item)).Select(function), new bool[] { false, true }, 20);
        Util.Multipass.Process(prospective.Select(function), new bool[] { false, true }, 20);
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
            Dbg.Inf($"    Immediate: {Commerce.ValueMarket(item, false, Commerce.TransactionType.Immediate, Market.Latency.Immediate)}");
            Dbg.Inf($"    Immediate HQ: {Commerce.ValueMarket(item, true, Commerce.TransactionType.Immediate, Market.Latency.Immediate)}");
            Dbg.Inf($"    Longterm: {Commerce.ValueMarket(item, false, Commerce.TransactionType.Longterm, Market.Latency.Immediate)}");
            Dbg.Inf($"    Fastsell: {Commerce.ValueMarket(item, false, Commerce.TransactionType.Fastsell, Market.Latency.Immediate)}");
            Dbg.Inf($"    Fastsell HQ: {Commerce.ValueMarket(item, true, Commerce.TransactionType.Fastsell, Market.Latency.Immediate)}");
            Dbg.Inf($"    Sales per day NQ: {Commerce.MarketSalesPerDay(item, false, Market.Latency.Immediate)}");
            Dbg.Inf($"    Sales per day HQ: {Commerce.MarketSalesPerDay(item, true, Market.Latency.Immediate)}");
            Dbg.Inf($"    Expected stack sale: {Commerce.MarketExpectedStackSale(item, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (1, NQ): {Commerce.MarketProfitAdjuster(1, item, false, 1, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (10, NQ): {Commerce.MarketProfitAdjuster(1, item, false, 10, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (99, NQ): {Commerce.MarketProfitAdjuster(1, item, false, 99, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (1, HQ): {Commerce.MarketProfitAdjuster(1, item, true, 1, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (10, HQ): {Commerce.MarketProfitAdjuster(1, item, true, 10, Market.Latency.Immediate)}");
            Dbg.Inf($"    Profit adjustment (99, HQ): {Commerce.MarketProfitAdjuster(1, item, true, 99, Market.Latency.Immediate)}");
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

                    var evaluationSettings = new Bootstrap.EvaluationSettings()
                    {
                        latency = Market.Latency.Immediate,

                        forGc = false,
                        ignoreIngredients = false,

                        allowReuse = true,
                        canQuickSynth = true,

                        disallowSolo = false,
                        disallowBulk = false,
                    };

                    Dbg.Inf("  " + Bootstrap.EvaluateItem(recipe, item.CanBeHq, evaluationSettings).display.Replace("\n", "\n  "));
                    Dbg.Inf("");
                }
            }

            if (Commerce.SellersForItem(item).Any())
            {
                Dbg.Inf($"  Vendors ({item.Ask}g):");
                foreach (var seller in Commerce.SellersForItem(item))
                {
                    Dbg.Inf($"    {seller.ToZifaString()}");
                }
                Dbg.Inf("");
            }

            if (item.IsMarketable())
            {
                Dbg.Inf("  Pricing:");
                foreach (var market in Market.Prices(item, Market.Latency.Immediate).Entries)
                {
                    Dbg.Inf($"    {market.sellPrice}: {market.stack}x {(market.hq ? "HQ" : "")}");
                }

                Dbg.Inf("");
                Dbg.Inf("  History:");
                foreach (var market in Market.History(item, Market.Latency.Immediate).history)
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
                            if (!rewards.Any(rhs => rhs.Item == reward.Item && rhs.Counts[0] == reward.Counts[0]))
                            {
                                rewards.Add(reward);
                            }
                        }
                    }
                }
            }
        }

        Dbg.Inf($"{prospective.Count} matches");
        DoItemsetComparison(rewards.Select(reward => new ItemsetOption() { item = reward.Item, hq = reward.IsHq, count = reward.Counts[0] }));
    }

    private struct ItemsetOption
    {
        public SaintCoinach.Xiv.Item item;
        public int count;
        public bool hq;
    }
    private static void DoItemsetComparison(IEnumerable<ItemsetOption> items)
    {
        foreach (var item in items.ProgressBar().Select(reward => Tuple.Create(reward, Commerce.MarketProfitAdjuster(Commerce.ValueSell(reward.item, reward.hq, Market.Latency.Standard), reward.item, reward.hq, reward.count, Market.Latency.Standard) * reward.count)).OrderByDescending(tup => tup.Item2))
        {
            Dbg.Inf($"  {item.Item1.item.Name}{(item.Item1.hq ? " HQ" : "")}{(item.Item1.count > 1 ? $" x{item.Item1.count}" : "")}: {item.Item2:F0}");
        }
    }

    struct MarketInfo
    {
        public float profit;
        public string text;
    }
    public static void DoVendorMarketAnalysis()
    {
        var marketables = Commerce.Marketables().ToArray();
        var results = new List<MarketInfo>();

        for (int i = 0; i < marketables.Length; ++i)
        {
            Dbg.Inf($"{i}/{marketables.Length}");

            var item = marketables[i];
            if (!item.IsMarketable())
            {
                continue;
            }

            int stack = Math.Min(item.StackSize, 99);
            float profit = Commerce.MarketProfitAdjuster(Commerce.ValueMarket(item, false, Commerce.TransactionType.Fastsell, Market.Latency.Standard) - item.Ask, item, false, stack, Market.Latency.Standard);

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
    public static void DoRetainerGatherAnalysis(string role, int level, int skill)
    {
        GatherType gatherer = GatherType.Gatherer;

        if (role == "min") role = "miner";
        if (role == "btn") role = "botanist";
        if (role == "dow") { role = "rogue"; gatherer = GatherType.Warrior; } // close enough
        if (role == "fsh") { role = "fisher"; gatherer = GatherType.Fisher; }

        IEnumerable<SaintCoinach.Xiv.RetainerTask> tasks = Db.GetSheet<SaintCoinach.Xiv.RetainerTask>();
        tasks = tasks.Where(task =>
        {
            if (!task.ClassJobCategory.ClassJobs.Any(cj => cj.Name == role))
            {
                return false;
            }

            if (gatherer == GatherType.Gatherer && task.RequiredGathering > skill)
            {
                return false;
            }

            if (gatherer == GatherType.Warrior && task.RequiredItemLevel > skill)
            {
                return false;
            }

            if (gatherer == GatherType.Fisher && task.RequiredGathering > skill)
            {
                return false;
            }

            if (task.RetainerLevel > level)
            {
                return false;
            }

            var items = task.Items.ToArray();
            if (items.Length != 1)
            {
                return false;
            }

            var normal = task.Task as SaintCoinach.Xiv.RetainerTaskNormal;
            if (normal == null)
            {
                return false;
            }

            return true;
        });

        Util.Multipass.Result GenerateOutput(SaintCoinach.Xiv.RetainerTask task, bool immediate, bool maximized)
        {
            var item = task.Items.First();

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

            if (highthresh <= skill && maximized)
            {
                // we don't need a Maximized version here, we just naturally have it
                return new Util.Multipass.Result() { value = float.MinValue, display = $"[[CANCELLED]]" };
            }

            var normal = task.Task as SaintCoinach.Xiv.RetainerTaskNormal;

            int quantity;
            if (highthresh <= skill || maximized)
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

            var latency = immediate ? Market.Latency.Immediate : Market.Latency.Standard;
            float profit = 0;
            if (item.CanBeHq)
            {
                // 20% HQ; vague estimate
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(item, true, Commerce.TransactionType.Fastsell, latency), item, true, throughput * 0.2f, latency) * quantity * 0.2f;
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(item, false, Commerce.TransactionType.Fastsell, latency), item, false, throughput * 0.8f, latency) * quantity * 0.8f;
            }
            else
            {
                profit += Commerce.MarketProfitAdjuster(Commerce.ValueMarket(item, false, Commerce.TransactionType.Fastsell, latency), item, false, throughput, latency) * quantity;
            }

            if (maximized)
            {
                return new Util.Multipass.Result() { value = profit, display = $"{profit:F0}: {quantity}x {item.Name} (lv{task.RetainerLevel}, req {highthresh})" };
            }
            else
            {
                return new Util.Multipass.Result() { value = profit, display = $"{profit:F0}: {quantity}x {item.Name} (lv{task.RetainerLevel})" };
            }
        }

        IEnumerable<Util.Multipass.Input<bool>> processors = Enumerable.Empty<Util.Multipass.Input<bool>>();

        processors = processors.Concat(
            tasks.Select(
                task => new Util.Multipass.Input<bool>()
                {
                    evaluator = immediate => GenerateOutput(task, immediate, false)
                }
             )
         );

        processors = processors.Concat(
            tasks.Select(
                task => new Util.Multipass.Input<bool>()
                {
                    evaluator = immediate => GenerateOutput(task, immediate, true)
                }
             )
         );

        Util.Multipass.Process(processors, new bool[] { false, true }, 10);
    }

    private static Dictionary<SaintCoinach.Xiv.Item, int> Sourced = new Dictionary<SaintCoinach.Xiv.Item, int>();

    private static string CraftSourceFormatterVendor(SaintCoinach.Xiv.Item item, int count)
    {
        return $"  {item.Name} x{count} ({item.VendorPrice():F0}g/ea)\n";
    }

    private static string CraftSourceFormatterMarket(SaintCoinach.Xiv.Item item, Market.Pricing.Bracket bracket)
    {
        string suffix = "";
        float expected = Commerce.ValueMarket(item, false, Commerce.TransactionType.Longterm, Market.Latency.Standard);
        if (bracket.marketMax > expected * 1.2f)
        {
            suffix += $" (expected ~{expected:F0}g/ea)";
        }

        if (bracket.fullStackPrice > bracket.incrementalPrice * 4)
        {
            suffix += $" (full stack price x{bracket.fullStackPrice / bracket.incrementalPrice:F2})";
        }  

        if (bracket.marketMin == bracket.marketMax)
        {
            return $"  {item.Name} x{bracket.marketCount} ({bracket.marketMin:F0}g/ea){suffix}\n";
        }
        else
        {
            return $"  {item.Name} x{bracket.marketCount} ({bracket.marketMin:F0}-{bracket.marketMax:F0}g/ea){suffix}\n";
        }
    }

    private static HashSet<SaintCoinach.Xiv.Recipe> standardRecipes;
    public static void SourceAddCraftElements(string role, int minlevel, int maxlevel)
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
                if (!Sourced.ContainsKey(ingredient.Item))
                {
                    Sourced[ingredient.Item] = 0;
                }

                Sourced[ingredient.Item] = Sourced[ingredient.Item] + ingredient.Count;
            }

            bool careAboutHQ = false;
            if (recipe.ResultItem.CanBeHq)
            {
                float basePrice = Commerce.ValueSell(recipe.ResultItem, false, Market.Latency.Standard);
                float hqPrice = Commerce.ValueSell(recipe.ResultItem, true, Market.Latency.Standard);

                careAboutHQ = !(hqPrice - 100 <= basePrice || hqPrice / 1.2f <= basePrice);
            }
            
            if (careAboutHQ)
            {
                if (!Sourced.ContainsKey(recipe.ResultItem))
                {
                    Sourced[recipe.ResultItem] = 0;
                }

                Sourced[recipe.ResultItem] = Sourced[recipe.ResultItem] - recipe.ResultCount;
            }
        }
    }

    public static void SourceDoAnalysis()
    {
        // Strip out negatives
        Sourced = Sourced.Where(kvp => kvp.Value > 0).ToDictionary();

        {
            var remaining = new Dictionary<Market.Pricing, int>();
            foreach (var kvp in Sourced)
            {
                remaining[Market.Prices(kvp.Key, Market.Latency.Immediate)] = kvp.Value;
            }

            string result = "";

            {
                result += "Crystals:\n";
                var nextpass = new Dictionary<Market.Pricing, int>();
                foreach (var itemcombo in remaining.OrderBy(itemcombo => itemcombo.Key.Item.Name))
                {
                    if (itemcombo.Key.Item.IsCrystal())
                    {
                        result += $"  {itemcombo.Key.Item.Name} x{itemcombo.Value}\n";
                    }
                    else
                    {
                        nextpass.Add(itemcombo.Key, itemcombo.Value);
                    }
                }
                result += "\n";

                remaining = nextpass;
            }

            {
                result += "Market-procured:\n";
                var nextpass = new Dictionary<Market.Pricing, int>();
                foreach (var itemcombo in remaining.OrderBy(itemcombo => itemcombo.Key.Item.Name).ProgressBar(false))
                {
                    // First filter out the market purchases

                    float cost = itemcombo.Key.PriceForRange(0, itemcombo.Value, out var bracket);

                    if (bracket.containsMarket)
                    {
                        result += CraftSourceFormatterMarket(itemcombo.Key.Item, bracket);
                    }

                    if (bracket.containsVendor)
                    {
                        nextpass.Add(itemcombo.Key, bracket.vendorCount);
                    }
                }
                remaining = nextpass;
            }

            while (true)
            {
                bool found = false;
                foreach (var item in remaining)
                {
                    if (Commerce.SellersForItem(item.Key.Item).Count() == 1)
                    {
                        remaining = SubsumeItems(remaining, Commerce.SellersForItem(item.Key.Item).First(), ref result);
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
                    foreach (var npc in Commerce.SellersForItem(item.Key.Item))
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
                        result += CraftSourceFormatterVendor(item.Key.Item, item.Value);
                    }
                    break;
                }

                var bestNpc = npcCounts.MaxBy(kv => kv.Value * 100000 + Commerce.ItemCountInShop(kv.Key)).Key;

                // Grab things
                remaining = SubsumeItems(remaining, bestNpc, ref result);
            }

            Dbg.Inf(result);
        }
    }

    private static Dictionary<Market.Pricing, int> SubsumeItems(Dictionary<Market.Pricing, int> remaining, SaintCoinach.Xiv.ENpc npc, ref string result)
    {
        result += $"\n{npc.ToZifaString()}:\n";

        var newRemaining = new Dictionary<Market.Pricing, int>();
        foreach (var item in remaining.OrderBy(item => item.Key.Item.Name))
        {
            if (Commerce.SellersForItem(item.Key.Item).Contains(npc))
            {
                result += CraftSourceFormatterVendor(item.Key.Item, item.Value);
            }
            else
            {
                newRemaining.Add(item.Key, item.Value);
            }
        }

        return newRemaining;
    }

    private static void CofferAnalyze(int ilvl, string slot)
    {
        var items = Db.GetSheet<SaintCoinach.Xiv.Item>();
        var itemsFiltered = items.Where(item =>
        {
            if (item.ItemLevel.Key != ilvl)
            {
                return false;
            }

            if (!item.EquipSlotCategory.PossibleSlots.Any(pslot => pslot.Name.ToLower() == slot.ToLower()))
            {
                return false;
            }

            return true;
        });

        DoItemsetComparison(itemsFiltered.Select(item => new ItemsetOption() { item = item, hq = true, count = 1 }));
    }

    private enum Stat
    {
        CP,
        Craftsmanship,
        Control,
    }
    private static int[][] MateriaSuccessRates = new int[][] {
        new int[] { 90, 82, 70, 58, 17, 17, 17, 17 },
        new int[] { 48, 44, 38, 32, 10, 0, 10, 0},
        new int[] { 28, 26, 22, 20, 7, 0, 7, 0 },
        new int[] { 16, 16, 14, 12, 5, 0, 5, 0 }
    };
    class Materia
    {
        public string name;
        public int amount;
        public int rank;
        public Stat stat;
        public float cost;

        public float CalculateMeldChance(int slot)
        {
            float meldChance = 100;
            if (slot >= overmeldStatus.baseSlots)
            {
                meldChance = MateriaSuccessRates[slot - overmeldStatus.baseSlots][rank];
            }

            return meldChance / 100;
        }
    }
    private static Materia[] MateriaData = new Materia[]
    {
        new Materia { stat = Stat.CP, rank = 0, amount = 1, name = "Craftsman's Cunning Materia I" },
        new Materia { stat = Stat.CP, rank = 1, amount = 2, name = "Craftsman's Cunning Materia II" },
        new Materia { stat = Stat.CP, rank = 2, amount = 3, name = "Craftsman's Cunning Materia III" },
        new Materia { stat = Stat.CP, rank = 3, amount = 4, name = "Craftsman's Cunning Materia IV" },
        new Materia { stat = Stat.CP, rank = 4, amount = 6, name = "Craftsman's Cunning Materia V" },
        new Materia { stat = Stat.CP, rank = 5, amount = 8, name = "Craftsman's Cunning Materia VI" },
        new Materia { stat = Stat.CP, rank = 6, amount = 7, name = "Craftsman's Cunning Materia VII" },
        new Materia { stat = Stat.CP, rank = 7, amount = 9, name = "Craftsman's Cunning Materia VIII" },
        new Materia { stat = Stat.Craftsmanship, rank = 0, amount = 3, name = "Craftsman's Competence Materia I" },
        new Materia { stat = Stat.Craftsmanship, rank = 1, amount = 4, name = "Craftsman's Competence Materia II" },
        new Materia { stat = Stat.Craftsmanship, rank = 2, amount = 5, name = "Craftsman's Competence Materia III" },
        new Materia { stat = Stat.Craftsmanship, rank = 3, amount = 6, name = "Craftsman's Competence Materia IV" },
        new Materia { stat = Stat.Craftsmanship, rank = 4, amount = 11, name = "Craftsman's Competence Materia V" },
        new Materia { stat = Stat.Craftsmanship, rank = 5, amount = 16, name = "Craftsman's Competence Materia VI" },
        new Materia { stat = Stat.Craftsmanship, rank = 6, amount = 14, name = "Craftsman's Competence Materia VII" },
        new Materia { stat = Stat.Craftsmanship, rank = 7, amount = 21, name = "Craftsman's Competence Materia VIII" },
        new Materia { stat = Stat.Control, rank = 0, amount = 1, name = "Craftsman's Command Materia I" },
        new Materia { stat = Stat.Control, rank = 1, amount = 2, name = "Craftsman's Command Materia II" },
        new Materia { stat = Stat.Control, rank = 2, amount = 3, name = "Craftsman's Command Materia III" },
        new Materia { stat = Stat.Control, rank = 3, amount = 4, name = "Craftsman's Command Materia IV" },
        new Materia { stat = Stat.Control, rank = 4, amount = 7, name = "Craftsman's Command Materia V" },
        new Materia { stat = Stat.Control, rank = 5, amount = 10, name = "Craftsman's Command Materia VI" },
        new Materia { stat = Stat.Control, rank = 6, amount = 9, name = "Craftsman's Command Materia VII" },
        new Materia { stat = Stat.Control, rank = 7, amount = 13, name = "Craftsman's Command Materia VIII" },
    };
    static float overmeldBestResult;
    static string overmeldBestResultReadable;
    struct OvermeldStatus
    {
        public Materia[] chosen;
        public int[] values;
        public int[] allowed;
        public int[] produced;
        public int baseSlots;
    }
    static OvermeldStatus overmeldStatus = new OvermeldStatus();
    private static void DoOvermeld(int slots, int cp, int crafts, int control, int craftsval, int controlval)
    {
        foreach (var materia in MateriaData.ProgressBar())
        {
            materia.cost = Commerce.ValueBuy(Db.Item(materia.name), false, Commerce.TransactionType.Immediate, Market.Latency.Immediate);
        }

        foreach (var materia in MateriaData)
        {
            Dbg.Inf($"{materia.name}: {materia.cost:F0}");
        }

        overmeldBestResult = 0;
        overmeldBestResultReadable = null;
        overmeldStatus = new OvermeldStatus();
        overmeldStatus.chosen = new Materia[5];
        overmeldStatus.values = new int[Enum.GetNames(typeof(Stat)).Length];
        overmeldStatus.allowed = new int[Enum.GetNames(typeof(Stat)).Length];
        overmeldStatus.produced = new int[Enum.GetNames(typeof(Stat)).Length];

        overmeldStatus.values[(int)Stat.Craftsmanship] = craftsval;
        overmeldStatus.values[(int)Stat.Control] = controlval;

        overmeldStatus.allowed[(int)Stat.CP] = cp;
        overmeldStatus.allowed[(int)Stat.Craftsmanship] = crafts;
        overmeldStatus.allowed[(int)Stat.Control] = control;

        overmeldStatus.baseSlots = slots;

        DoOvermeldOptimize(0, 0);

        Dbg.Inf("");
        Dbg.Inf("Complete!");
        Dbg.Inf(overmeldBestResultReadable);
    }

    private static void DoOvermeldOptimize(int index, float cost)
    {
        if (index == overmeldStatus.chosen.Length)
        {
            // we am done; calculate value
            if (overmeldStatus.produced[(int)Stat.CP] < overmeldStatus.allowed[(int)Stat.CP])
            {
                // nope.
                return;
            }

            float netValue = -cost;
            for (int i = 0; i < overmeldStatus.produced.Length; ++i)
            {
                netValue += overmeldStatus.values[i] * Math.Min(overmeldStatus.allowed[i], overmeldStatus.produced[i]);
            }

            if (netValue > overmeldBestResult)
            {
                // yay we're better
                overmeldBestResult = netValue;

                float totalCost = 0;
                overmeldBestResultReadable = "";
                overmeldBestResultReadable += "\n";
                overmeldBestResultReadable += string.Join("\n", overmeldStatus.chosen.Select((materia, slot) => {
                    float meldChance = materia.CalculateMeldChance(slot);
                    float elementCost = materia.cost / meldChance;
                    totalCost += elementCost;

                    return $"  {materia.name} {materia.cost}g x{1 / meldChance:F0} ({elementCost:F0}g total, {elementCost / materia.amount:F0}/{materia.stat})";
                }));
                overmeldBestResultReadable += "\n\n";

                float totalValue = 0;
                for (int i = 0; i < Enum.GetNames(typeof(Stat)).Length; ++i)
                {
                    int produced = overmeldStatus.produced[i];
                    int allowed = overmeldStatus.allowed[i];
                    overmeldBestResultReadable += $"  {(Stat)i}: +{Math.Min(produced, allowed)} / {allowed}";

                    if (produced > allowed)
                    {
                        overmeldBestResultReadable += $" (wasted {produced - allowed})";
                    }

                    float statValue = produced * overmeldStatus.values[i];
                    totalValue += statValue;
                    overmeldBestResultReadable += $" (value {statValue}))";

                    overmeldBestResultReadable += "\n";
                }

                overmeldBestResultReadable += $"\nTotal cost: {totalCost:F0}, total value {totalValue:F0}, net value {netValue:F0}";
            }

            return;
        }

        foreach (var materia in MateriaData)
        {
            // add each materia in order

            float meldChance = materia.CalculateMeldChance(index);

            if (meldChance == 0)
            {
                // can't meld it
                continue;
            }

            overmeldStatus.chosen[index] = materia;
            overmeldStatus.produced[(int)materia.stat] += materia.amount;

            DoOvermeldOptimize(index + 1, cost + materia.cost / meldChance);

            overmeldStatus.chosen[index] = null;
            overmeldStatus.produced[(int)materia.stat] -= materia.amount;
        }
    }

    public static void AnalyzeCraftingFood()
    {
        var items = Db.GetSheet<SaintCoinach.Xiv.Item>().Where(item =>
        {
            var enhancement = item.ItemAction as SaintCoinach.Xiv.ItemActions.Enhancement;
            if (enhancement == null)
            {
                return false;
            }

            var food = enhancement.ItemFood;
            if (food == null)
            {
                return false;
            }

            if (!food.Parameters.Any(param => param.BaseParam.Name == "CP"))
            {
                return false;
            }

            return true;
        });

        Dbg.Inf("");

        const int count = 10;
        foreach (var item in items.ProgressBar().OrderBy(item => item.ItemLevel.Key))
        {
            float price = Market.Prices(item, Market.Latency.Immediate).PriceForQuantity(count) / count;

            var enhancement = item.ItemAction as SaintCoinach.Xiv.ItemActions.Enhancement;
            var food = enhancement.ItemFood;

            string statstring = string.Join(", ", food.Parameters.Select(param => $"{param.BaseParam.Name} {param.Values.First().ToString()}"));

            Dbg.Inf($"{price:F0}: {item.Name}, ({statstring})");
        }
    }
}
