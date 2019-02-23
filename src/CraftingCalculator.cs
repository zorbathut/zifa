
// Some code in this file was copied from ffxiv-craft-opt-web under the zlib license.

using System;
using System.Collections.Generic;
using System.Linq;

public static class CraftingCalculator
{
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

    public struct CraftingState : IEquatable<CraftingState>
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

        public override bool Equals(object obj) 
        {
            return obj is CraftingState && this == (CraftingState)obj;
        }
        bool IEquatable<CraftingState>.Equals(CraftingState obj) 
        {
            return this == obj;
        }
        public override int GetHashCode() 
        {
            return progress.GetHashCode() ^
                quality.GetHashCode() ^
                durability.GetHashCode() ^
                condition.GetHashCode() ^
                cp.GetHashCode() ^
                innerQuiet.GetHashCode() ^
                steadyHand.GetHashCode() ^
                greatStrides.GetHashCode();
        }
        public static bool operator==(CraftingState lhs, CraftingState rhs) 
        {
            return lhs.progress == rhs.progress &&
                lhs.quality == rhs.quality &&
                lhs.durability == rhs.durability &&
                lhs.condition == rhs.condition &&
                lhs.cp == rhs.cp &&
                lhs.innerQuiet == rhs.innerQuiet &&
                lhs.steadyHand == rhs.steadyHand &&
                lhs.greatStrides == rhs.greatStrides;
        }
        public static bool operator!=(CraftingState lhs, CraftingState rhs) 
        {
            return !(lhs == rhs);
        }
    }

    public struct CraftingResult
    {
        public CraftingAbilityDef nextAction;
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

        Dbg.Inf($"Working . . .");

        var result = OptimizeResult(startState, globalState);

        Dbg.Inf($"Cache size: {globalState.cache.Count}");
        Dbg.Inf($"Expected score: {result.expectedScore}");

        Dbg.Inf("");

        var currentState = startState;
        while (true)
        {
            var currentResult = OptimizeResult(currentState, globalState);
            if (currentResult.nextAction == null)
            {
                break;
            }

            Dbg.Inf("");
            Dbg.Inf("=============");
            Dbg.Inf($"Progress {currentState.progress} - Quality {currentState.quality} - Durability {currentState.durability} - Condition {currentState.condition} - CP {currentState.cp}");
            Dbg.Inf($"Next action: {currentResult.nextAction}");

            string input = Console.ReadLine();
            if (input == "y" || input == "Y")
            {
                currentState = currentResult.nextAction.Perform(currentState, globalState, true);
                if (currentState.condition == Condition.Distribute) currentState.condition = Condition.Normal;
            }
            else if (input == "n" || input == "N")
            {
                currentState = currentResult.nextAction.Perform(currentState, globalState, true);
                if (currentState.condition == Condition.Distribute) currentState.condition = Condition.Normal;
            }
            else if (input == "e" || input == "E")
            {
                currentState.condition = Condition.Excellent;
            }
            else if (input == "g" || input == "G")
            {
                currentState.condition = Condition.Good;
            }
            else if (input == "o" || input == "O")
            {
                currentState.condition = Condition.Normal;
            }
            else if (input == "p" || input == "P")
            {
                currentState.condition = Condition.Poor;
            }
            else
            {
                Dbg.Inf("Invalid input, please enter y/n for success/failure, or e/g/o/p to manually set condition");
            }
        }

        Dbg.Inf("Done!");
    }

    // Takes state and simply does the cache lookup.
    private static CraftingResult OptimizeResult(CraftingState craftingState, GlobalState globalState)
    {
        if (!globalState.cache.ContainsKey(craftingState))
        {
            globalState.cache[craftingState] = OptimizeResultImmediate(craftingState, globalState);
        }

        return globalState.cache[craftingState];
    }

    // Does the actual optimization for finding the next best step. Handles success and failure, handles the condition split, then handles the actual events.
    private static CraftingResult OptimizeResultImmediate(CraftingState craftingState, GlobalState globalState)
    {
        // End condition: success!
        if (craftingState.progress >= globalState.totalProgress)
        {
            var result = new CraftingResult();
            result.expectedScore = globalState.qualityChance[Math.Min(craftingState.quality, globalState.totalQuality)];
            return result;
        }

        // End condition: failure!
        if (craftingState.durability <= 0)
        {
            var result = new CraftingResult();
            result.expectedScore = 0;
            return result;
        }

        // Condition split!
        if (craftingState.condition == Condition.Distribute)
        {
            float excellentChance = CraftingUtil.ConditionChanceExcellent(globalState.recipeLevel);
            float goodChance = CraftingUtil.ConditionChanceGood(globalState.recipeLevel, globalState.crafterLevel);
            float normalChance = 1f - goodChance - excellentChance;

            craftingState.condition = Condition.Excellent;
            CraftingResult excellentResult = OptimizeResult(craftingState, globalState);

            craftingState.condition = Condition.Good;
            CraftingResult goodResult = OptimizeResult(craftingState, globalState);

            craftingState.condition = Condition.Normal;
            CraftingResult normalResult = OptimizeResult(craftingState, globalState);

            CraftingResult compositeResult = normalResult;
            compositeResult.expectedScore = excellentChance * excellentResult.expectedScore + goodChance * goodResult.expectedScore + normalChance * normalResult.expectedScore;

            return compositeResult;
        }

        var best = new CraftingResult();

        // Do all the things!

        foreach (var ability in Def.Database<CraftingAbilityDef>.List)
        {
            CraftingState nextState;
            if (ability.requirement != null)
            {
                nextState = ability.requirement.Validate(craftingState);

                if (nextState.durability == 0)
                {
                    // failure
                    continue;
                }
            }
            else
            {
                nextState = craftingState;
            }

            int successChance;
            if (ability.success != null)
            {
                successChance = ability.success.Chance(craftingState);
            }
            else
            {
                successChance = 100;
            }

            var accumulator = new CraftingResult();
            accumulator.nextAction = ability;
            if (successChance == 0)
            {
                var result = OptimizeResult(ability.effect.Apply(nextState, globalState, false), globalState);
                accumulator.expectedScore = result.expectedScore;
            }
            else if (successChance == 100)
            {
                var result = OptimizeResult(ability.effect.Apply(nextState, globalState, true), globalState);
                accumulator.expectedScore = result.expectedScore;
            }
            else
            {
                var failResult = OptimizeResult(ability.effect.Apply(nextState, globalState, false), globalState);
                var succeedResult = OptimizeResult(ability.effect.Apply(nextState, globalState, true), globalState);
                accumulator.expectedScore = succeedResult.expectedScore * successChance / 100 + failResult.expectedScore * ( 100 - successChance ) / 100;
            }

            if (best.expectedScore < accumulator.expectedScore)
            {
                best = accumulator;
            }
        }
        
        return best;
    }
}
