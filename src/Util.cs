
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

    public static class Twopass
    {
        private struct Item
        {
            public Func<bool, Result> evaluator;
            public Result result;
        }

        public struct Result
        {
            public float value;
            public string display;
        }

        public static void Process(IEnumerable<Func<bool, Result>> evaluators, int desiredCount)
        {
            var quickResults = new List<Item>();

            var lastDisplay = DateTimeOffset.Now;

            foreach (var evaluator in evaluators.ProgressBar())
            {
                var result = evaluator(false);
                quickResults.Add(new Item() {evaluator = evaluator, result = result});

                if (DateTimeOffset.Now - lastDisplay > TimeSpan.FromMinutes(5))
                {
                    quickResults = quickResults.OrderBy(x => x.result.value).ToList();
                    foreach (var displayable in quickResults.TakeLast(desiredCount))
                    {
                        Dbg.Inf(displayable.result.display);
                    }

                    lastDisplay = DateTimeOffset.Now;
                    Dbg.Inf("Continuing . . .");
                }
            }

            quickResults = quickResults.OrderBy(x => x.result.value).ToList();

            var goodResults = new List<Item>();

            while (quickResults.Count > 0 && (goodResults.Count < desiredCount || goodResults[goodResults.Count - desiredCount].result.value < quickResults[quickResults.Count - 1].result.value))
            {
                Dbg.Inf($"Immediate-testing; at {goodResults.Count}/{desiredCount} elements");

                var process = quickResults[quickResults.Count - 1];
                quickResults.RemoveAt(quickResults.Count - 1);

                process.result = process.evaluator(true);
                goodResults.Add(process);

                goodResults = goodResults.OrderBy(x => x.result.value).ToList();
            }

            foreach (var result in goodResults)
            {
                Dbg.Inf(result.result.display);
            }
        }
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