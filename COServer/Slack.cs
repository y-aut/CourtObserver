using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using COLib;
using COServer.Controllers;

namespace COServer
{
    using JsonDict = Dictionary<string, JsonElement>;

    /// <summary>
    /// ホームタブに関する更新を Slack に送ります。
    /// </summary>
    public static class Slack
    {
        /// <summary>
        /// views.publish の URL です。
        /// </summary>
        private const string PUBLISH_URL = @"https://slack.com/api/views.publish";

        private static ILogger _logger => Util.Logger;

        /// <summary>
        /// ホームタブを更新します。
        /// </summary>
        /// <param name="user">表示するユーザーです。</param>
        /// <param name="date">選択されている日付です。</param>
        /// <param name="sessionId">ユーザー毎のセッション ID です。</param>
        public static async Task UpdateHome(SlackUser user, DateOnly date, int sessionId = -1)
        {
            try
            {
                // 予約可能なコートのリストを取得
                var calendar = await CourtCalendarIO.GetCourtsAsync(date);

                var json = await GetAppHomeJson(user, date, calendar);

                // セッション ID が変わっていれば、更新を破棄
                if (sessionId >= 0 && sessionId != SlackEventController.GetUserSessionId(user))
                {
                    _logger.LogInformation("Session Discarded. Session ID: {id}", sessionId);
                    return;
                }

                // データを送信
                var parameters = JsonSerializer.Serialize(new Dictionary<string, string>()
                {
                    { "user_id", user.ID },
                    { "view", json },
                });

                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(new HttpMethod("POST"), PUBLISH_URL);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Const.TOKEN}");

                request.Content = new StringContent(parameters);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                request.Content.Headers.ContentEncoding.Add("UTF-8");

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Received failure response From Slack: {response}", response.ToString());
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var dict = JsonSerializer.Deserialize<JsonDict>(content);
                if (dict?.GetValueAsBool("ok") != true)
                {
                    _logger.LogWarning("Received failure content: {content}", content);
                    return;
                }

                _logger.LogInformation("Sended AppHome Json to Slack.");
            }
            catch (Exception e)
            {
                _logger.Warn(e);
            }
        }

        /// <summary>
        /// blocks の値を用いて、ホームタブを更新します。
        /// </summary>
        /// <param name="user">表示するユーザーです。</param>
        /// <param name="blocks">view.blocks の値です。</param>
        public static async Task UpdateHome(SlackUser user, string blocks)
        {
            try
            {
                var json = $"{{ \"type\": \"home\", \"blocks\": {blocks} }}";

                // データを送信
                var parameters = JsonSerializer.Serialize(new Dictionary<string, string>()
                {
                    { "user_id", user.ID },
                    { "view", json },
                });

                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(new HttpMethod("POST"), PUBLISH_URL);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Const.TOKEN}");

                request.Content = new StringContent(parameters);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                request.Content.Headers.ContentEncoding.Add("UTF-8");

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Received failure response From Slack: {response}", response.ToString());
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var dict = JsonSerializer.Deserialize<JsonDict>(content);
                if (dict?.GetValueAsBool("ok") != true)
                {
                    _logger.LogWarning("Received failure content: {content}", content);
                    return;
                }

                _logger.LogInformation("Sended AppHome Json to Slack.");
            }
            catch (Exception e)
            {
                _logger.Warn(e);
            }
        }

        /// <summary>
        /// App ホーム用の Json 文字列を作成します。
        /// </summary>
        private static async Task<string> GetAppHomeJson(SlackUser user, DateOnly date,
            Dictionary<int, IEnumerable<CourtValue>> calendar)
        {
            var json = new StringBuilder(File.ReadAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\app_home.txt")));
            json.Replace("%DATE%", date.ToApiString());
            json.Replace("%DATE_DISP%", date.ToDisplayString());

            IEnumerable<int> hours;
            if (date == JST.Today)
            {
                hours = Enumerable.Range(Const.START_HOUR, Const.END_HOUR - Const.START_HOUR);
            }
            else
            {
                hours = Const.PRIMARY_HOUR.ToList();
            }

            // 通知設定の取得は並列で行う
            var strList = new List<StringBuilder>();
            var notifTasks = new List<Task<bool>>();
            var usersTasks = new List<Task<IEnumerable<SlackUser>>>();
            var reservedStrList = new List<StringBuilder>();

            for (int i = 0; i < hours.Count(); i++)
            {
                // 予約できるコート
                var courts = new List<Court>();
                // 情報を未取得のコート
                bool isUnknown = true;
                if (calendar.TryGetValue(hours.ElementAt(i), out var list))
                {
                    foreach (var val in list)
                    {
                        if (isUnknown && val.CourtState != CourtState.Unknown)
                        {
                            isUnknown = false;
                        }
                        if (val.CourtState == CourtState.Empty)
                        {
                            courts.Add(val.Court);
                        }
                    }
                }
                if (isUnknown)
                {
                    continue;
                }

                StringBuilder str;
                if (courts.Any())
                {
                    str = new StringBuilder(File.ReadAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\app_home_empty.txt")));
                    str.Replace("%COURT%", string.Join(", ", courts.Select(c => c.ToBriefString())));
                }
                else
                {
                    str = new StringBuilder(File.ReadAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\app_home_reserved.txt")));
                    // ユーザーの通知を取得
                    notifTasks.Add(Notif.GetValueAsync(new DateHour(date, hours.ElementAt(i)), user));
                    // 通知をオンにしているユーザーを取得
                    usersTasks.Add(Notif.GetUsersAsync(new DateHour(date, hours.ElementAt(i))));
                    reservedStrList.Add(str);
                }
                // 時刻を置き換え
                str.Replace("%START%", hours.ElementAt(i).ToString());
                str.Replace("%END%",
                    (i == hours.Count() - 1 ? Const.END_HOUR : hours.ElementAt(i + 1)).ToString());

                strList.Add(str);
            }

            // 通知設定が取得できるまで待機
            await Task.WhenAll(notifTasks);

            for (int i = 0; i < notifTasks.Count; i++)
            {
                bool notif = await notifTasks[i];

                var matches = Regex.Matches(reservedStrList[i].ToString(), @"%OFF=(.*)ON=(.*)%");
                foreach (Match match in matches)
                {
                    reservedStrList[i].Replace(match.Value, match.Groups[notif ? 2 : 1].Value);
                }

                var users = await usersTasks[i];
                if (users == null || !users.Any())
                {
                    reservedStrList[i].Replace("%PEOPLE%", "通知はオフになっています。");
                }
                else
                {
                    if (users.Contains(user))
                    {
                        if (users.Count() == 1)
                        {
                            reservedStrList[i].Replace("%PEOPLE%",
                                "あなたはこの時間帯への通知をオンにしています。");
                        }
                        else
                        {
                            reservedStrList[i].Replace("%PEOPLE%",
                                $"あなたと他 {users.Count() - 1} 人はこの時間帯への通知をオンにしています。");
                        }
                    }
                    else
                    {
                        reservedStrList[i].Replace("%PEOPLE%",
                            $"{users.Count()} 人がこの時間帯への通知をオンにしています。");
                    }
                }
            }

            json.Replace("%COURTS%", string.Concat(strList));

            return json.ToString();
        }
    }
}
