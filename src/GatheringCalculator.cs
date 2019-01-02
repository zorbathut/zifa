
using System;
using System.Collections.Generic;
using System.Linq;

public static class GatheringCalculator
{
    const int BaseChance = 90;
    const int BaseHQ = 12;
    const int StartingGP = 500;
    const int StartingAttempts = 5;
    const bool LookingForHQ = true;

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
        //new ActionInfo() { action = Action.LeafTurn2, cost = 300, hqChance = 30 },
        new ActionInfo() { action = Action.Prune, cost = 100, hqChance = 10, ephemeral = true },
        new ActionInfo() { action = Action.Prune2, cost = 200, hqChance = 20, ephemeral = true },
    };

    private static ActionInfo GetActionInfo(this Action action)
    {
        return ActionInfos.Where(ai => ai.action == action).FirstOrDefault();
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
        public float fullOutput;
    }

    private struct ActionResult
    {
        public Action nextAction;
        public GatheringState nextState;
        public float actionOutput;
    }

    public static void Process()
    {
        var startState = new GatheringState() { remainingGp = StartingGP, remainingAttempts = StartingAttempts };
        var summary = GetBestStep(startState);

        Dbg.Inf($"Expected result: {summary.fullOutput:F2}");

        var currentState = startState;
        while (true)
        {
            var result = GetBestStep(currentState);
            Dbg.Inf($"  Action: {result.nextAction}");
            currentState = result.nextState;

            if (result.nextAction == Action.Done)
            {
                break;
            }
        }
    }

    private static Dictionary<GatheringState, GatheringResult> Cache = new Dictionary<GatheringState, GatheringResult>();
    private static GatheringResult GetBestStep(GatheringState state)
    {
        if (state.remainingAttempts == 0)
        {
            // we're done, man
            return new GatheringResult();
        }

        if (!Cache.ContainsKey(state))
        {
            var best = new GatheringResult();
            foreach (var actionResult in ExecuteActions(state))
            {
                var chainedResult = GetBestStep(actionResult.nextState);

                float fullOutput = chainedResult.fullOutput + actionResult.actionOutput;
                if (best.fullOutput < fullOutput)
                {
                    best.fullOutput = fullOutput;
                    best.nextState = actionResult.nextState;
                    best.nextAction = actionResult.nextAction;
                }
            }

            Cache[state] = best;
        }

        return Cache[state];
    }

    private static IEnumerable<ActionResult> ExecuteActions(GatheringState state)
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
                int actualChance = BaseChance;
                int actualHq = BaseHQ;
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

                float results = LookingForHQ ? (actualResults * actualChance * actualHq / 10000f) : (actualResults * actualChance / 100f);

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
