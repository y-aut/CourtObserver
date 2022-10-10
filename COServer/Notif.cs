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
        /// 指定した時刻の通知をオンにしているユーザーのリストに user を追加します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> AddUserAsync(DateHour date, SlackUser user)
        {
            return await Firestore.AddDataAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストから user を削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveUserAsync(DateHour date, SlackUser user)
        {
            return await Firestore.RemoveDataAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// ユーザーの指定した時刻の通知を設定します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> SetUserAsync(DateHour date, SlackUser user, bool value)
        {
            if (value)
            {
                return await AddUserAsync(date, user);
            }
            else
            {
                return await RemoveUserAsync(date, user);
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