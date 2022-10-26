using COLib;

namespace COServer
{
    /// <summary>
    /// どの時間の通知をどのユーザーがオンにしているかの情報を扱うクラスです。
    /// </summary>
    public static class Notif
    {
        /// <summary>
        /// Firestore コレクションの ID です。
        /// </summary>
        private const string COLLECTION_ID = "notif";

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを返します。
        /// </summary>
        public static async Task<IEnumerable<SlackUser>> GetUsersAsync(DateHour date)
        {
            var dict = await Firestore.GetDataAsync(COLLECTION_ID, date.Date.ToKeyString());
            if (dict == null || !dict.ContainsKey(date.Hour.ToString()))
            {
                return new List<SlackUser>();
            }
            return ((List<object>)dict[date.Hour.ToString()]).Select(i => new SlackUser((string)i));
        }

        /// <summary>
        /// 指定した時刻の通知を user がオンにしているかどうかを返します。
        /// </summary>
        public static async Task<bool> GetValueAsync(DateHour date, SlackUser user)
        {
            return (await GetUsersAsync(date)).Contains(user);
        }

        /// <summary>
        /// 指定したユーザーが通知をオンにしている時間帯を取得します。
        /// </summary>
        public static async Task<List<DateHour>> GetDateHoursAsync(SlackUser user)
        {
            var list = new List<DateHour>();
            for (int hour = Const.START_HOUR; hour < Const.END_HOUR; hour++)
            {
                var dates = await Firestore.GetArrayDocuments(COLLECTION_ID, hour.ToString(), user.ID);
                list.AddRange(dates.Select(s => new DateHour(s.FromKeyString(), hour)));
            }
            return list;
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストに user を追加します。
        /// </summary>
        public static async Task AddUserAsync(DateHour date, SlackUser user)
        {
            await Firestore.AddDocument(COLLECTION_ID, date.Date.ToKeyString());
            await Firestore.AddArrayItemAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストから user を削除します。
        /// </summary>
        public static async Task RemoveUserAsync(DateHour date, SlackUser user)
        {
            await Firestore.RemoveArrayItemAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// ユーザーの指定した時刻の通知を設定します。
        /// </summary>
        public static async Task SetUserAsync(DateHour date, SlackUser user, bool value)
        {
            if (value)
            {
                await AddUserAsync(date, user);
            }
            else
            {
                await RemoveUserAsync(date, user);
            }
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveUsersAsync(DateHour date)
        {
            return await Firestore.RemoveListAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString());
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