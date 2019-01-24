
using Newtonsoft.Json.Linq;
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
}
