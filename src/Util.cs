
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;

public static class Util
{
    public static string GetURLContents(string url)
    {
        while (true)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                Dbg.Err(url);
                Dbg.Ex(ex);

                WebResponse errResp = ex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    var reader = new StreamReader(respStream);
                    Dbg.Err(reader.ReadToEnd());
                }

                const int waitTime = 5;
                Dbg.Inf($"Waiting {waitTime}s . . .");
                System.Threading.Thread.Sleep(waitTime * 1000);
            }
        }
    }

    public static void Sort<T>(this List<T> collection, Func<T, T, bool> comparator)
    {
        collection.Sort((lhs, rhs) => comparator(lhs, rhs) ? -1 : (comparator(rhs, lhs) ? 1 : 0));
    }

    public static void ExecuteNonQuery(this SQLiteConnection connection, string command)
    {
        new SQLiteCommand(command, connection).ExecuteNonQuery();
    }

    public static T GetField<T>(this SQLiteDataReader reader, string label)
    {
        return reader.GetFieldValue<T>(reader.GetOrdinal(label));
    }

    public class Element
    {
        public long value;
        public int count;
    }
    public static float Median(this IEnumerable<Element> elements_enum)
    {
        var elements = elements_enum.ToList();
        elements.Sort((lhs, rhs) => lhs.value < rhs.value);

        if (elements.Count == 0)
        {
            // okay then
            return float.NaN;
        }

        while (elements.Count > 1)
        {
            long mid = (elements[0].value + elements[elements.Count - 1].value) / 2;
            int removal = Math.Min(elements[0].count, elements[elements.Count - 1].count);

            elements[0].count -= removal;
            elements[elements.Count - 1].count -= removal;

            if (elements[0].count == 0)
            {
                elements.RemoveAt(0);
            }
            if (elements[elements.Count - 1].count == 0)
            {
                elements.RemoveAt(elements.Count - 1);
            }

            if (elements.Count == 0)
            {
                // whoops
                return mid;
            }
        }

        return elements[0].value;
    }

    public static float Percentile(this IEnumerable<int> elements_enum, float percent)
    {
        var elements = elements_enum.ToList();
        elements.Sort((lhs, rhs) => lhs < rhs);

        if (elements.Count == 0)
        {
            // okay then
            return float.NaN;
        }

        float index = (elements.Count - 1) * percent;
        return Lerp(elements[(int)Math.Floor(index)], elements[(int)Math.Ceiling(index)], index - (float)Math.Truncate(index));
    }

    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, T added)
    {
        foreach (var item in first)
        {
            yield return item;
        }

        yield return added;
    }

    public static T FirstOrDefault<T>(this IEnumerable<T> source, T def)
    {
        using (IEnumerator<T> enumerator = source.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                return enumerator.Current;
            }
        }
        
        return def;
    }

    public static bool IsMarketable(this SaintCoinach.Xiv.Item item)
    {
        return item.ItemSearchCategory.Category != 0;
    }

    private static Random shuffleRng = new Random();
    public static void Shuffle<T>(this T[] input)  
    {  
        int n = input.Length;
        while (n > 1) {  
            n--;  
            int k = shuffleRng.Next(n + 1);  
            T value = input[k];  
            input[k] = input[n];  
            input[n] = value;  
        }  
    }

    public static IEnumerable<T> ProgressBar<T>(this IEnumerable<T> input, bool shuffle = true)
    {
        var values = input.ToArray();

        if (shuffle)
        {
            values.Shuffle();
        }

        // in seconds
        float accumulatedTime = 0;
        float accumulatedItems = 0;

        DateTimeOffset lastShown = DateTimeOffset.Now;

        for (int i = 0; i < values.Length; ++i)
        {
            TimeSpan cherenkovInitTaken = Api.InitTime();
            var startTime = DateTimeOffset.Now;

            yield return values[i];

            var deltaTime = (DateTimeOffset.Now - startTime) + (cherenkovInitTaken - Api.InitTime());

            // exponential falloff calculation; 50% every minute
            var falloff = (float)Math.Pow(0.5f, deltaTime.TotalMinutes);

            accumulatedTime *= falloff;
            accumulatedItems *= falloff;

            accumulatedTime += (float)deltaTime.TotalSeconds;
            accumulatedItems += 1;

            if (i > 0 && (DateTimeOffset.Now - lastShown).TotalSeconds > 0.2f)
            {
                var remaining = (values.Length - i) * (accumulatedTime / accumulatedItems);

                Dbg.Inf($"{i} / {values.Length} -- ETA {remaining / 60:F2}m");
                lastShown = DateTimeOffset.Now;
            }
        }
    }

    public static T MaxBy<T>(this IEnumerable<T> input, Func<T, int> predicate)
    {
        var result = default(T);
        int best = int.MinValue;
        foreach (var elem in input)
        {
            int compare = predicate(elem);
            if (compare >= best)
            {
                best = compare;
                result = elem;
            }
        }

        return result;
    }

    public static string ToZifaString(this SaintCoinach.Xiv.ENpc npc)
    {
        if (npc.Locations.Any())
        {
            return $"{npc.Singular}/{npc.Title} in {npc.Locations.First().PlaceName.Name}";
        }
        else
        {
            return $"{npc.Singular}/{npc.Title}";
        }
    }

    public static V TryGetValue<K, V>(this Dictionary<K, V> dict, K key)
    {
        if (dict.TryGetValue(key, out V result))
        {
            return result;
        }
        else
        {
            return default(V);
        }
    }

    public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> input)
    {
        return input.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int elements)
    {
        return source.Skip(Math.Max(0, source.Count() - elements));
    }

    public static void SortBy<K, V>(this List<V> list, Func<V, K> value)
    {
        list.Sort((lhs, rhs) => Comparer<K>.Default.Compare(value(lhs), value(rhs)));
    }

    public static float Log2(float input)
    {
        return (float)(Math.Log(input) / Math.Log(2));
    }

    public static class Multipass
    {
        public struct Input<PT>
        {
            public Func<PT, Result> evaluator;
            public object unique;
        }

        private struct Item<PT>
        {
            public Input<PT> input;
            public Result result;
        }

        public struct Result
        {
            public float value;
            public string display;
        }

        public static float Process<PT>(IEnumerable<Input<PT>> inputs, PT[] passes, int desiredCount)
        {
            List<Item<PT>>[] passData = new List<Item<PT>>[passes.Length];
            for (int i = 0; i < passes.Length; ++i)
            {
                passData[i] = new List<Item<PT>>();
            }

            var lastDisplay = DateTimeOffset.Now;
            foreach (var input in inputs.ProgressBar())
            {
                var result = input.evaluator(passes[0]);
                passData[0].Add(new Item<PT>() {input = input, result = result});

                if (DateTimeOffset.Now - lastDisplay > TimeSpan.FromMinutes(5))
                {
                    passData[0].SortBy(x => x.result.value);
                    foreach (var displayable in passData[0].TakeLast(desiredCount))
                    {
                        Dbg.Inf(displayable.result.display);
                    }

                    lastDisplay = DateTimeOffset.Now;
                    Dbg.Inf("Continuing . . .");
                }
            }

            passData[0].SortBy(x => x.result.value);

            // Time to start doing the pass thing!

            int[] promoted = new int[passes.Length - 1];
            var finalPass = passData.Last();
            while (true)
            {
                // Our algorithm:
                // * If our worst desired final-pass result is better than the best result in all previous passes, stop.
                // * Otherwise, grab the best result in any previous pass and upgrade it to the next pass.

                if (!passData.Take(passes.Length - 1).Any(pass => pass.Any()))
                {
                    // We're out of items; stop.
                    break;
                }

                // If we just plain don't have enough, don't even bother with the next check
                if (finalPass.Count >= desiredCount)
                {
                    var worstFullyProcessed = finalPass[finalPass.Count - desiredCount];

                    // If the worst fully-processed item we have is better than the best item in all previous passes, we're done.
                    if (!passData.Take(passes.Length - 1).Any(pass => pass.Last().result.value > worstFullyProcessed.result.value))
                    {
                        break;
                    }
                }

                // Find the pass with the highest-value item; this is the one we're going to promote
                var bestPass = -1;
                for (int i = 0; i < passes.Length - 1; ++i)
                {
                    if (!passData[i].Any())
                    {
                        continue;
                    }

                    if (bestPass == -1 || passData[i].Last().result.value > passData[bestPass].Last().result.value)
                    {
                        bestPass = i;
                    }
                }

                var processTarget = passData[bestPass].Last();
                passData[bestPass].RemoveAt(passData[bestPass].Count - 1);

                // What we want to figure out here is the number of concrete items done
                // That's the number in our goodResults table that are better-or-equal compared to the best item in all previous passes; they will theoretically not be removed and are done!
                float bestElsewhere = passData.Take(passes.Length - 1).Select(pass => pass.Any() ? pass.Last().result.value : 0).Max();
                int finalized = finalPass.Count(result => result.result.value >= bestElsewhere);
                var bests = passData.Select(pass => pass.Any() ? Log2(pass.Last().result.value).ToString("F1") : "empty");
                Dbg.Inf($"Promoting from pass {bestPass}; promoted [{string.Join(", ", promoted)}], bestl2 [{string.Join(", ", bests)}], finalized {finalized}/{desiredCount} elements");
                ++promoted[bestPass];

                processTarget.result = processTarget.input.evaluator(passes[bestPass + 1]);

                if (float.IsNaN(processTarget.result.value))
                {
                    // Well, that's terrible.
                    continue;
                }

                passData[bestPass + 1].Add(processTarget);
                passData[bestPass + 1].SortBy(x => x.result.value);

                // If we're putting this in the final pass, remove worse un-uniqued elements
                if (bestPass + 1 == passes.Length - 1 && processTarget.input.unique != null)
                {
                    if (passData[bestPass + 1] != finalPass)
                    {
                        Dbg.Err("Pass mismatch!");
                    }

                    var bestOfThisUniqueness = finalPass.Where(r => r.input.unique == processTarget.input.unique).Last();
                    finalPass.RemoveAll(r => r.input.unique == bestOfThisUniqueness.input.unique && r.input.evaluator != bestOfThisUniqueness.input.evaluator);
                }

                // Maybe show some stuff?
                if (DateTimeOffset.Now - lastDisplay > TimeSpan.FromMinutes(1))
                {
                    // top 5 from each pass, top desiredCount from the final pass
                    for (int i = 0; i < passes.Length; ++i)
                    {
                        Dbg.Inf($"\n\nPass {passes[i]}:\n");

                        int count = (i == passes.Length - 1 ? desiredCount : 5);
                        foreach (var displayable in passData[i].TakeLast(count))
                        {
                            Dbg.Inf("  " + displayable.result.display.Replace("\n", "\n  "));
                        }
                    }

                    lastDisplay = DateTimeOffset.Now;
                    Dbg.Inf("Continuing . . .");
                }
            }

            // Done!
            foreach (var result in finalPass.TakeLast(desiredCount))
            {
                Dbg.Inf(result.result.display);
            }

            return finalPass.Last().result.value;
        }
    }

    public static float MinWithoutNan(float lhs, float rhs)
    {
        if (float.IsNaN(lhs))
        {
            return rhs;
        }
        else if (float.IsNaN(rhs))
        {
            return lhs;
        }
        else
        {
            return Math.Min(lhs, rhs);
        }
    }

    public static bool IsCrystal(this SaintCoinach.Xiv.Item item)
    {
        // convenient that they're all batched up
        return item.Key >= 2 && item.Key <= 19;
    }
}

public static class EnumUtil
{
    public static IEnumerable<T> Values<T>()
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }
}

public static class MathUtil
{
    public static int Clamp(int val, int low, int high)
    {
        return Math.Min(Math.Max(val, low), high);
    }

    public static float Clamp(float val, float low, float high)
    {
        return Math.Min(Math.Max(val, low), high);
    }

    public static double Clamp(double val, double low, double high)
    {
        return Math.Min(Math.Max(val, low), high);
    }
}