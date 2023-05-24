using System.Text;

namespace AntiKiller.Core
{
    public static class Logs
    {
        public readonly static string SavePath = Path.Combine(Environment.CurrentDirectory, "Logs");
        public static string LogPath => Path.Combine(Environment.CurrentDirectory, "Logs");
        public static string LogName => Path.Combine(SavePath, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        public const ConsoleColor DefaultColor = ConsoleColor.Gray;
        public static void Text(object text)
        {
            LogAndSave(text);
        }
        public static void Info(object text)
        {
            LogAndSave(text, "[Info]", ConsoleColor.Yellow);
        }
        public static void Error(object text)
        {
            LogAndSave(text, "[Error]", ConsoleColor.Red);
        }
        public static void Warn(object text)
        {
            LogAndSave(text, "[Warn]", ConsoleColor.DarkYellow);
        }
        public static void Success(object text)
        {
            LogAndSave(text, "[Success]", ConsoleColor.Green);
        }
        internal static void Init()
        {
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);
        }
        public static void LogAndSave(object message, string prefix = "[Log]", ConsoleColor color = DefaultColor, bool save = true)
        {
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"{prefix} {message}");
                Console.ForegroundColor = DefaultColor;
                if (save)
                    File.AppendAllText(LogName, $"{DateTime.Now:yyyy-MM-dd-HH:mm:ss} - {prefix} {message}{Environment.NewLine}", Encoding.UTF8);
            }
            catch { }
        }
    }
    public static class UnfollowLog
    {
        public static void Init()
        {
            var timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = 1000 * 60
            };
            timer.Elapsed += Callback;
            timer.Start();
        }
        public static void Callback(object? o, System.Timers.ElapsedEventArgs e)
        {
            var dir = Path.Combine(Environment.CurrentDirectory, "UnfollowLog");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var unfollow = Datas.COL_ANTIS.Query().Where(a => a.UnfollowDate > DateTime.Now.Date).ToList();
            File.WriteAllText(Path.Combine(dir, $"{DateTime.Now:yyyy_mm_dd}.txt"), $"{DateTime.Now} 取关人数: {unfollow.Select(u => u.UId).Distinct().Count()}}}\r\n\r\n{string.Join("\r\n", unfollow.Select(u => $"{u.Name}<{u.UId}> (于 {u.FollowDate} 关注, {u.UnfollowDate} 取关)"))}");
        }
    }
}
