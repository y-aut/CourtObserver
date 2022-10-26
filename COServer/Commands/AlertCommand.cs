using COLib;
using System.Text;

namespace COServer.Commands
{
    /// <summary>
    /// Alert コマンドを扱うクラスです。
    /// </summary>
    public class AlertCommand : SlackCommand
    {
        /// <summary>
        /// 設定可能な月数。ここで指定した月数以上先の通知を設定することはできない。
        /// </summary>
        private const int LIMIT_MONTHS = 3;

        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// <param name="text">コマンドパラメータ。</param>
        /// <param name="user">利用しているユーザー。</param>
        /// <param name="url">レスポンス URL。</param>
        public AlertCommand(string? text, SlackUser user, Uri url) :
            base("alert", text, user, url) { }

        /// <summary>
        /// コマンドの使用方法を表す文字列を取得します。
        /// </summary>
        protected override string GetUsage()
        {
            return "```/alert [on|off] start_date [end_date] start_hour [end_hour] " +
                "[-d w|h|sun|mon|tue|... [w|h|...] ...]\n\n" +
                "start_date : 設定開始日を指定します。(例) 2022-10-3\n" +
                "end_date   : 設定終了日を指定します。この日付は範囲に含まれません。\n" +
                "start_hour : 設定開始時刻を指定します。(例) 16\n" +
                "end_hour   : 設定終了時刻を指定します。この時刻は範囲に含まれません。\n" +
                "-d         : 曜日を指定します。w は平日、h は土日を表します。" +
                "スペースを挟んで複数の曜日を指定することができます。```\n\n" +
                "(使用例)\n" +
                "`/alert 2022-1-13 14`\n" +
                "  2022年1月13日の14時からのコートの通知をオンにします。\n" +
                "`/alert off 2022-8-1 2022-9-1 14 21 -d h tue`\n" +
                "  2022年8月の週末と火曜日の14時から20時までのコートの通知をオフにします。\n" +
                "`/alert on 2022-12-1 2023-1-1 18 -d w`\n" +
                "  2022年12月の平日の18時からのコートの通知をオンにします。";
        }

