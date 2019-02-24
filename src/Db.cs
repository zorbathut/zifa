
using System.Collections.Generic;
using System;

public static class Db
{
    private static SaintCoinach.ARealmReversed Realm = null;

    public static void Init()
    {   
        const string GameDirectory = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn";
        Realm = new SaintCoinach.ARealmReversed(GameDirectory, "../../../thirdparty/SaintCoinach/SaintCoinach/SaintCoinach.History.zip", SaintCoinach.Ex.Language.English);
        if (!Realm.IsCurrentVersion) {
            const bool IncludeDataChanges = true;
            var updateReport = Realm.Update(IncludeDataChanges, new Progress<SaintCoinach.Ex.Relational.Update.UpdateProgress>(prog => Dbg.Inf(prog.ToString())));
        }
    }

    public static SaintCoinach.Xiv.IXivSheet<T> GetSheet<T>() where T : SaintCoinach.Xiv.XivRow
    {
        return Realm.GameData.GetSheet<T>();
    }

    public static SaintCoinach.Xiv.XivSheet2<T> GetSheet2<T>() where T : SaintCoinach.Xiv.XivSubRow
    {
        return Realm.GameData.GetSheet2<T>();
    }

    public static SaintCoinach.Xiv.Item Item(int id)
    {
        return GetSheet<SaintCoinach.Xiv.Item>()[id];
    }

    private static Dictionary<string, int> ItemLookup = null;
    public static SaintCoinach.Xiv.Item Item(string name)
    {
        if (ItemLookup == null)
        {
            ItemLookup = new Dictionary<string, int>();
            foreach (var item in GetSheet<SaintCoinach.Xiv.Item>())
            {
                if (ItemLookup.ContainsKey(item.Name))
                {
                    ItemLookup[item.Name] = -1;
                }
                else
                {
                    ItemLookup[item.Name] = item.Key;
                }
            }

            Dbg.Inf("Generated item lookup table");
        }

        return Item(ItemLookup[name]);
    }

    public static IEnumerable<SaintCoinach.Xiv.Item> ItemLoose(string[] tokens)
    {
        foreach (var item in GetSheet<SaintCoinach.Xiv.Item>())
        {
            string iname = item.Name.ToString();

            bool good = true;
            foreach (var token in tokens)
            {
                if (token[0] == '-')
                {
                    if (iname.IndexOf(token.Substring(1), StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        good = false;
                        break;
                    }
                }
                else
                {
                    if (iname.IndexOf(token, StringComparison.CurrentCultureIgnoreCase) == -1)
                    {
                        good = false;
                        break;
                    }
                }
            }

            if (good)
            {
                yield return item;
            }
        }
    }

    private static Dictionary<SaintCoinach.Xiv.Item, SaintCoinach.Xiv.Recipe> RecipeLookup = null;
    public static SaintCoinach.Xiv.Recipe Recipe(SaintCoinach.Xiv.Item item)
    {
        if (RecipeLookup == null)
        {
            RecipeLookup = new Dictionary<SaintCoinach.Xiv.Item, SaintCoinach.Xiv.Recipe>();
            foreach (var recipe in GetSheet<SaintCoinach.Xiv.Recipe>())
            {
                if (recipe.ResultItem == null)
                {
                    continue;
                }

                if (RecipeLookup.ContainsKey(recipe.ResultItem))
                {
                    RecipeLookup[recipe.ResultItem] = null;
                }
                else
                {
                    RecipeLookup[recipe.ResultItem] = recipe;
                }
            }

            Dbg.Inf("Generated recipe lookup table");
        }

        return RecipeLookup[item];
    }
}
