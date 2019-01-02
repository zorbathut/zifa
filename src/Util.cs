
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
        var request = WebRequest.Create(url);
        var response = request.GetResponse();
        var stream = response.GetResponseStream();
        var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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
        public int price;
        public int count;
    }
    public static float Median(this IEnumerable<Element> elements_enum)
    {
        var elements = elements_enum.ToList();
        elements.Sort((lhs, rhs) => lhs.price < rhs.price);

        if (elements.Count == 0)
        {
            // okay then
            return float.NaN;
        }

        while (elements.Count > 1)
        {
            int mid = (elements[0].price + elements[elements.Count - 1].price) / 2;
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

        return elements[0].price;
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, T added)
    {
        foreach (var item in first)
        {
            yield return item;
        }

        yield return added;
    }
}
