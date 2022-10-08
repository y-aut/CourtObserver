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
        private Dictionary<DateOnly, int> data;

        public CourtCalendar()
        {
            data = new Dictionary<DateOnly, int>();
        }

        /// <summary>
        /// date 日の hour 時からが空いているかどうかを示す値を取得します。
        /// 取得ができていない場合は、null が返されます。
        /// </summary>
        public bool? GetValue(DateOnly date, int hour)
        {
            if (!data.ContainsKey(date))
            {
                return null;
            }
            return (data[date] & (1 << hour)) != 0;
        }

        /// <summary>
        /// date 日の hour 時からが空いているかどうかを示す値を設定します。
        /// </summary>
        public void SetValue(DateOnly date, int hour, bool value)
        {
            if (!data.ContainsKey(date))
            {
                data.Add(date, 0);
            }
            if (value)
            {
                data[date] |= 1 << hour;
            }
            else
            {
                data[date] &= ~(1 << hour);
            }
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        public void Clean()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var date in data.Keys)
            {
                if (date < today)
                {
                    data.Remove(date);
                }
            }
        }
    }
}