        /// <summary>
        /// コマンドの実際の処理を実装します。
        /// </summary>
        /// <param name="message">出力メッセージ。</param>
        /// <returns>コマンドが正常に実行されたかどうか。</returns>
        protected override bool ExecuteImpl(out string message)
        {
            if (!Text.Any())
            {
                message = "パラメータを指定してください。";
                return false;
            }

            var words = Text.Split(' ');
            if (words.Length < 2)
            {
                message = "パラメータの数が不正です。";
                return false;
            }

            // 設定値
            bool? value = null;
            DateOnly? startDate = null;
            DateOnly? endDate = null;
            int? startHour = null;
            int? endHour = null;
            uint days = 0u;  // 曜日ごとの設定をビット列で保持
            bool daysSpecified = false;

            const int DAYOFWEEK_COUNT = 7;

            // days 用のビットマスク
            const uint DAYSMASK_FULL = (1u << DAYOFWEEK_COUNT) - 1;
            const uint DAYSMASK_HOLIDAY = (1u << (int)DayOfWeek.Sunday) | (1u << (int)DayOfWeek.Saturday);
            const uint DAYSMASK_WEEKDAY = DAYSMASK_FULL ^ DAYSMASK_HOLIDAY;

            int index = 0;
            while (index < words.Length)
            {
                switch (words[index])
                {
                    case "on":
                        if (value != null)
                        {
                            message = "設定値が複数回指定されています。";
                            return false;
                        }
                        value = true;
                        index++;
                        break;

                    case "off":
                        if (value != null)
                        {
                            message = "設定値が複数回指定されています。";
                            return false;
                        }
                        value = false;
                        index++;
                        break;

                    case "-d":
                        if (daysSpecified)
                        {
                            message = "`-d` オプションが複数回指定されています。";
                            return false;
                        }
                        daysSpecified = true;

                        // 曜日を取得
                        bool found = true;
                        while (found)
                        {
                            index++;
                            if (index >= words.Length)
                            {
                                break;
                            }

                            switch (words[index])
                            {
                                case "w":
                                    days |= DAYSMASK_WEEKDAY;
                                    break;

                                case "h":
                                    days |= DAYSMASK_HOLIDAY;
                                    break;

                                default:
                                    var day = COLib.Util.DayOfWeekShortEnglish.IndexOf(words[index]);
                                    if (day == -1)
                                    {
                                        found = false;
                                    }
                                    else
                                    {
                                        days |= 1u << day;
                                    }
                                    break;
                            }
                        }
                        break;

                    default:
                        // 時刻かどうか
                        if (startHour == null && int.TryParse(words[index], out int sHour) &&
                            0 <= sHour && sHour <= 24)
                        {
                            startHour = sHour;
                            index++;
                            if (index < words.Length)
                            {
                                if (int.TryParse(words[index], out int eHour) && 0 <= eHour && eHour <= 24)
                                {
                                    endHour = eHour;
                                    if (startHour >= endHour)
                                    {
                                        message = "開始時刻は終了時刻よりも前である必要があります。";
                                        return false;
                                    }
                                    index++;
                                }
                            }
                        }
                        // 日付かどうか
                        else if (startDate == null && DateOnly.TryParse(words[index], out var sDate))
                        {
                            startDate = sDate;
                            index++;
                            if (index < words.Length)
                            {
                                if (DateOnly.TryParse(words[index], out var eDate))
                                {
                                    endDate = eDate;
                                    if (startDate >= endDate)
                                    {
                                        message = "開始日は終了日よりも前である必要があります。";
                                        return false;
                                    }
                                    index++;
                                }
                            }
                        }
                        else
                        {
                            message = $"不明なパラメータです: {words[index]}";
                            return false;
                        }
                        break;
                }
            }

            if (value == null)
            {
                value = true;
            }
            if (startDate == null)
            {
                message = "日付が指定されていません。";
                return false;
            }
            if (startHour == null)
            {
                message = "時刻が指定されていません。";
                return false;
            }
            if (endHour == null)
            {
                endHour = startHour + 1;
            }
            if (endDate == null)
            {
                if (daysSpecified)
                {
                    message = "曜日を指定するには終了日を指定する必要があります。";
                    return false;
                }
                endDate = startDate.Value.AddDays(1);
            }
            else if (daysSpecified && endDate == startDate.Value.AddDays(1))
            {
                message = "曜日を指定するには2日以上の期間を指定する必要があります。";
                return false;
            }
            if (!daysSpecified)
            {
                days = DAYSMASK_FULL;
            }
            else if (days == 0u)
            {
                message = "`-d` オプションの後に曜日が指定されていません。";
                return false;
            }

            var today = JST.Today;
            if (startDate < today)
            {
                message = "今日よりも前の日付を開始日に指定することはできません。";
                return false;
            }

            // 月数の制限
            var limit = today.AddMonths(LIMIT_MONTHS);
            if (endDate > limit)
            {
                message = $"{LIMIT_MONTHS}か月以上後の日付を終了日に指定することはできません。";
                return false;
            }

            // 文字列に変換する
            var strMsg = new StringBuilder();

            // 日付
            strMsg.Append(startDate.Value.ToDisplayLongString());
            if (endDate != startDate.Value.AddDays(1))
            {
                strMsg.Append("から" + endDate.Value.AddDays(-1).ToDisplayLongString() + "まで");
            }
            strMsg.Append('の');

            // 曜日
            if (days != DAYSMASK_FULL)
            {
                var daysCopy = days;
                if ((daysCopy & DAYSMASK_WEEKDAY) == DAYSMASK_WEEKDAY)
                {
                    daysCopy ^= DAYSMASK_WEEKDAY;
                    strMsg.Append("平日" + (daysCopy != 0u ? "と" : ""));
                }
                if ((daysCopy & DAYSMASK_HOLIDAY) == DAYSMASK_HOLIDAY)
                {
                    daysCopy ^= DAYSMASK_HOLIDAY;
                    strMsg.Append("週末" + (daysCopy != 0u ? "と" : ""));
                }
                if (daysCopy != 0u)
                {
                    int count = 0;
                    for (int i = 0; i < DAYOFWEEK_COUNT; i++)
                    {
                        if ((daysCopy & (1 << i)) != 0u)
                        {
                            strMsg.Append(COLib.Util.DayOfWeekString[i]);
                            count++;
                        }
                    }
                    if (count == 1)
                    {
                        strMsg.Append("曜日");
                    }
                }
                strMsg.Append('の');
            }

            // 時刻
            strMsg.Append($"{startHour}時");
            if (endHour != startHour + 1)
            {
                strMsg.Append($"から{endHour - 1}時まで");
            }
            strMsg.Append("の通知を" + (value.Value ? "オン" : "オフ") + "に設定しました。");

            // 該当する日時を検索
            startHour = Math.Max(startHour.Value, Const.START_HOUR);
            endHour = Math.Min(endHour.Value, Const.END_HOUR);
            if (startHour >= endHour)
            {
                message = "時刻の範囲が不正です。" +
                    $"有効な時刻は{Const.START_HOUR}時から{Const.END_HOUR}時までです。";
                return false;
            }

            var primaryHour = Const.PRIMARY_HOUR.Where(h => startHour <= h && h < endHour);
            var dateHours = new List<DateHour>();
            for (var date = startDate.Value; date < endDate; date = date.AddDays(1))
            {
                if ((days & (1u << (int)date.DayOfWeek)) == 0u)
                {
                    continue;
                }
                if (date == today)
                {
                    for (int hour = startHour.Value; hour < endHour; hour++)
                    {
                        dateHours.Add(new DateHour(date, hour));
                    }
                }
                else
                {
                    foreach (int hour in primaryHour)
                    {
                        dateHours.Add(new DateHour(date, hour));
                    }
                }
            }

            if (!dateHours.Any())
            {
                message = "指定されたパラメータに該当する日付がありません。";
                return false;
            }

            // 設定を変更する
            foreach (var date in dateHours)
            {
                _ = Notif.SetUserAsync(date, User, value.Value);
            }

            message = strMsg.ToString();
            return true;
        }
    }
}
