using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace COServer
{
    /// <summary>
    /// Cloud Firestore を操作するためのクラスです。
    /// </summary>
    public static class Firestore
    {
        /// <summary>
        /// Firestore プロジェクト ID です。
        /// </summary>
        private const string FIRESTORE_PROJECT_ID = "codatabase-89e31";

        /// <summary>
        /// データベースのインスタンスです。
        /// </summary>
        private static FirestoreDb db = Initialize();

        /// <summary>
        /// データベースのインスタンスを初期化します。
        /// </summary>
        public static FirestoreDb Initialize()
        {
            var jsonString = File.ReadAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\service_account_key.json"));
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return FirestoreDb.Create(FIRESTORE_PROJECT_ID, builder.Build());
        }

        /// <summary>
        /// キーに対応するコレクションに新しい値を追加します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> AddDataAsync(string collection, string document, string key, object value)
        {
            var doc = db.Collection(collection).Document(document);
            var dict = (await doc.GetSnapshotAsync()).ToDictionary();

            if (dict == null)
            {
                dict = new Dictionary<string, object>()
                {
                    { key, new List<object>() { value } }
                };
            }
            else if (!dict.TryGetValue(key, out var obj))
            {
                dict.Add(key, new List<object>() { value });
            }
            else
            {
                if (obj is not List<object> list)
                {
                    Util.Logger.LogWarning("Database error occured.");
                    return false;
                }
                if (list.Contains(value))
                {
                    return false;
                }
                list.Add(value);
                dict[key] = list;
            }

            await doc.SetAsync(dict);
            return true;
        }

        /// <summary>
        /// キーに対応するコレクションから値を削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveDataAsync(string collection, string document, string key, object value)
        {
            var doc = db.Collection(collection).Document(document);
            var dict = (await doc.GetSnapshotAsync()).ToDictionary();

            if (!dict.TryGetValue(key, out var obj))
            {
                return false;
            }
            else
            {
                if (obj is not List<object> list)
                {
                    Util.Logger.LogWarning("Database error occured.");
                    return false;
                }
                if (!list.Contains(value))
                {
                    return false;
                }
                if (list.Count == 1)
                {
                    if (dict.Count == 1)
                    {
                        await doc.DeleteAsync();
                        return true;
                    }
                    dict.Remove(key);
                }
                else
                {
                    list.Remove(value);
                    dict[key] = list;
                }
            }

            await doc.SetAsync(dict);
            return true;
        }

        /// <summary>
        /// キーに対応するコレクションを削除します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> RemoveListAsync(string collection, string document, string key)
        {
            var doc = db.Collection(collection).Document(document);
            var dict = (await doc.GetSnapshotAsync()).ToDictionary();

            if (!dict.ContainsKey(key))
            {
                return false;
            }

            if (dict.Count == 1)
            {
                await doc.DeleteAsync();
                return true;
            }

            dict.Remove(key);
            await doc.SetAsync(dict);
            return true;
        }

        /// <summary>
        /// 特定のドキュメント内のキーと値のペアをリストにして返します。
        /// 取得に失敗した場合は、null が返されます。
        /// </summary>
        public static async Task<Dictionary<string, object>> GetDataAsync(string collection, string document)
        {
            var doc = db.Collection(collection).Document(document);
            var snapshot = await doc.GetSnapshotAsync();
            return snapshot.ToDictionary();
        }

        /// <summary>
        /// 条件を満たすドキュメントを全て削除します。
        /// </summary>
        public static async Task RemoveDocumentsAsync(string collection, Func<DocumentSnapshot, bool> predicate)
        {
            var query = db.Collection(collection);
            await Task.WhenAll((await query.GetSnapshotAsync()).Where(predicate)
                .Select(doc => query.Document(doc.Id).DeleteAsync()).ToArray());
        }
    }
}
