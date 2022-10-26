using COLib;

namespace COServer.Commands
{
    /// <summary>
    /// Debug コマンドを扱うクラスです。
    /// </summary>
    public class DebugCommand : SlackCommand
    {
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// <param name="text">コマンドパラメータ。</param>
        /// <param name="user">利用しているユーザー。</param>
        /// <param name="url">レスポンス URL。</param>
        public DebugCommand(string? text, SlackUser user, Uri url) :
            base("debug", text, user, url) { }

        /// <summary>
        /// コマンドの使用方法を表す文字列を取得します。
        /// </summary>
        protected override string GetUsage()
        {
            return "これはデバッグ用コマンドです。";
        }

        /// <summary>
        /// コマンドの実際の処理を実装します。
        /// </summary>
        /// <param name="message">出力メッセージ。</param>
        /// <returns>コマンドが正常に実行されたかどうか。</returns>
        protected override bool ExecuteImpl(out string message)
        {
            message = $"コマンドを受け取りました。\nユーザーID: `{User.ID}`\nコマンドの引数: " +
                (Text.Any() ? $"`{Text}`" : "なし");
            return true;
        }
    }
}
