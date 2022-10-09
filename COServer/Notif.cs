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
            var list = (await GetUsersAsync(date)).ToList();
            if (list.Contains(user))
            {
                return false;
            }
            list.Add(user);
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                list.Select(i => i.ID));
            return true;
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストから user を削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveUserAsync(DateHour date, SlackUser user)
        {
            var list = (await GetUsersAsync(date)).ToList();
            if (!list.Contains(user))
            {
                return false;
            }
            list.Remove(user);
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                list.Select(i => i.ID));
            return true;
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveUsersAsync(DateHour date)
        {
            var list = await GetUsersAsync(date);
            if (!list.Any())
            {
                return false;
            }
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                new List<string>());
            return true;
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        public static async Task CleanAsync()
        {
            var today = int.Parse(DateOnly.FromDateTime(DateTime.Today).ToKeyString());
            await Firestore.RemoveDocumentsAsync(COLLECTION_ID,
                doc => int.Parse(doc.Id) < today);
        }
    }
}