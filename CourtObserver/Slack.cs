using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourtObserver
{
    public class Slack
    {
        /// <summary>
        /// Slack API の Webhook URL です。
        /// </summary>
        const string WEBHOOK_URL = @"https://hooks.slack.com/services/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";

        /// <summary>
        /// ファイルアップロード用の URL です。
        /// </summary>
        const string UPLOAD_URL = @"https://slack.com/api/files.upload";

        /// <summary>
        /// メッセージ送信用の URL です。
        /// </summary>
        const string POSTMESSAGE_URL = @"https://slack.com/api/chat.postMessage";

        /// <summary>
        /// Slack App の Bot Token です。
        /// </summary>
        const string TOKEN = @"xoxb-3492638691285-4187297113366-t5XTyeScZmSc6fVn1gMyuvKi";

        /// <summary>
        /// メインチャンネルの ID です。
        /// </summary>
        const string CHANNEL_ID = @"C03EY5A4S57";

        ///// <summary>
        ///// テキストを送信します。
        ///// </summary>
        ///// <param name="text">送信するテキスト。</param>
        //[Obsolete]
        //public static async Task SendTextAsync_Deprecated(string text)
        //{
        //    var payload = new Payload { Text = text };
        //    var json = JsonSerializer.Serialize(payload);

        //    using var client = new HttpClient();
        //    using var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var result = await client.PostAsync(WEBHOOK_URL, content);
        //    Console.WriteLine(result);
        //}

        /// <summary>
        /// テキストを送信します。
        /// </summary>
        /// <param name="text">送信するテキスト。</param>
        public static async Task SendTextAsync(string text)
        {
            var urlContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "channel", CHANNEL_ID },
                { "text", text },
            });
            var query = await urlContent.ReadAsStringAsync();

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"),
                $"{POSTMESSAGE_URL}?{query}");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {TOKEN}");

            var response = await httpClient.SendAsync(request);
            Console.WriteLine(response);
        }

        /// <summary>
        /// ファイルをアップロードします。
        /// </summary>
        public static async Task UploadFileAsync(string path)
        {
            // curl -F file=@path -F channels=CHANNEL -H "Authorization: Bearer TOKEN" URL
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"), UPLOAD_URL);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {TOKEN}");

            using var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(path)), "file", Path.GetFileName(path));
            multipartContent.Add(new StringContent(CHANNEL_ID), "channels");

            request.Content = multipartContent;

            var response = await httpClient.SendAsync(request);
            Console.WriteLine(response);
        }

        /// <summary>
        /// テキストと画像を送信します。
        /// </summary>
        /// <param name="text">送信するテキスト。</param>
        /// <param name="path">送信する画像のパス。</param>
        public static async Task SendTextImageAsync(string text, string path)
        {
            await SendTextAsync(text);
            await UploadFileAsync(path);
        }
    }

    public class Payload
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }
}
