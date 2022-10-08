using CoreTweet;

namespace CourtObserver
{
    public class Twitter
    {
        private string ConsumerKey { get; set; }
        private string ConsumerKeySecret { get; set; }
        private string AccessToken { get; set; }
        private string AccessTokenSecret { get; set; }
        private Tokens Token { get; set; }

        public Twitter()
        {
            // キーの設定
            ConsumerKey = "";
            ConsumerKeySecret = "";
            AccessToken = "";
            AccessTokenSecret = "";

            // トークンの生成
            Token = Tokens.Create(
                ConsumerKey,
                ConsumerKeySecret,
                AccessToken,
                AccessTokenSecret);
        }

        /// <summary>
        /// テキストをツイートします。
        /// </summary>
        /// <param name="text">ツイートするテキスト。</param>
        public void Tweet(string text)
        {
            Token.Statuses.Update(new
            {
                status = text
            });
        }

        /// <summary>
        /// テキストとメディアをツイートします。
        /// </summary>
        /// <param name="text">ツイートするテキスト。</param>
        /// <param name="path">ツイートに含めるメディアのパス。</param>
        public void Tweet(string text, string path)
        {
            long mediaID = UploadImage(path);

            Token.Statuses.Update(new
            {
                status = text,
                media_ids = new long[] { mediaID }
            });
        }

        /// <summary>
        /// メディアをアップロードします。
        /// </summary>
        /// <param name="path">アップロードするメディアのパス。</param>
        private long UploadImage(string path)
        {
            // media 引数には FileInfo, Stream, IEnumerable<byte> が指定できます。
            // また media_data 引数に画像を BASE64 でエンコードした文字列を指定することができます。
            MediaUploadResult upload_result = Token.Media.Upload(media: new FileInfo(path));
            return upload_result.MediaId;
        }
    }
}
