using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourtObserver
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

        private Dictionary<(DateOnly date, int hour), CourtState> data;

        public CourtCalendar()
        {
            data = new();
        }

        /// <summary>
        /// date 日の hour 時からが空いているかどうかを示す値を取得します。
        /// 取得ができていない場合は、null が返されます。
        /// </summary>
        public CourtState? GetValue(DateOnly date, int hour)
        {
            if (!data.ContainsKey((date, hour)))
            {
                return null;
            }
            return data[(date, hour)];
        }

        /// <summary>
        /// date 日の hour 時からが空いているかどうかを示す値を設定します。
        /// </summary>
        public void SetValue(DateOnly date, int hour, CourtState value)
        {
            if (!data.ContainsKey((date, hour)))
            {
                data.Add((date, hour), value);
            }
            data[(date, hour)] = value;
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        public void Clean()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var key in data.Keys)
            {
                if (key.date < today)
                {
                    data.Remove(key);
                }
            }
        }
    }
}
