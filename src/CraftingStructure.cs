
using System;

public class CraftingAbilityDef : Def.Def
{
    public Requirement requirement;
    public Success success;
    public Effect effect;

    public CraftingCalculator.CraftingState Perform(CraftingCalculator.CraftingState input, CraftingCalculator.GlobalState global, bool success)
    {
        if (requirement != null)
        {
            input = requirement.Validate(input);
            //we just assume it worked I guess
        }

        input = effect.Apply(input, global, success);

        return input;
    }
}

public abstract class Requirement
{
    public abstract CraftingCalculator.CraftingState Validate(CraftingCalculator.CraftingState input);
}

public abstract class Success
{
    public abstract int Chance(CraftingCalculator.CraftingState input);
}

public abstract class Effect
{
    public abstract CraftingCalculator.CraftingState Apply(CraftingCalculator.CraftingState input, CraftingCalculator.GlobalState global, bool success);
}


public class RequirementSimple : Requirement
{
    int cp = 0;

    public override CraftingCalculator.CraftingState Validate(CraftingCalculator.CraftingState input)
    {
        if (input.cp >= cp)
        {
            input.cp -= cp;
            return input;
        }

        input.durability = 0;
        return input;
    }
}

public class SuccessSimple : Success
{
    int chance = 0;

    public override int Chance(CraftingCalculator.CraftingState input)
    {
        return MathUtil.Clamp(chance + (input.steadyHand > 0 ? 20 : 0), 0, 100);
    }
}

public class EffectSimple : Effect
{
    int progress = 0;
    int quality = 0;
    int durability = 0;

    int durabilityCost = 10;

    public override CraftingCalculator.CraftingState Apply(CraftingCalculator.CraftingState input, CraftingCalculator.GlobalState global, bool success)
    {
        if (success)
        {
            input.progress += (int)CraftingUtil.BaseProgressIncrease(global) * progress / 100;
            input.quality += (int)CraftingUtil.BaseQualityIncrease(global) * quality / 100;
            input.durability = Math.Min(global.maxDurability, input.durability + durability);
        }

        input.Tick(durabilityCost: durabilityCost);

        return input;
    }
}

public class EffectSteadyHand : Effect
{
    public override CraftingCalculator.CraftingState Apply(CraftingCalculator.CraftingState input, CraftingCalculator.GlobalState global, bool success)
    {
        input.Tick(durabilityCost: 0);

        if (success)
        {
            input.steadyHand = 5;
        }

        return input;
    }
}