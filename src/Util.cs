
using System;
using System.Collections.Generic;
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
}
