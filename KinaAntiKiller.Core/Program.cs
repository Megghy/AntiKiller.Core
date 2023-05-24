// See https://aka.ms/new-console-template for more information
using AntiKiller.Core;

Logs.Init();

if (Config.Instance.UId <= 0 || string.IsNullOrEmpty(Config.Instance.Cookie))
{
    Console.WriteLine("请在配置文件中设置UId和Cookie");
    Console.ReadLine();
    Environment.Exit(0);
}
else
{
    Datas.Init();
    Searcher.Init();
    UnfollowLog.Init();
    while (true)
        Task.Delay(1).Wait();
}