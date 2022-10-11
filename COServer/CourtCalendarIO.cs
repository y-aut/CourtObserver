using COLib;

namespace COServer
{
    /// <summary>
    /// コートが予約できるかどうかの情報をデータベースと送受信するクラスです。
    /// </summary>
    public class CourtCalendarIO
    {
        /// <summary>
        /// Firestore コレクションの ID です。
        /// </summary>
        private const string COLLECTION_ID = "court";

        /// <summary>
        /// 指定した日のコートの空き状況のリストと時間の組を取得します。
        /// </summary>
        public static async Task<Dictionary<int, IEnumerable<CourtValue>>> GetCourtsAsync(DateOnly date)
        {
            var dict = await Firestore.GetDataAsync(COLLECTION_ID, date.ToKeyString());
            if (dict == null)
            {
                return new();
            }
            var newDict = new Dictionary<int, IEnumerable<CourtValue>>();
            foreach (var item in dict)
            {
                newDict.Add(int.Parse(item.Key),
                    ((List<object>)item.Value).Select(i => CourtValue.Parse((string)i)));
            }
            return newDict;
        }

        /// <summary>
        /// 指定した日のコートの空き状況のリストを更新します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> UpdateCourtAsync(DateHour date, CourtValue court)
        {
            return await Firestore.UpdatePair(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), court.ToDataString(),
                i => i[..i.IndexOf(',')], i => i[(i.IndexOf(',') + 1)..]);
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        public static async Task CleanAsync()
        {
            var today = int.Parse(JST.Today.ToKeyString());
            await Firestore.RemoveDocumentsAsync(COLLECTION_ID,
                doc => int.Parse(doc.Id) < today);
        }
    }
}
