
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

    public static void Sort<T>(this ICollection<T> collection, Func<T, T, bool> comparator)
    {
        var list = collection.ToList();
        list.Sort((lhs, rhs) => comparator(lhs, rhs) ? -1 : (comparator(rhs, lhs) ? 1 : 0));
        collection.Clear();
        foreach (var elem in list)
        {
            collection.Add(elem);
        }
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