
// Some code in this file was copied from ffxiv-craft-opt-web under the zlib license.

using System;
using System.Collections.Generic;

public static class CraftingUtil
{
    // It is unclear how accurate these are, but I have no reason to believe they're wrong, so, okay

    public static float ConditionChanceGood(int recipeLevel, int crafterLevel)
    {
        bool qualityAssurance = crafterLevel >= 63;
        if (recipeLevel >= 300) // 70*+
            return qualityAssurance ? 0.11f : 0.10f;
        else if (recipeLevel >= 276) // 65+
            return qualityAssurance ? 0.17f : 0.15f;
        else if (recipeLevel >= 255) // 61+
            return qualityAssurance ? 0.22f : 0.20f;
        else if (recipeLevel >= 150) // 60+
            return qualityAssurance ? 0.11f : 0.10f;
        else if (recipeLevel >= 136) // 55+
            return qualityAssurance ? 0.17f : 0.15f;
        else
            return qualityAssurance ? 0.27f : 0.25f;
    }

    public static float ConditionChanceExcellent(int recipeLevel)
    {
        if (recipeLevel >= 300) // 70*+
            return 0.01f;
        else if (recipeLevel >= 255) // 61+
            return 0.02f;
        else if (recipeLevel >= 150) // 60+
            return 0.01f;
        else
            return 0.02f;
    }

    private static float QualityFromHqPercent(int hqPercent) {
        var x = hqPercent;
        return (float)(-5.6604E-6 * Math.Pow(x, 4) + 0.0015369705 * Math.Pow(x, 3) - 0.1426469573 * Math.Pow(x, 2) + 5.6122722959 * x - 5.5950384565);
    }

    public static List<float> GenerateQualityCache(int maxQuality)
    {
        // this can definitely be done more cleanly

        var results = new List<float>();
        for (int pct = 0; pct <= 100; ++pct)
        {
            int qualityVal = (int)(maxQuality * QualityFromHqPercent(pct) / 100);

            while (results.Count < maxQuality && results.Count < qualityVal)
            {
                results.Add(pct / 100f);
            }
        }

        return results;
    }

    public static float BaseProgressIncrease(CraftingCalculator.GlobalState globalState)
    {
        float baseProgress = 0;
        if (globalState.crafterLevel > 250)
            baseProgress = 1.834712812e-5f * globalState.crafterCraftsmanship * globalState.crafterCraftsmanship + 1.904074773e-1f * globalState.crafterCraftsmanship + 1.544103837f;
        else if (globalState.crafterLevel > 110)
            baseProgress = 2.09860e-5f * globalState.crafterCraftsmanship * globalState.crafterCraftsmanship + 0.196184f * globalState.crafterCraftsmanship + 2.68452f;
        else
            baseProgress = 0.214959f * globalState.crafterCraftsmanship + 1.6f;

        // Level boost for recipes below crafter level
        // TODO: This is wrong; make it better
        int levelDifference = globalState.crafterLevel - globalState.recipeLevel;
        float levelCorrectionFactor = 0;
        if (levelDifference > 0) levelCorrectionFactor += (0.25f / 5) * Math.Min(levelDifference, 5);
        if (levelDifference > 5) levelCorrectionFactor += (0.10f / 5) * Math.Min(levelDifference - 5, 10);
        if (levelDifference > 15) levelCorrectionFactor += (0.05f / 5) * Math.Min(levelDifference - 15, 5);
        if (levelDifference > 20) levelCorrectionFactor += 0.0006f * (levelDifference - 20);

        // Level penalty for recipes above crafter level
        float recipeLevelPenalty = 0;
        // TODO: this is missing
        /*if (levelDifference < 0) {
            levelCorrectionFactor += 0.025 * Math.Max(levelDifference, -10);
            if (ProgressPenaltyTable[recipeLevel]) {
                recipeLevelPenalty += ProgressPenaltyTable[recipeLevel];
            }
        }*/

        // Level factor is rounded to nearest percent
        levelCorrectionFactor = (float)Math.Floor(levelCorrectionFactor * 100) / 100;

        return baseProgress * (1 + levelCorrectionFactor) * (1 + recipeLevelPenalty);
    }

    public static float BaseQualityIncrease(CraftingCalculator.GlobalState globalState)
    {
        float baseQuality = 3.46e-5f * globalState.crafterControl * globalState.crafterControl + 0.3514f * globalState.crafterControl + 34.66f;

        float recipeLevelPenalty = 0;
        if (globalState.recipeLevel > 50)
        {
            // Starts at base penalty amount depending on recipe tier
            // TODO: implement this
            /*var recipeLevelPenaltyLevel = 0;
            for (var penaltyLevel in QualityPenaltyTable) {
                penaltyLevel = parseInt(penaltyLevel);
                var penaltyValue = QualityPenaltyTable[penaltyLevel];
                if (recipeLevel >= penaltyLevel && penaltyLevel >= recipeLevelPenaltyLevel) {
                    recipeLevelPenalty = penaltyValue;
                    recipeLevelPenaltyLevel = penaltyLevel;
                }
            }
            // Smaller penalty applied for additional recipe levels within the tier
            recipeLevelPenalty += (recipeLevel - recipeLevelPenaltyLevel) * -0.0002;*/
        }
        else
        {
            recipeLevelPenalty += globalState.recipeLevel * -0.00015f + 0.005f;
        }

        // Level penalty for recipes above crafter level
        int levelDifference = globalState.crafterLevel - globalState.recipeLevel;
        float levelCorrectionFactor = 0;
        if (levelDifference < 0)
        {
            levelCorrectionFactor = 0.05f * Math.Max(levelDifference, -10);
        }

        return baseQuality * (1 + levelCorrectionFactor) * (1 + recipeLevelPenalty);
    }
}
