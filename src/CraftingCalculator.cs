
// Some code in this file was copied from ffxiv-craft-opt-web under the zlib license.

using System;
using System.Collections.Generic;
using System.Linq;

public static class CraftingCalculator
{
    public enum Action
    {
        Done,
        BasicSynthesis,
        BasicTouch,
        MastersMend,
        SteadyHand,
        InnerQuiet,
        Observe,
        CarefulSynthesis,
        StandardTouch,
        GreatStrides,
        MastersMend2,
        StandardSynthesis,
    }

    const int BasicTouchCost = 18;
    const int MastersMendCost = 92;

    public struct GlobalState
    {
        public int totalProgress;
        public int totalQuality;
        public int maxDurability;
        public int maxCP;

        public int crafterLevel;
        public int crafterCraftsmanship;
        public int crafterControl;

        public int recipeLevel;

        public Dictionary<CraftingState, CraftingResult> cache;
        public List<float> qualityChance;

        public void CacheValues()
        {
            qualityChance = CraftingUtil.GenerateQualityCache(totalQuality);
        }
    }

    public enum Condition
    {
        Normal,
        Good,
        Excellent,
        Poor,

        Distribute,  // "Came from Normal, do the fancy split thing"
    }

    public struct CraftingState
    {
        public int progress;
        public int quality;
        public int durability;
        public Condition condition;
        public int cp;

        public sbyte innerQuiet;
        public sbyte steadyHand;
        public sbyte greatStrides;

        public void Tick(int durabilityCost = 10)
        {
            durability -= durabilityCost;

            innerQuiet = (sbyte)Math.Max(innerQuiet - 1, 0);
            steadyHand = (sbyte)Math.Max(steadyHand - 1, 0);
            greatStrides = (sbyte)Math.Max(greatStrides - 1, 0);

            if (condition == Condition.Excellent) condition = Condition.Poor;
            else if (condition == Condition.Poor || condition == Condition.Good) condition = Condition.Normal;
            else condition = Condition.Distribute;
        }

        public int SuccessBonus()
        {
            return steadyHand > 0 ? 20 : 0;
        }
    }

    public struct CraftingResult
    {
        public Action nextAction;
        public float expectedScore;

        // TODO: expectedMoves? worstMoves?
    }
    
    public static void Process()
    {
        var globalState = new GlobalState() { totalProgress = 53, totalQuality = 702, maxDurability = 40, maxCP = 272, crafterLevel = 41, crafterCraftsmanship = 201, crafterControl = 199, recipeLevel = 12 };
        globalState.cache = new Dictionary<CraftingState, CraftingResult>();
        globalState.CacheValues();

        Dbg.Inf($"Base progress increase: {CraftingUtil.BaseProgressIncrease(globalState)}");
        Dbg.Inf($"Base quality increase: {CraftingUtil.BaseQualityIncrease(globalState)}");

        var startState = new CraftingState() { durability = globalState.maxDurability, cp = globalState.maxCP };

        var result = OptimizeResult(startState, globalState);

        Dbg.Inf($"Expected score: {result.expectedScore}");
    }

    private static CraftingResult OptimizeResult(CraftingState craftingState, GlobalState globalState)
    {
        if (!globalState.cache.ContainsKey(craftingState))
        {
            globalState.cache[craftingState] = OptimizeResultImmediate(craftingState, globalState);
        }

        return globalState.cache[craftingState];
    }

    private static CraftingResult OptimizeResultImmediate(CraftingState craftingState, GlobalState globalState)
    {
        if (craftingState.progress >= globalState.totalProgress)
        {
            var result = new CraftingResult();
            result.expectedScore = globalState.qualityChance[Math.Min(craftingState.quality, globalState.totalQuality)];
            return result;
        }

        if (craftingState.durability <= 0)
        {
            var result = new CraftingResult();
            result.expectedScore = 0;
            return result;
        }

        var best = new CraftingResult();

        // do all the things, find the best

        // BasicSynthesis
        {
            var accumulator = new CraftingResult();
            accumulator.nextAction = Action.BasicSynthesis;

            int chance = Math.Min(90 + craftingState.SuccessBonus(), 100);

            {
                var result = OptimizeResult(DoCommand(craftingState, globalState, Action.BasicSynthesis, true), globalState);
                accumulator.expectedScore += result.expectedScore * chance / 100;
            }

            {
                var result = OptimizeResult(DoCommand(craftingState, globalState, Action.BasicSynthesis, false), globalState);
                accumulator.expectedScore += result.expectedScore * (100 - chance) / 100;
            }

            if (best.expectedScore < accumulator.expectedScore)
            {
                best = accumulator;
            }
        }

        // BasicTouch
        const int BasicTouchCost = 18;
        if (craftingState.cp >= BasicTouchCost)
        {
            var accumulator = new CraftingResult();
            accumulator.nextAction = Action.BasicTouch;

            int chance = Math.Min(70 + craftingState.SuccessBonus(), 100);

            var nextState = craftingState;
            nextState.cp -= BasicTouchCost;

            {
                var result = OptimizeResult(DoCommand(craftingState, globalState, Action.BasicTouch, true), globalState);
                accumulator.expectedScore += result.expectedScore * chance / 100;
            }

            {
                var result = OptimizeResult(DoCommand(craftingState, globalState, Action.BasicTouch, false), globalState);
                accumulator.expectedScore += result.expectedScore * (100 - chance) / 100;
            }

            if (best.expectedScore < accumulator.expectedScore)
            {
                best = accumulator;
            }
        }

        // MastersMend
        const int MastersMendCost = 92;
        if (craftingState.cp >= MastersMendCost)
        {
            var accumulator = new CraftingResult();
            accumulator.nextAction = Action.MastersMend;

            var result = OptimizeResult(DoCommand(craftingState, globalState, Action.MastersMend, true), globalState);
            accumulator.expectedScore = result.expectedScore;

            if (best.expectedScore < accumulator.expectedScore)
            {
                best = accumulator;
            }
        }

        return best;
    }

    private static CraftingState DoCommand(CraftingState craftingState, GlobalState globalState, Action action, bool outcome)
    {
        if (action == Action.BasicSynthesis)
        {
            if (outcome)
            {
                var success = craftingState;
                success.progress += (int)CraftingUtil.BaseProgressIncrease(globalState);
                success.Tick();
                return success;
            }
            else
            {
                var failure = craftingState;
                failure.Tick();
                return failure;
            }
        }
        else if (action == Action.BasicTouch)
        {
            var nextState = craftingState;
            nextState.cp -= BasicTouchCost;

            if (outcome)
            {
                var success = nextState;
                success.quality += (int)CraftingUtil.BaseQualityIncrease(globalState);
                success.Tick();

                return success;
            }
            else
            {
                var failure = nextState;
                failure.Tick();

                return failure;
            }
        }
        else if (action == Action.MastersMend)
        {
            var success = craftingState;
            success.cp -= MastersMendCost;
            success.durability = Math.Min(success.durability + 30, globalState.maxDurability);
            success.Tick();
            return success;
        }

        Dbg.Err("I don't know what's going on");
        return craftingState;
    }
}
