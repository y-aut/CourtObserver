using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using COLib;

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

        private static ILogger _logger
        {
            get
            {
                if (Util.Logger == null)
                {
                    throw new NullReferenceException();
                }
                return Util.Logger;
            }
        }

        /// <summary>
        /// ホームタブを更新します。
        /// </summary>
        /// <param name="user">表示するユーザーです。</param>
        /// <param name="view">現在のビューです。</param>
        public static async Task UpdateHome(SlackUser user, JsonDict? view)
        {
            try
            {
                // 選択されている日付を取得
                DateOnly date;
                if (view == null)
                {
                    date = DateOnly.FromDateTime(DateTime.Today);
                }
                else
                {
                    var dateString = view.GetStringFromPath("state/values/./datepicker-action/selected_date");
                    if (dateString == null)
                    {
                        date = DateOnly.FromDateTime(DateTime.Today);
                    }
                    else
                    {
                        date = DateOnly.ParseExact(dateString, "yyyy-MM-dd");
                    }
                }

                // ダミー
                var cal = new List<CourtCalendar>();
                cal.Add(new CourtCalendar(Court.Takara));
                cal.Add(new CourtCalendar(Court.Okazaki));
                for (int i = 0; i < 10; i++)
                {
                    for (int j = Const.START_HOUR; j < Const.END_HOUR; j++)
                    {
                        cal[0].SetValue(new DateHour(date.AddDays(i), j),
                            (i + j) % 2 == 0 ? CourtState.Empty : CourtState.Reserved);
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    for (int j = Const.START_HOUR; j < Const.END_HOUR; j++)
                    {
                        cal[1].SetValue(new DateHour(date.AddDays(i), j),
                            i % 2 == 1 ? CourtState.Empty : CourtState.Reserved);
                    }
                }

                var json = GetAppHomeJson(user, date, cal);

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
        private static string GetAppHomeJson(SlackUser user, DateOnly date, List<CourtCalendar> calendars)
        {
            var json = new StringBuilder(File.ReadAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\app_home.txt")));
            json.Replace("%DATE%", date.ToApiString());

            IEnumerable<int> hours;
            if (date == DateOnly.FromDateTime(DateTime.Today))
            {
                hours = Enumerable.Range(Const.START_HOUR, Const.END_HOUR - Const.START_HOUR);
            }
            else
            {
                hours = Const.PRIMARY_HOUR.ToList();
            }

            var courtsStr = new StringBuilder();
            for (int i = 0; i < hours.Count(); i++)
            {
                // 予約できるコート
                var courts = new List<Court>();
                // 情報を未取得のコート
                bool isUnknown = true;
                for (int j = 0; j < calendars.Count; j++)
                {
                    var val = calendars[j].GetValue(new DateHour(date, hours.ElementAt(i)));
                    if (isUnknown && val != null && val != CourtState.Unknown)
                    {
                        isUnknown = false;
                    }
                    if (val == CourtState.Empty)
                    {
                        courts.Add(calendars[j].Court);
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
                    bool notif = false;

                    var matches = Regex.Matches(str.ToString(), @"%OFF=(.*)ON=(.*)%");
                    foreach (Match match in matches)
                    {
                        str.Replace(match.Value, match.Groups[notif ? 2 : 1].Value);
                    }
                }
                // 時刻を置き換え
                str.Replace("%START%", hours.ElementAt(i).ToString());
                str.Replace("%END%",
                    (i == hours.Count() - 1 ? Const.END_HOUR : hours.ElementAt(i + 1)).ToString());

                courtsStr.Append(str);
            }
            json.Replace("%COURTS%", courtsStr.ToString());

            return json.ToString();
        }
    }
}
