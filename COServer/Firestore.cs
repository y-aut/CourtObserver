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
        /// ドキュメントが存在しない場合、新たに作成します。
        /// </summary>
        public static async Task AddDocument(string collection, string document)
        {
            await db.Collection(collection).Document(document)
                .SetAsync(new Dictionary<string, string>(), SetOptions.MergeAll);
        }

        /// <summary>
        /// キーに対応するコレクションに含まれる配列に新しい値を追加します。
        /// </summary>
        public static async Task AddArrayItemAsync(string collection, string document, string key, object value)
        {
            await db.Collection(collection).Document(document).UpdateAsync(key, FieldValue.ArrayUnion(value));
        }

        /// <summary>
        /// キーに対応するコレクションに含まれる配列から値を削除します。
        /// </summary>
        public static async Task RemoveArrayItemAsync(string collection, string document, string key, object value)
        {
            await db.Collection(collection).Document(document).UpdateAsync(key, FieldValue.ArrayRemove(value));
        }

        /// <summary>
        /// キーに対応するコレクションで、指定したキーに対応する配列に特定の値を含むドキュメントの ID を取得します。
        /// </summary>
        public static async Task<IEnumerable<string>> GetArrayDocuments(string collection, string key, object value)
        {
            var query = await db.Collection(collection).WhereArrayContains(key, value).GetSnapshotAsync();
            return query.Documents.Select(d => d.Id);
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

        /// <summary>
        /// キーと値の組を用いてコレクションの値を更新します。
        /// </summary>
        /// <returns>データを変更したかどうか。</returns>
        public static async Task<bool> UpdatePair(string collection, string document,
            string key, string value, Func<string, string> keySelecter, Func<string, string> valueSelecter)
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
                var objKey = keySelecter(value);
                var objVal = valueSelecter(value);
                var index = list.FindIndex(i => keySelecter((string)i) == objKey);
                if (index >= 0)
                {
                    if (valueSelecter((string)list[index]) == objVal)
                    {
                        return false;
                    }
                    list[index] = value;
                }
                else
                {
                    list.Add(value);
                }
                dict[key] = list;
            }

            await doc.SetAsync(dict);
            return true;
        }
    }
}
