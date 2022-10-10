using COLib;

namespace COLib
{
    /// <summary>
    /// 日付と時間について、空いているかどうかの情報を格納します。
    /// </summary>
    public class CourtCalendar
    {
        /// <summary>
        /// 日曜日でない祝日を保存しておくリストです。
        /// </summary>
        public static List<DateOnly> Holidays { get; private set; } = new List<DateOnly>();

        private readonly Dictionary<DateHour, CourtState> data;
        public Court Court { get; private set; }

        public CourtCalendar(Court court)
        {
            data = new();
            Court = court;
        }

        /// <summary>
        /// CourtCalendar オブジェクトの値をコピーしてインスタンスを作成します。
        /// </summary>
        public CourtCalendar(CourtCalendar src)
        {
            data = new(src.data);
            Court = src.Court;
        }

        /// <summary>
        /// 指定した時刻が空いているかどうかを示す値を取得します。
        /// 取得ができていない場合は、null が返されます。
        /// </summary>
        public CourtState? GetValue(DateHour date)
        {
            if (!data.ContainsKey(date))
            {
                return null;
            }
            return data[date];
        }

        /// <summary>
        /// 指定した時刻が空いているかどうかを示す値を設定します。
        /// </summary>
        public void SetValue(DateHour date, CourtState value)
        {
            if (!data.ContainsKey(date))
            {
                data.Add(date, value);
            }
            data[date] = value;
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        public void Clean()
        {
            var today = JST.Today;
            foreach (var key in data.Keys)
            {
                if (key.Date < today)
                {
                    data.Remove(key);
                }
            }
        }

        /// <summary>
        /// 全てのデータを削除します。
        /// </summary>
        public void Clear()
        {
            data.Clear();
        }

        /// <summary>
        /// 日時のリストを取得します。
        /// </summary>
        public Dictionary<DateHour, CourtState>.KeyCollection GetDates()
        {
            return data.Keys;
        }
    }
}
