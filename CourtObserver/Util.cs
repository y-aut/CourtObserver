using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourtObserver
{
    /// <summary>
    /// テニスコートを表す列挙型です。
    /// </summary>
    public enum Court
    {
        Takara,
        Okazaki,
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
    }
}
