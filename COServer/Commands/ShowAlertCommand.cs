using COLib;
using System.Text;

namespace COServer.Commands
{
    /// <summary>
    /// ShowAlert コマンドを扱うクラスです。
    /// </summary>
    public class ShowAlertCommand : SlackCommand
    {
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// <param name="text">コマンドパラメータ。</param>
        /// <param name="user">利用しているユーザー。</param>
        /// <param name="url">レスポンス URL。</param>
        public ShowAlertCommand(string? text, SlackUser user, Uri url) :
            base("showalert", text, user, url) { }

        /// <summary>
        /// コマンドの使用方法を表す文字列を取得します。
        /// </summary>
        protected override string GetUsage()
        {
            return "通知をオンにしている時間帯の一覧を表示します。";
        }

        /// <summary>
        /// コマンドの実際の処理を実装します。
        /// </summary>
        /// <param name="message">出力メッセージ。</param>
        /// <returns>コマンドが正常に実行されたかどうか。</returns>
        protected override bool ExecuteImpl(out string message)
        {
            if (Text.Any())
            {
                message = "パラメータを指定することはできません。";
                return false;
            }

            _ = SendDataAsync();

            message = "通知をオンにしている時間帯の一覧を表示します。";
            return true;
        }

        /// <summary>
        /// 情報を取得して送信します。
        /// </summary>
        private async Task SendDataAsync()
        {
            await Task.Yield();

            string message;

            // 設定を取得する
            var dateHours = await Notif.GetDateHoursAsync(User);

            if (!dateHours.Any())
            {
                message = "通知をオンにしている時間帯はありません。";
            }
            else
            {
                dateHours.Sort();

                // 文字列に変換する
                var strMsg = new StringBuilder();

                var current = DateOnly.MinValue;
                foreach (var date in dateHours)
                {
                    if (date.Date != current)
                    {
                        strMsg.Append($"\n{date.Date.ToDisplayString()}: {date.Hour}");
                        current = date.Date;
                    }
                    else
                    {
                        strMsg.Append($", {date.Hour}");
                    }
                }
                // 改行を削除
                strMsg.Remove(0, 1);

                message = strMsg.ToString();
            }

            // Slack に送信する
            await COLib.Slack.SendWebhookAsync(message, ResponseUrl);
        }
    }
}
