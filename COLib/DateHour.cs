﻿namespace COLib
{
    /// <summary>
    /// 関数を保持する静的クラスです。
    /// </summary>
    public static partial class Util
    {
        public static string ToKeyString(this DateOnly date)
        {
            return date.ToString("yyyyMMdd");
        }

        public static string ToApiString(this DateOnly date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        public static DateOnly FromKeyString(this string str)
        {
            return DateOnly.ParseExact(str, "yyyyMMdd");
        }
    }

    /// <summary>
    /// 日付と時刻をひとまとめにした構造体です。
    /// </summary>
    public struct DateHour : IEquatable<DateHour>, IComparable<DateHour>
    {
        /// <summary>
        /// 日付を表します。
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// 時刻を表します。
        /// </summary>
        public int Hour { get; set; }

        public DateHour(DateOnly date, int hour)
        {
            Date = date;
            Hour = hour;
        }

        /// <summary>
        /// API の通信用文字列に変換します。
        /// </summary>
        public string ToApiString()
        {
            return Date.ToApiString() + "-" + Hour.ToString();
        }

        /// <summary>
        /// "2022-10-3-16" などの文字列を日付 (2022/10/3) と時刻 (16:00) にパースします。
        /// </summary>
        public static bool TryParse(string str, out DateHour result)
        {
            result = new DateHour();

            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            var words = str.Split('-');
            if (words.Length != 4)
            {
                return false;
            }

            if (!int.TryParse(words[0], out int year) ||
                !int.TryParse(words[1], out int month) ||
                !int.TryParse(words[2], out int day) ||
                !int.TryParse(words[3], out int hour))
            {
                return false;
            }

            try
            {
                result.Date = new DateOnly(year, month, day);
            }
            catch (Exception)
            {
                return false;
            }

            if (!(0 <= hour && hour < 24))
            {
                return false;
            }
            result.Hour = hour;

            return true;
        }

        public static bool operator ==(DateHour left, DateHour right) =>
            left.Date == right.Date && left.Hour == right.Hour;
        public static bool operator !=(DateHour left, DateHour right) => !(left == right);
        public static bool operator <(DateHour left, DateHour right) =>
            left.Date == right.Date ? (left.Hour < right.Hour) : (left.Date < right.Date);
        public static bool operator >(DateHour left, DateHour right) =>
            left.Date == right.Date ? (left.Hour > right.Hour) : (left.Date > right.Date);
        public static bool operator <=(DateHour left, DateHour right) => !(left > right);
        public static bool operator >=(DateHour left, DateHour right) => !(left < right);

        public override bool Equals(object? obj)
        {
            if (obj is DateHour date)
            {
                return Equals(date);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Date.GetHashCode() ^ Hour.GetHashCode();
        }

        public bool Equals(DateHour other)
        {
            return this == other;
        }

        public int CompareTo(DateHour other)
        {
            if (this < other)
            {
                return -1;
            }
            else if (this == other)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}