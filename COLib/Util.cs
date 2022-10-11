using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLib
{
    /// <summary>
    /// テニスコートを表す列挙型です。
    /// </summary>
    public enum Court
    {
        Takara,
        Okazaki,

        Unknown = -1,
    }

    /// <summary>
    /// テニスコートの空き状況を表す列挙型です。
    /// </summary>
    public enum CourtState
    {
        Unknown,    // 不明

        Empty,      // 予約可能
        Lottery,    // 抽選予約可能
        Reserved,   // 予約不可
        OutOfDate,  // 予約受付期間外
        Closed,     // 休館・点検
    }

    /// <summary>
    /// コートと空き状況をペアにもつ構造体です。
    /// </summary>
    public struct CourtValue
    {
        public Court Court { get; set; }
        public CourtState CourtState { get; set; }

        public CourtValue(Court court, CourtState state)
        {
            Court = court;
            CourtState = state;
        }

        public string ToDataString()
        {
            return Court.ToDataString() + "," + CourtState.ToDataString();
        }

        public static CourtValue Parse(string str)
        {
            int comma = str.IndexOf(",");
            return new CourtValue(str[..comma].ParseCourt(), str[(comma + 1)..].ParseCourtState());
        }

        public static bool TryParse(string str, out CourtValue value)
        {
            try
            {
                value = Parse(str);
                return true;
            }
            catch (Exception)
            {
                value = new();
                return false;
            }
        }
    }

    /// <summary>
    /// 定数を保持するクラスです。
    /// </summary>
    public static class Const
    {
        /// <summary>
        /// Slack App の Bot Token です。
        /// </summary>
        public const string TOKEN = @"xoxb-3492638691285-4187297113366-t5XTyeScZmSc6fVn1gMyuvKi";

        // 予約可能時間
        public const int START_HOUR = 8;
        public const int END_HOUR = 21;

        // 当日以外に予約できる時間
        public static readonly int[] PRIMARY_HOUR = { 8, 10, 12, 14, 16, 18 };

        // 予約できる時間数
        public static readonly int[] PRIMARY_HOUR_SPAN = { 2, 2, 2, 2, 2, 3 };
    }

    public static partial class Util
    {
        private static readonly List<string> dayOfWeekString = new List<string>()
            { "日", "月", "火", "水", "木", "金", "土" };

        /// <summary>
        /// 日曜日から始まる日本語の曜日の配列です。
        /// </summary>
        public static List<string> DayOfWeekString => dayOfWeekString;

        /// <summary>
        /// 日付を表示用の文字列に変換します。
        /// </summary>
        public static string ToDisplayString(this DateOnly date)
        {
            return $"{date.Month}月{date.Day}日（{DayOfWeekString[(int)date.DayOfWeek]}）";
        }

        /// <summary>
        /// コートを表示用の文字列に変換します。
        /// </summary>
        public static string ToDisplayString(this Court court)
        {
            return court switch
            {
                Court.Takara => "宝が池公園テニスコート",
                Court.Okazaki => "岡崎公園テニスコート",
                _ => "",
            };
        }

        /// <summary>
        /// コートを省略文字列に変換します。
        /// </summary>
        public static string ToBriefString(this Court court)
        {
            return court switch
            {
                Court.Takara => "宝が池",
                Court.Okazaki => "岡崎",
                _ => "",
            };
        }

        /// <summary>
        /// コートをデータベース用文字列に変換します。
        /// </summary>
        public static string ToDataString(this Court court)
        {
            return court switch
            {
                Court.Takara => "takara",
                Court.Okazaki => "okazaki",
                _ => "",
            };
        }

        /// <summary>
        /// データベース用文字列をコートに変換します。
        /// </summary>
        public static Court ParseCourt(this string str)
        {
            return str.ToLower() switch
            {
                "takara" => Court.Takara,
                "okazaki" => Court.Okazaki,
                _ => Court.Unknown,
            };
        }

        /// <summary>
        /// 空き状況をデータベース用文字列に変換します。
        /// </summary>
        public static string ToDataString(this CourtState court)
        {
            return court switch
            {
                CourtState.Empty => "empty",
                CourtState.Reserved => "reserved",
                CourtState.Lottery => "lottery",
                CourtState.OutOfDate => "outofdate",
                CourtState.Closed => "closed",
                CourtState.Unknown => "unknown",
                _ => "",
            };
        }

        /// <summary>
        /// データベース用文字列を空き状況に変換します。
        /// </summary>
        public static CourtState ParseCourtState(this string str)
        {
            return str.ToLower() switch
            {
                "empty" => CourtState.Empty,
                "reserved" => CourtState.Reserved,
                "lottery" => CourtState.Lottery,
                "outofdate" => CourtState.OutOfDate,
                "closed" => CourtState.Closed,
                _ => CourtState.Unknown,
            };
        }

        /// <summary>
        /// コンソールに現在の時刻と情報を書き込みます。
        /// </summary>
        public static void WriteInfo(string str)
        {
            Console.WriteLine($"[{JST.Now:MM/dd HH:mm:ss.fff} {str}");
        }
    }

    /// <summary>
    /// 日本標準時を扱うクラスです。
    /// </summary>
    public static class JST
    {
        private static readonly TimeZoneInfo JapanTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        /// <summary>
        /// JST で今日の日付を取得します。
        /// </summary>
        public static DateOnly Today
        {
            get
            {
                return DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(
                    JapanTimeZone.BaseUtcOffset).DateTime);
            }
        }

        /// <summary>
        /// JST で現在の時刻を取得します。
        /// </summary>
        public static DateTime Now
        {
            get
            {
                return DateTimeOffset.UtcNow.ToOffset(JapanTimeZone.BaseUtcOffset).DateTime;
            }
        }
    }
}
