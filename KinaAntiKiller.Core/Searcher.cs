using System.Text.Json.Nodes;

namespace AntiKiller.Core
{
    public static class Searcher
    {
        public const int PAGE_SIZE = 50;
        public static readonly HttpClient client = new();
        public static async Task DoSearch()
        {
            while (true)
            {
                try
                {
                    var pageNum = 1;
                    var isSuccess = false;
                    List<FansInfo> fans = new();

                    while (true)
                    {
                        try
                        {
                            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.bilibili.com/x/relation/fans?vmid={Config.Instance.UId}&pn={pageNum}&ps={PAGE_SIZE}&order=desc");
                            requestMessage.Headers.TryAddWithoutValidation("Cookie", Config.Instance.Cookie);
                            var json = await (await client.SendAsync(requestMessage))?.Content?.ReadAsStringAsync();
                            var jsonNode = JsonNode.Parse(json);
                            var total = 0;
                            var code = jsonNode["code"]?.GetValue<int>();
                            if (code == 0)
                            {
                                if (jsonNode["data"]!["list"]!.AsArray() is { } userArray)
                                {
                                    total = jsonNode["data"]!["total"]!.GetValue<int>();
                                    foreach (var userJson in userArray)
                                    {
                                        var fan = new FansInfo()
                                        {
                                            Id = userJson["mid"]!.GetValue<long>(),
                                            Name = userJson["uname"]!.GetValue<string>(),
                                            Sign = userJson["sign"]!.GetValue<string>(),
                                            FaceUrl = userJson["face"]!.GetValue<string>(),
                                            FollowDate = GetTime(userJson["mtime"]!.GetValue<long>()),
                                        };
                                        fans.Add(fan);
                                    }
                                    Console.WriteLine($"[{fans.Count}/{total}]");
                                    if (fans.Count >= 1000
                                        || fans.Count == total
                                        || userArray.Count == 0) //没获取完但是没更多了, 可能是粉丝里有销号的
                                    {
                                        isSuccess = true;
                                        break; //获取完了, 退出循环
                                    }
                                    pageNum++;
                                    await Task.Delay(2000);
                                }
                            }
                            else if (code == 22007)
                            {
                                Logs.Warn($"未能获取更多粉丝, 由于没有设置cookie或者cookie无效. message: {jsonNode["message"]}");
                                isSuccess = false;
                                break; //获取不了, 退出循环
                            }
                            else
                            {
                                Logs.Warn($"获取失败. message: {jsonNode["message"]}");
                                await Task.Delay(10000);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logs.Error($"获取失败. pn: {pageNum}\r\n{ex}");
                            await Task.Delay(10000);
                        }
                    }
                    if (fans.Count > 0 && isSuccess)
                        await CheckFans(fans);

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Logs.Error(ex);
                }
                finally
                {
                    await Task.Delay(1000 * 10);
                }
            }
        }
        public static async Task CheckFans(List<FansInfo> fans)
        {
            if (Datas.Fans.Count == 0)
            {
                Logs.Warn($"当前没有任何数据, 插入 {fans.Count} 条");
                Datas.COL_FANS.Insert(fans);
                Datas.Fans.AddRange(fans);
            }
            else
            {
                var unfollow = Datas.Fans.Where(f => f.IsVisiable && !fans.Exists(nowFans => nowFans.Id == f.Id)).ToList();
                var newFans = fans.Where(f => !Datas.Fans.Exists(oldFans => oldFans.IsVisiable && oldFans.Id == f.Id)).ToList();
                var tempUnfollow = unfollow.ToList();
                foreach (var user in tempUnfollow)
                {
                    try
                    {
                        var followResult = await IsFollowingMe(user.Id);
                        if (followResult == true)
                        {
                            if (user.FollowDate < fans.Min(f => f.FollowDate))
                            {
                                Logs.Info($"{user} 已不再可观测.");
                                user.IsVisiable = false;
                                Datas.COL_FANS.Update(user);
                            }
                            else
                            {
                                //只是由于位置变动暂时获取不到
                            }
                            unfollow.Remove(user);
                        }
                        else if (!followResult.HasValue) //如果暂时没获取到关注状态先不管, 以免误操作
                        {
                            unfollow.Remove(user);
                            Logs.Warn($"未能确定 {user} 是否取关了, 暂时忽略");
                        }
                        else
                        {
                            //确实取关了, 不动
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.Error(ex);
                        unfollow.Remove(user);
                    }
                    finally
                    {
                        Task.Delay(1000 * 3).Wait();
                    }
                }
                if (unfollow.Count > 0)
                {
                    Logs.Info($"[{DateTime.Now}] 发现 {unfollow.Count} 个取关粉丝\r\n {string.Join(", ", unfollow.Select(a => a.ToString()))}");
                    if (Config.Instance.Block)
                    {
                        try
                        {
                            var cookie = Config.Instance.Cookie.Replace(" ", "");
                            var pairs = cookie.Split(";");
                            var bili_jct = string.Empty;
                            foreach (var item in pairs)
                            {
                                if (item.Split('=') is { } pair && pair[0].Trim() == "bili_jct")
                                    bili_jct = pair[1];
                            }

                            foreach (var item in unfollow)
                            {
                                // 创建一个FormUrlEncodedContent实例，用于存储urlencoded数据
                                var content = new FormUrlEncodedContent(new[]
                                {
                                    new KeyValuePair<string, string>("fid", item.Id.ToString()),
                                    new KeyValuePair<string, string>("act", "5"), //拉黑
                                    new KeyValuePair<string, string>("re_src", "11"),
                                    new KeyValuePair<string, string>("csrf", bili_jct)
                                });
                                content.Headers.Add("Cookie", Config.Instance.Cookie);
                                try
                                {
                                    var response = await client.PostAsync("https://api.bilibili.com/x/relation/modify", content);
                                    var json = JsonNode.Parse(await response.Content.ReadAsStreamAsync());
                                    if (json["code"]?.GetValue<int>() == 0)
                                    {
                                        Logs.LogAndSave($"已拉黑: {item}", "[BLACKLIST]", ConsoleColor.Cyan);
                                        Datas.Fans.Remove(item);
                                        Datas.COL_FANS.Delete(item.Id);
                                    }
                                    else
                                    {
                                        Logs.Warn($"{item.Name} 拉黑失败: {json["message"]}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logs.Error(ex);
                                    Task.Delay(1000 * 10).Wait();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logs.Error(ex);
                        }
                    }
                    var antiInfo = unfollow.Select(a => new AntiInfo()
                    {
                        UId = a.Id,
                        Name = a.Name,
                        FaceUrl = a.FaceUrl,
                        Sign = a.Sign,
                        UnfollowDate = DateTime.Now,
                        FollowDate = a.FollowDate
                    });
                    Datas.COL_ANTIS.Insert(antiInfo);
                }
                else if (newFans.Count > 0)
                {
                    newFans.ForEach(f =>
                    {
                        if (Datas.Fans.Find(existFans => existFans.Id == f.Id) is { } existFan)
                        {
                            existFan.IsVisiable = true;
                            Datas.COL_FANS.Update(existFan); //又可以观测到了
                        }
                        else
                        {
                            Datas.Fans.Add(f);
                            Datas.COL_FANS.Insert(f);
                            Logs.Success($"[{DateTime.Now}] 更新完成, 新增 {newFans.Count} 个粉丝: {string.Join(", ", newFans.Select(a => a.ToString()))}");
                        }
                    });
                }
                else
                {
                    Logs.Success($"[{DateTime.Now}] 更新完成, 没有取关的粉丝, 共 {fans.Count} 个");
                }
            }
        }
        public static async Task<bool?> IsFollowingMe(long uid)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.bilibili.com/x/space/acc/relation?mid={uid}");
            requestMessage.Headers.TryAddWithoutValidation("Cookie", Config.Instance.Cookie);
            var jsonNode = JsonNode.Parse(await client.Send(requestMessage)?.Content?.ReadAsStreamAsync());
            if (jsonNode["code"].GetValue<int>() == 0)
            {
                var attribute = jsonNode["data"]["be_relation"]["attribute"].GetValue<int>();
                return attribute is 1 or 2;
            }
            return null;
        }

        public static readonly DateTime UnixStartDate = new(1970, 1, 1, 8, 0, 0, 0);
        public static DateTime GetTime(long timeStamp)
        {
            if (timeStamp is > 100000000 and < 10000000000)
            {
                long lTime = timeStamp * 10000000;
                return UnixStartDate.Add(new(lTime));
            }
            else
            {
                long lTime = timeStamp * 10000;
                return UnixStartDate.Add(new(lTime));
            }
        }
    }
}
