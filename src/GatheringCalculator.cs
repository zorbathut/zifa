
using System;
using System.Collections.Generic;
using System.Linq;

public static class GatheringCalculator
{
    private enum Action
    {
        Done,
        Gather,
        FieldMastery1,
        FieldMastery2,
        FieldMastery3,
        LeafTurn,
        FloraMaster,
        BountifulHarvest,
        AgelessWords,
        BruntForce,
        BlessedHarvest,
        LeafTurn2,
        Prune,
        Prune2,
    }

    private struct ActionInfo
    {
        public Action action;
        public int cost;
        public bool ephemeral;
        public int dropChance;
        public int hqChance;
        public int attempts;
        public int results;
    }
    private static readonly List<ActionInfo> ActionInfos = new List<ActionInfo>()
    {
        new ActionInfo() { action = Action.Gather },
        new ActionInfo() { action = Action.FieldMastery1, cost = 50, dropChance = 5 },
        new ActionInfo() { action = Action.FieldMastery2, cost = 100, dropChance = 15 },
        new ActionInfo() { action = Action.FieldMastery3, cost = 250, dropChance = 50 },
        new ActionInfo() { action = Action.LeafTurn, cost = 100, hqChance = 10 },
        new ActionInfo() { action = Action.FloraMaster, cost = 50, dropChance = 15, ephemeral = true },
        new ActionInfo() { action = Action.BountifulHarvest, cost = 100, results = 1, ephemeral = true },
        new ActionInfo() { action = Action.AgelessWords, cost = 300, attempts = 1, ephemeral = true },
        //new ActionInfo() { action = Action.BruntForce, dropChance = 100 },    // TODO
        new ActionInfo() { action = Action.BlessedHarvest, cost = 400, results = 1 },
        new ActionInfo() { action = Action.LeafTurn2, cost = 300, hqChance = 30 },
        //new ActionInfo() { action = Action.Prune, cost = 100, hqChance = 10, ephemeral = true },
        //new ActionInfo() { action = Action.Prune2, cost = 200, hqChance = 20, ephemeral = true },
    };

    private static ActionInfo GetActionInfo(this Action action)
    {
        return ActionInfos.Where(ai => ai.action == action).FirstOrDefault();
    }

    private struct GlobalState
    {
        public int baseChance;
        public int baseHQ;
        public bool lookingForHQ;

        public Dictionary<GatheringState, GatheringResult> cache;
    }

    private struct GatheringState
    {
        public int remainingGp;
        public int remainingAttempts;
        public int buffsApplied;

        public bool HasBuff(Action buff)
        {
            return (buffsApplied & (1 << (int)buff)) != 0;
        }

        public GatheringState ApplyBuff(Action buff, int cost)
        {
            var result = this;
            result.buffsApplied |= (1 << (int)buff);
            result.remainingGp -= cost;
            return result;
        }

        private GatheringState RemoveBuff(Action buff)
        {
            var result = this;
            result.buffsApplied &= ~(1 << (int)buff);
            return result;
        }

        public GatheringState Gather()
        {
            var result = this;
            result.remainingAttempts -= 1;

            foreach (var buff in ActionInfos)
            {
                if (buff.ephemeral)
                {
                    result = result.RemoveBuff(buff.action);
                }
            }

            return result;
        }
    }

    private struct GatheringResult
    {
        public Action nextAction;
        public GatheringState nextState;
        public int fullOutput;
        public int moves;
    }

    private struct ActionResult
    {
        public Action nextAction;
        public GatheringState nextState;
        public int actionOutput;
    }

    public struct BakedResult
    {
        public int output;
        public string description;
    }

    public static void Process(int lootChance, int hqChance, int startingGp, int startingAttempts, bool focusOnHq)
    {
        var startState = new GatheringState() { remainingGp = startingGp, remainingAttempts = startingAttempts };
        var globalState = new GlobalState() { baseChance = lootChance, baseHQ = hqChance, lookingForHQ = focusOnHq, cache = new Dictionary<GatheringState, GatheringResult>() };

        var bakedResult = GetBakedResult(startState, globalState);

        Dbg.Inf($"Expected result: {bakedResult.output/10000f:F2}{bakedResult.description}");
    }

