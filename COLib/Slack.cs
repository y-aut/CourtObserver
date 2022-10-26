using System.Text;
using System.Text.Json;

namespace COLib
{
    public static class Slack
    {
        /// <summary>
        /// ファイルアップロード用の URL です。
        /// </summary>
        const string UPLOAD_URL = @"https://slack.com/api/files.upload";

        /// <summary>
        /// メッセージ送信用の URL です。
        /// </summary>
        const string POSTMESSAGE_URL = @"https://slack.com/api/chat.postMessage";

#if DEBUG
        /// <summary>
        /// メインチャンネルの ID です。
        /// </summary>
        const string CHANNEL_ID = @"C03EY5A4S57";
#else
        /// <summary>
        /// メインチャンネルの ID です。
        /// </summary>
        const string CHANNEL_ID = @"C045M38UPLP";
#endif

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
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Const.TOKEN}");

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Util.WriteInfo("Slack にテキストが送信されました。");
            }
            else
            {
                Util.WriteInfo("Slack にテキストを送信する際に失敗レスポンスを受け取りました。\n" +
                    response.ToString());
            }
        }

        /// <summary>
        /// メンション付きテキストを送信します。
        /// </summary>
        /// <param name="text">送信するテキスト。</param>
        /// <param name="users">メンションするユーザー。</param>
        public static async Task SendTextAsync(string text, IEnumerable<SlackUser> users)
        {
            await SendTextAsync(string.Join(" ", users.Select(i => $"<@{i.ID}>")) + " " + text);
        }

        /// <summary>
        /// ファイルをアップロードします。
        /// </summary>
        public static async Task UploadFileAsync(string path)
        {
            // curl -F file=@path -F channels=CHANNEL -H "Authorization: Bearer TOKEN" URL
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"), UPLOAD_URL);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Const.TOKEN}");

            using var multipartContent = new MultipartFormDataContent
            {
                { new ByteArrayContent(File.ReadAllBytes(path)), "file", Path.GetFileName(path) },
                { new StringContent(CHANNEL_ID), "channels" }
            };

            request.Content = multipartContent;

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Util.WriteInfo("Slack にファイルがアップロードされました。");
            }
            else
            {
                Util.WriteInfo("Slack にファイルをアップロードする際に失敗レスポンスを受け取りました。\n" +
                    response.ToString());
            }
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

        /// <summary>
        /// Webhook URL を使用してテキストを送信します。
        /// </summary>
        /// <param name="text">送信するテキスト。</param>
        public static async Task SendWebhookAsync(string text, Uri url)
        {
            var payload = new Dictionary<string, string> { { "text", text } };
            var json = JsonSerializer.Serialize(payload);

            using var client = new HttpClient();
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, content);
            Console.WriteLine(result);
        }
    }
}
