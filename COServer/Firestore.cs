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
            var jsonString = File.ReadAllText("service_account_key.json");
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return FirestoreDb.Create(FIRESTORE_PROJECT_ID, builder.Build());
        }

        /// <summary>
        /// キーと値のペアをデータベースに格納します。
        /// </summary>
        public static async Task SetDataAsync(string collection, string document, string key, object value)
        {
            var doc = db.Collection(collection).Document(document);
            var data = new Dictionary<string, object>()
            {
                { key, value },
            };
            await doc.SetAsync(data);
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