    private enum NodeVariants
    {
        None,
        Loot,
        Hq,
        Attempts,
    }

    public static void ProcessLongterm(int lootChance, int hqChance, int startingGp, int startingAttempts, bool focusOnHq)
    {
        var results = new List<BakedResult>();

        foreach (var variant in EnumUtil.Values<NodeVariants>())
        {
            var startState = new GatheringState() { remainingGp = startingGp, remainingAttempts = startingAttempts };
            var globalState = new GlobalState() { baseChance = lootChance, baseHQ = hqChance, lookingForHQ = focusOnHq, cache = new Dictionary<GatheringState, GatheringResult>() };

            if (variant == NodeVariants.Loot) globalState.baseChance += 10;
            if (variant == NodeVariants.Hq) globalState.baseHQ += 5;
            if (variant == NodeVariants.Attempts) startState.remainingAttempts += 1;

            startState.remainingGp = 0;

            var baseResult = GetBakedResult(startState, globalState);

            for (int gp = 50; gp <= startingGp; gp += 50)
            {
                startState.remainingGp = gp;

                var result = GetBakedResult(startState, globalState);
                result.output -= baseResult.output;
                result.output *= 1000;
                result.output /= gp;

                result.description = $"{result.output/10000f:F2} output/kGP - {variant}:{gp}:" + result.description;

                results.Add(result);
            }
        }

        foreach (var result in results.OrderByDescending(r => r.output))
        {
            Dbg.Inf(result.description);
        }
    }

    private static BakedResult GetBakedResult(GatheringState startState, GlobalState globalState)
    {
        var summary = GetBestStep(startState, globalState);

        var output = new BakedResult() { output = summary.fullOutput };

        var currentState = startState;
        while (true)
        {
            var result = GetBestStep(currentState, globalState);
            output.description += $"\n  Action: {result.nextAction}";
            currentState = result.nextState;

            if (result.nextAction == Action.Done)
            {
                break;
            }
        }

        return output;
    }

    private static GatheringResult GetBestStep(GatheringState state, GlobalState globalState)
    {
        if (state.remainingAttempts == 0)
        {
            // we're done, man
            return new GatheringResult();
        }

        if (!globalState.cache.ContainsKey(state))
        {
            var best = new GatheringResult();
            foreach (var actionResult in ExecuteActions(state, globalState))
            {
                var chainedResult = GetBestStep(actionResult.nextState, globalState);

                int fullOutput = chainedResult.fullOutput + actionResult.actionOutput;
                int fullMoves = chainedResult.moves + 1;
                if (best.fullOutput < fullOutput || (best.fullOutput == fullOutput && best.moves > fullMoves))
                {
                    best.fullOutput = fullOutput;
                    best.nextState = actionResult.nextState;
                    best.nextAction = actionResult.nextAction;
                    best.moves = fullMoves;
                }
            }

            globalState.cache[state] = best;
        }

        return globalState.cache[state];
    }

    private static IEnumerable<ActionResult> ExecuteActions(GatheringState state, GlobalState globalState)
    {
        foreach (var actionInfo in ActionInfos)
        {
            if (state.remainingGp < actionInfo.cost)
            {
                // nope
                continue;
            }

            if (state.HasBuff(actionInfo.action))
            {
                // nope
                continue;
            }

            if (actionInfo.action == Action.Gather)
            {
                int actualChance = globalState.baseChance;
                int actualHq = globalState.baseHQ;
                int actualResults = 1;

                foreach (var buff in ActionInfos)
                {
                    if (state.HasBuff(buff.action))
                    {
                        actualChance += buff.dropChance;
                        actualHq += buff.hqChance;
                        actualResults += buff.results;
                    }
                }

                actualChance = Math.Min(actualChance, 100);
                actualHq = Math.Min(actualHq, 100);

                int results = globalState.lookingForHQ ? (actualResults * actualChance * actualHq) : (actualResults * actualChance * 100);

                yield return new ActionResult()
                {
                    nextAction = actionInfo.action,
                    nextState = state.Gather(),
                    actionOutput = results,
                };
            }
            else
            {
                yield return new ActionResult()
                {
                    nextAction = actionInfo.action,
                    nextState = state.ApplyBuff(actionInfo.action, actionInfo.cost),
                };
            }
        }
    }
}
