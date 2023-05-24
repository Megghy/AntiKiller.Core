using LiteDB;

namespace AntiKiller.Core
{
    public static class Datas
    {
#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif
        public static List<FansInfo> Fans = new();
        public static LiteDatabase DB { get; private set; }
        public static ILiteCollection<FansInfo> COL_FANS { get; private set; }
        public static ILiteCollection<AntiInfo> COL_ANTIS { get; private set; }
        public static void Init()
        {
            DB = new(Path.Combine(Environment.CurrentDirectory, "Data.db"));
            COL_FANS = DB.GetCollection<FansInfo>("fans");
            COL_ANTIS = DB.GetCollection<AntiInfo>("antis");
            COL_ANTIS.EnsureIndex(a => a.UnfollowDate);

            Fans = COL_FANS.FindAll().ToList();
            Logs.Success($"当前共 {Fans.Count} 条粉丝记录");
        }
    }
}
